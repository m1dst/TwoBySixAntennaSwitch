using System;
using System.Text;
using System.Threading;
using Microsoft.SPOT.Hardware;

namespace TwoBySixAntennaSwitch.Eeprom
{
    public class Eeprom
    {

        // Enumerator for various types of EEPROM chips. Pretty much the only difference between 
        //  them is the maximum address for storing information - although the 16B chip I2C 
        //  communications use a single address byte instead of two (which is handled in the 
        //  ReadBytes and WriteBytes methods).
        public enum IC : ushort
        {
            _24LC16B = 0xFF,    // tested, works
            _24LC32A = 0x0FFF,  // untested
            _24LC64 = 0x1FFF,   // untested
            _24LC128 = 0x3FFF,  // untested
            _24LC256 = 0x7FFF,  // tested, works
            _24LC512 = 0xFFFF   // tested, works
        }

        protected const int TransactionTimeout = 1000; // ms
        protected const byte DefaultAddress = 0x50;    // 1010AAA, where (A)s get ORed in from the Address value
        protected const int DefaultClockSpeed = 100;   // MHz

        protected byte address = DefaultAddress;
        protected int clockRateKHz = DefaultClockSpeed;
        // A0 thru A2 address - should be a value between 0 and 7
        public byte Address { get { return address; } set { address = value; SetConfig(); } }
        public int ClockRateKHz { get { return clockRateKHz; } set { clockRateKHz = value; SetConfig(); } }
        // Specifies whether numbers > 1 byte are stored big-endian or not.
        public bool BigEndian { get; set; }
        // I2CBus instance
        public I2CBus I2CBus { get; set; }

        protected I2CDevice.Configuration I2CConfig;   // Passed by the constructor
        protected UInt16 MaxSize = 0x00;               // Set by the IC parameter in the constructor

        public Eeprom(IC chip, I2CBus i2cBus) : this(chip, i2cBus, 0, DefaultClockSpeed) { }
        public Eeprom(IC chip, I2CBus i2cBus, byte address) : this(chip, i2cBus, address, DefaultClockSpeed) { }
        public Eeprom(IC chip, I2CBus i2cBus, byte address, int clockRateKHz)
        {
            if (address > 7) { address = 0; }
            I2CBus = i2cBus;
            this.address = address;
            this.clockRateKHz = clockRateKHz;
            MaxSize = (ushort)chip;
            SetConfig();
        }

        protected void SetConfig()
        {
            I2CConfig = new I2CDevice.Configuration((ushort)(DefaultAddress | address), clockRateKHz);
        }

        protected int Write(byte[] addressBytes, byte[] data)
        {
            // Prepare the write buffer
            var writeBuffer = new byte[addressBytes.Length + data.Length];
            // copy the destination address and data into the write buffer
            addressBytes.CopyTo(writeBuffer, 0);
            data.CopyTo(writeBuffer, addressBytes.Length);
            // instantiate the eeprom using the current configuration
            I2CBus.Write(I2CConfig, writeBuffer, TransactionTimeout);
            // Give the EEPROM a brief pause to finish its business
            Thread.Sleep(100);
            // return the length of the write buffer
            return writeBuffer.Length;
        }
        protected byte[] WriteRead(byte[] addressBytes, uint length)
        {
            // prepare a read buffer of the appropriate length
            var data = new byte[length];
            // Do a write/read operation
            I2CBus.WriteRead(I2CConfig, addressBytes, data, TransactionTimeout);
            // return our read data byte array
            return data;
        }

        // ========================
        //   Read / write methods
        // ========================

