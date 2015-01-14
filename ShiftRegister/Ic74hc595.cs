using System;
using System.Threading;
using Microsoft.SPOT.Hardware;

/*
 * Copyright 2012-2014 Stefan Thoolen (http://www.netmftoolbox.com/)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
namespace TwoBySixAntennaSwitch.ShiftRegister
{
    /// <summary>
    /// A chain of one or multiple serial to parallel ICs over managed SPI
    /// </summary>
    public class Ic74hc595 : IDisposable
    {
        #region "Constructors and destructor"
        /// <summary>A reference to the SPI Interface</summary>
        private MultiSPI _SpiInterface;

        /// <summary>Contains all pin values</summary>
        private byte[] _Data;

        /// <summary>When using bitbang mode, this bool is true. <see cref="_SpiInterface"/> won't be used if this is true.</summary>
        private bool _BitBangMode = false;
        /// <summary>When using bitbang mode, this will contain a reference to the SPCK pin. See also <see cref="_BitBangMode"/></summary>
        private IGPOPort _BBM_SPCK;
        /// <summary>When using bitbang mode, this will contain a reference to the CS pin. See also <see cref="_BitBangMode"/></summary>
        private IGPOPort _BBM_CS;
        /// <summary>When using bitbang mode, this will contain a reference to the MOSI pin. See also <see cref="_BitBangMode"/></summary>
        private IGPOPort _BBM_MOSI;
        /// <summary>Set to true when bitbang pins are created by the constructor</summary>
        private bool _PinDisposalRequired = false;

        /// <summary>
        /// Initialises a chain of one or multiple serial to parallel ICs over managed SPI
        /// </summary>
        /// <param name="SPI_Module">The SPI interface it's connected to</param>
        /// <param name="LatchPin">The slave select pin connected to the IC(s)</param>
        /// <param name="Bytes">The amount of 8-bit IC(s) connected</param>
        /// <param name="SpeedKHz">The max. SPI speed</param>
        public Ic74hc595(SPI.SPI_module SPI_Module, Cpu.Pin LatchPin, uint Bytes = 1, uint SpeedKHz = 1000)
        {
            // Full SPI configuration
            this._SpiInterface = new MultiSPI(new SPI.Configuration(
                ChipSelect_Port: LatchPin,
                ChipSelect_ActiveState: false,
                ChipSelect_SetupTime: 0,
                ChipSelect_HoldTime: 0,
                Clock_IdleState: true,
                Clock_Edge: true,
                Clock_RateKHz: SpeedKHz,
                SPI_mod: SPI_Module
            ));

            // The amount of ICs
            this._Init(Bytes);
        }

        /// <summary>
        /// Initialises a chain of one or multiple serial to parallel ICs over bitbanged SPI [WHEN POSSIBLE, USE MANAGED MODE!]
        /// </summary>
        /// <remarks>
        /// Use only when the managed SPI-pins can't be used. This method is way slower and locks the pins for any other purpose until disposed.
        /// </remarks>
        /// <param name="ClockPin">The clock pin connected to the IC(s)</param>
        /// <param name="DataPin">The data pin connected to the IC(s)</param>
        /// <param name="LatchPin">The slave select pin connected to the IC(s)</param>
        /// <param name="Bytes">The amount of 8-bit IC(s) connected</param>
        public Ic74hc595(Cpu.Pin ClockPin, Cpu.Pin DataPin, Cpu.Pin LatchPin, uint Bytes = 1)
        {
            // Enables bitbang mode and marks the pins as need-to-be-disposed
            this._BitBangMode = true;
            this._PinDisposalRequired = true;
            // Makes references to the SPI pins
            this._BBM_CS = new IntegratedGPO(LatchPin, true);
            this._BBM_MOSI = new IntegratedGPO(DataPin, false);
            this._BBM_SPCK = new IntegratedGPO(ClockPin, false);

            // The amount of ICs
            this._Init(Bytes);
        }
        /*
        /// <summary>
        /// Initialises a chain of one or multiple serial to parallel ICs over bitbanged SPI [WHEN POSSIBLE, USE MANAGED MODE!]
        /// </summary>
        /// <remarks>
        /// Use only when the managed SPI-pins can't be used. This method is way slower and locks the pins for any other purpose until disposed.
        /// </remarks>
        /// <param name="ClockPin">The clock pin connected to the IC(s)</param>
        /// <param name="DataPin">The data pin connected to the IC(s)</param>
        /// <param name="LatchPin">The slave select pin connected to the IC(s)</param>
        /// <param name="Bytes">The amount of 8-bit IC(s) connected</param>
        public SPIShifterOut(IGPOPort ClockPin, IGPOPort DataPin, IGPOPort LatchPin, uint Bytes = 1)
        {
            // Enables bitbang mode
            this._BitBangMode = true;
            // Makes references to the SPI pins
            this._BBM_CS = LatchPin; this._BBM_CS.Write(true);
            this._BBM_MOSI = DataPin; this._BBM_MOSI.Write(false);
            this._BBM_SPCK = ClockPin; this._BBM_SPCK.Write(false);

            // The amount of ICs
            this._Init(Bytes);
        }
        */
        /// <summary>
        /// Disposes this object, frees all locked items
        /// </summary>
        public void Dispose()
        {
            // Turns off all pins
            for (uint ByteNo = 0; ByteNo < this._Data.Length; ++ByteNo)
                this._Data[ByteNo] = 0;
            this._WriteSPI();

            // Disposes all pins
            if (this._PinDisposalRequired)
            {
                this._BBM_CS.Dispose();
                this._BBM_MOSI.Dispose();
                this._BBM_SPCK.Dispose();
            }
        }

        /// <summary>
        /// Initialises all bits and bytes
        /// </summary>
        /// <param name="Bytes">The amount of 8-bit IC(s) connected</param>
        private void _Init(uint Bytes)
        {
            // Prepares the data array
            this._Data = new byte[Bytes];

            // Declares all pins
            this.Pins = new Ic74hc595GPOPort[Bytes * 8];
            for (uint PinNo = 0; PinNo < this.Pins.Length; ++PinNo)
                this.Pins[PinNo] = new Ic74hc595GPOPort(this, PinNo);

            // Writes all zeroes to the SPI bus for the first time
            this._WriteSPI();
        }
        #endregion

        #region "Reading/writing"
        /// <summary>
        /// Pushes all data to the SPI bus
        /// </summary>
        private void _WriteSPI()
        {
            // Normal mode
            if (!this._BitBangMode)
            {
                this._SpiInterface.Write(this._Data);
                return;
            }
            // Bitbang mode; enables output to the IC chain
            this._BBM_CS.Write(false);
            // Loops through all bytes
            for (uint ByteNo = 0; ByteNo < this._Data.Length; ++ByteNo)
            {
                byte Value = this._Data[ByteNo];
                // Loops through all 8 bits
                for (byte BitNo = 0; BitNo < 8; ++BitNo)
                {
                    // Checks if the 8th bit is set
                    int BitVal = Value & 0x80;
                    // Shifts all bits in the value one place
                    Value = (byte)(Value << 1);
                    // If the 8th bit is true, we write true, elsewise false
                    this._BBM_MOSI.Write(BitVal == 0x80);
                    // Enable the clock for a short moment
                    this._BBM_SPCK.Write(true);
                    Thread.Sleep(1);
                    this._BBM_SPCK.Write(false);
                }
            }
            // Disables output to the IC chain
            this._BBM_CS.Write(true);
        }

        /// <summary>
        /// Writes a single bit
        /// </summary>
        /// <param name="Bit">The bit to write</param>
        /// <param name="State">The new state for the bit</param>
        protected void Write(uint Bit, bool State)
        {
            // Writes the bit
            this._Write(Bit, State);
            // Actually sends it to the SPI bus
            this._WriteSPI();
        }

        /// <summary>
        /// Writes a single bit to the buffer
        /// </summary>
        /// <param name="Bit">The bit to write</param>
        /// <param name="State">The new state for the bit</param>
        private void _Write(uint Bit, bool State)
        {
            // Calculates the actual bit and actual byte
            byte BitNo = (byte)(Bit & 0x07);
            uint ByteNo = (uint)this._Data.Length - 1 - ((Bit - BitNo) / 8);
            byte BitMask = (byte)(1 << (7 - BitNo));
            // Now we'll apply the new state
            if ((this._Data[ByteNo] & BitMask) == BitMask && State == false) this._Data[ByteNo] -= BitMask;
            else if ((this._Data[ByteNo] & BitMask) != BitMask && State == true) this._Data[ByteNo] += BitMask;
        }

        /// <summary>
        /// Writes a byte to the buffer
        /// </summary>
        /// <param name="StartBit">The first bit to write</param>
        /// <param name="Data">The data to write</param>
        /// <param name="BitCount">The amount of bits to write</param>
        /// <param name="Inverted">When true, bits will be inverted</param>
        protected void WriteByte(uint StartBit, uint Data, int BitCount, bool Inverted = false)
        {
            for (int BitNo = BitCount - 1; BitNo >= 0; --BitNo)
            {
                int BitMask = 1 << BitNo;
                bool Value = (Data & BitMask) == BitMask;
                uint PinNo;
                if (Inverted)
                    PinNo = (uint)(StartBit + BitNo);
                else
                    PinNo = (uint)(StartBit + BitCount - 1 - BitNo);
                this._Write(PinNo, Value);
            }
            this._WriteSPI();
        }

        /// <summary>
        /// Reads a single bit
        /// </summary>
        /// <param name="Bit">The bit to read</param>
        /// <returns>The current state of the bit</returns>
        protected bool Read(uint Bit)
        {
            // Calculates the actual bit and actual byte
            byte BitNo = (byte)(Bit & 0x07);
            uint ByteNo = (uint)this._Data.Length - 1 - ((Bit - BitNo) / 8);
            byte BitMask = (byte)(1 << (7 - BitNo));
            // Gets the value of the bit
            return (this._Data[ByteNo] & BitMask) == BitMask;
        }
        #endregion

        #region "GPO Pins"
        /// <summary>GPO Port wrapper for the SPIShifterOut class</summary>
        protected class Ic74hc595GPOPort : IGPOPort
        {
            /// <summary>Reference to the main chain</summary>
            private Ic74hc595 _Chain;
            /// <summary>The number of the bit</summary>
            private uint _BitNo;

            /// <summary>True when the pin is high, false when low</summary>
            public bool State { get; protected set; }

            /// <summary>
            /// Defines a GPO Port
            /// </summary>
            /// <param name="MainChain">The object of the main chain</param>
            /// <param name="BitNo">The number of the bit</param>
            public Ic74hc595GPOPort(Ic74hc595 MainChain, uint BitNo)
            {
                // Copies the parameters to local values
                this._Chain = MainChain;
                this._BitNo = BitNo;
            }

            /// <summary>
            /// Writes the pin value
            /// </summary>
            /// <param name="State">True for high, false for low</param>
            public void Write(bool State)
            {
                this._Chain.Write(this._BitNo, State);
                this.State = State;
            }

            /// <summary>
            /// Frees the pin
            /// </summary>
            public void Dispose() { }
        }

        /// <summary>Reference to all pins</summary>
        public IGPOPort[] Pins;
        #endregion

        #region "Parallel Out support"
        /// <summary>
        /// Parallel Out class
        /// </summary>
        protected class Ic74hc595ParallelOut : IParallelOut
        {
            /// <summary>Reference to the main chain</summary>
            private Ic74hc595 _Chain;
            /// <summary>The bit to start at</summary>
            private uint _StartBit;
            /// <summary>The amount of bits in this chain</summary>
            private uint _BitCount;
            /// <summary>The buffer of the data</summary>
            private uint _Buffer = 0;
            /// <summary>When true, bits will be inverted</summary>
            private bool _Inverted;
            /// <summary>Frees the pin for other usage</summary>
            public void Dispose() { }

            /// <summary>Initialises a new parallel output port</summary>
            /// <param name="MainChain">The object of the main chain</param>
            /// <param name="StartBit">The first bit to write</param>
            /// <param name="BitCount">The amount of bits to write</param>
            /// <param name="Inverted">When true, bits will be inverted</param>
            public Ic74hc595ParallelOut(Ic74hc595 MainChain, uint StartBit, uint BitCount, bool Inverted)
            {
                this._Chain = MainChain;
                this._StartBit = StartBit;
                this._BitCount = BitCount;
                this._Inverted = Inverted;
            }

            /// <summary>Returns the last written block of data</summary>
            /// <returns>The last written block of data</returns>
            public uint Read()
            {
                return this._Buffer;
            }

            /// <summary>Amount of bits in the array</summary>
            public uint Size { get { return this._BitCount; } }

            /// <summary>Writes a block of data to the array</summary>
            /// <param name="Value">The block of data to write</param>
            public void Write(uint Value)
            {
                this._Buffer = Value;
                this._Chain.WriteByte(this._StartBit, this._Buffer, (int)this._BitCount, this._Inverted);
            }
        }

        /// <summary>
        /// Creates a new parallel output port on this IC chain
        /// </summary>
        /// <param name="StartBit">The first bit to write to</param>
        /// <param name="BitCount">The amount of bits</param>
        /// <param name="Inverted">When true, bits will be inverted</param>
        /// <returns>Parallel output port object</returns>
        public IParallelOut CreateParallelOut(uint StartBit = 0, uint BitCount = 8, bool Inverted = false)
        {
            return new Ic74hc595ParallelOut(this, StartBit, BitCount, Inverted);
        }
        #endregion
    }
}
