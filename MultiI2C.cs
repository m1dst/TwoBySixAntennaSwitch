using Microsoft.SPOT.Hardware;

namespace TwoBySixAntennaSwitch
{
    /// <summary>
    /// I2C Helper to make it easier to use multiple I2C-devices on one I2C-bus
    /// </summary>
    public class MultiI2C
    {
        /// <summary>Reference to the I2C Device. All MultiI2C devices use the same I2CDevice class from the NETMF, so this reference is static</summary>
        private static I2CDevice _I2CDevice;

        /// <summary>I2C Configuration. Different for each device, so not a static reference</summary>
        private I2CDevice.Configuration _Configuration;

        /// <summary>Transaction timeout</summary>
        public int Timeout { get; set; }

        /// <summary>The address of the I2C device</summary>
        public ushort DeviceAddress { get { return this._Configuration.Address; } }

        /// <summary>The speed of the I2C device</summary>
        public int ClockRateKhz { get { return this._Configuration.ClockRateKhz; } }

        /// <summary>
        /// Initializes a new I2C device
        /// </summary>
        /// <param name="Address">The address of the I2C device</param>
        /// <param name="ClockRateKhz">The speed in Khz of the I2C device</param>
        public MultiI2C(ushort Address, int ClockRateKhz = 100)
        {
            // Sets the configuration in a local value
            this._Configuration = new I2CDevice.Configuration(Address, ClockRateKhz);

            // Sets the default timeout
            this.Timeout = 100;

            // If no I2C Device exists yet, we create it's first instance
            if (_I2CDevice == null)
            {
                // Creates the SPI Device
                _I2CDevice = new I2CDevice(this._Configuration);
            }
        }

        /// <summary>
        /// The 8-bit bytes to write to the I2C-buffer
        /// </summary>
        /// <param name="WriteBuffer">An array of 8-bit bytes</param>
        /// <returns>The amount of transferred bytes</returns>
        public int Write(byte[] WriteBuffer)
        {
            _I2CDevice.Config = this._Configuration;
            return _I2CDevice.Execute(new I2CDevice.I2CTransaction[] { I2CDevice.CreateWriteTransaction(WriteBuffer) }, this.Timeout);
        }

        /// <summary>
        /// The 16-bit bytes to write to the I2C-buffer
        /// </summary>
        /// <param name="WriteBuffer">An array of 16-bit bytes</param>
        /// <returns>The amount of transferred bytes</returns>
        public int Write(ushort[] WriteBuffer)
        {
            // Transforms the write buffer to 8-bit bytes and returns the value devided by 2 (to get the amount of 16-bit bytes again)
            return this.Write(Tools.UShortsToBytes(WriteBuffer)) / 2;
        }

        /// <summary>
        /// Reads 8-bit bytes
        /// </summary>
        /// <param name="ReadBuffer">An array with 8-bit bytes to read</param>
        /// <returns>The amount of transferred bytes</returns>
        public int Read(byte[] ReadBuffer)
        {
            _I2CDevice.Config = this._Configuration;
            return _I2CDevice.Execute(new I2CDevice.I2CTransaction[] { I2CDevice.CreateReadTransaction(ReadBuffer) }, this.Timeout);
        }

        /// <summary>
        /// Reads 16-bit bytes
        /// </summary>
        /// <param name="ReadBuffer">An array with 16-bit bytes to read</param>
        /// <returns>The amount of transferred bytes</returns>
        public int Read(ushort[] ReadBuffer)
        {
            // Creates an 8-bit readbuffer
            byte[] bReadBuffer = new byte[ReadBuffer.Length * 2];
            // Actually executes the transaction
            int Transferred = this.Read(bReadBuffer);
            // Converts the 8-bit readbuffer to 16-bit
            ReadBuffer = Tools.BytesToUShorts(bReadBuffer);
            // Devide by 2 to get the amount of 16-bit bytes
            return Transferred / 2;
        }

        /// <summary>
        /// Writes an array of 8-bit bytes to the interface, and reads an array of 8-bit bytes from the interface.
        /// </summary>
        /// <param name="WriteBuffer">An array with 8-bit bytes to write</param>
        /// <param name="ReadBuffer">An array with 8-bit bytes to read</param>
        /// <returns>The amount of transferred bytes</returns>
        public int WriteRead(byte[] WriteBuffer, byte[] ReadBuffer)
        {
            _I2CDevice.Config = this._Configuration;
            return _I2CDevice.Execute(new I2CDevice.I2CTransaction[] { 
                I2CDevice.CreateWriteTransaction(WriteBuffer),
                I2CDevice.CreateReadTransaction(ReadBuffer) 
            }, this.Timeout);
        }

        /// <summary>
        /// Writes an array of 16-bit bytes to the interface, and reads an array of 16-bit bytes from the interface.
        /// </summary>
        /// <param name="WriteBuffer">An array with 16-bit bytes to write</param>
        /// <param name="ReadBuffer">An array with 16-bit bytes to read</param>
        /// <returns>The amount of transferred bytes</returns>
        public int WriteRead(ushort[] WriteBuffer, ushort[] ReadBuffer)
        {
            // Creates an 8-bit readbuffer
            byte[] bReadBuffer = new byte[ReadBuffer.Length * 2];
            // Actually executes the transaction
            int Transferred = this.WriteRead(Tools.UShortsToBytes(WriteBuffer), bReadBuffer);
            // Converts the 8-bit readbuffer to 16-bit
            ReadBuffer = Tools.BytesToUShorts(bReadBuffer);
            // Devide by 2 to get the amount of 16-bit bytes
            return Transferred / 2;
        }
    }
}