        public byte[] ReadBytes(UInt16 readAddress, uint length)
        {
            // Check to make sure the read address is less than the maximum size.
            if (readAddress <= MaxSize)
            {
                // Grab the low byte.
                var addrLow = (byte)(readAddress & 0xFF);
                // Check to see if the chip is not a 24LC16B. If so, it uses a 
                //  two-byte address.
                if (MaxSize > 0xFF)
                {
                    // 2-byte address is used.
                    var addrHigh = (byte)((readAddress >> 8) & 0xFF);
                    return WriteRead(new[] { addrHigh, addrLow }, length);
                }
                // it's a 24LC16B chip. Send the read with a single byte address.
                return WriteRead(new[] { addrLow }, length);
            }
            throw new Exception("Read address above maximum EEPROM register.");
        }

        public byte ReadByte(UInt16 readAddress)
        {
            return ReadBytes(readAddress, 1)[0];
        }
        public int ReadInt16(UInt16 readAddress)
        {
            byte[] data = ReadBytes(readAddress, 2);
            int retVal;
            if (BigEndian)
            {
                retVal = data[1] + (data[0] << 8);
            }
            else
            {
                retVal = data[0] + (data[1] << 8);
            }
            return retVal;
        }
        public UInt16 ReadUInt16(UInt16 readAddress)
        {
            byte[] data = ReadBytes(readAddress, 2);
            UInt16 retVal;
            if (BigEndian)
            {
                retVal = (UInt16)(data[1] + (data[0] << 8));
            }
            else
            {
                retVal = (UInt16)(data[0] + (data[1] << 8));
            }
            return retVal;
        }
        public string ReadString(UInt16 readAddress, uint length)
        {
            byte[] byteBuffer = ReadBytes(readAddress, length);
            return new string(Encoding.UTF8.GetChars(byteBuffer));
        }

        public int WriteBytes(UInt16 writeAddress, byte[] data)
        {
            if (MaxSize >= writeAddress)
            {
                // Grab the low byte.
                var addrLow = (byte)(writeAddress & 0xFF);
                if (MaxSize > 0xFF)
                {
                    // double byte address.
                    var addrHigh = (byte)((writeAddress >> 8) & 0xFF);
                    return Write(new[] { addrHigh, addrLow }, data);
                }
                // single byte address
                return Write(new[] { addrLow }, data);
            }
            throw new Exception("Write address above maximum EEPROM register.");
        }

        public int WriteByte(UInt16 writeAddress, byte value)
        {
            return WriteBytes(writeAddress, new[] { value });
        }
        public int WriteInt16(UInt16 writeAddress, int value)
        {
            var valLow = (byte)(value & 0xFF);
            var valHigh = (byte)(value >> 8);
            if (BigEndian)
            {
                return WriteBytes(writeAddress, new[] { valHigh, valLow });
            }
            return WriteBytes(writeAddress, new[] { valLow, valHigh });
        }
        public int WriteUInt16(UInt16 writeAddress, UInt16 value)
        {
            var valLow = (byte)(value & 0xFF);
            var valHigh = (byte)(value >> 8);
            if (BigEndian)
            {
                return WriteBytes(writeAddress, new[] { valHigh, valLow });
            }
            return WriteBytes(writeAddress, new[] { valLow, valHigh });
        }
        public int WriteString(UInt16 writeAddress, string value)
        {
            // Make a null-character terminated byte array - one value larger than the string
            byte[] strBytes = Encoding.UTF8.GetBytes(value);
            var writeBuffer = new byte[strBytes.Length + 1];
            strBytes.CopyTo(writeBuffer, 0);
            // write the string...
            return WriteBytes(writeAddress, writeBuffer);
        }

        #region Overloads

        public int ReadInt16(int readAddress)
        {
            return ReadInt16((ushort)readAddress);
        }

        public int WriteInt16(int writeAddress, int value)
        {
            return WriteInt16((ushort)writeAddress, value);
        }

        public string ReadString(int readAddress, uint length)
        {
            return ReadString((ushort)readAddress, length);
        }

        public int WriteString(int writeAddress, string value)
        {
            return WriteString((ushort)writeAddress, value);
        }


        #endregion

    }
}
