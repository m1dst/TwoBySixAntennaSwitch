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
    /// <summary>.NETMF OutputPort wrapper</summary>
    public class IntegratedGPO : IGPOPort
    {
        /// <summary>Reference to the GPO port</summary>
        private OutputPort _Port;

        /// <summary>True when the pin is high, false when low</summary>
        public bool State { get; protected set; }

        /// <summary>
        /// Creates a new GPO Port
        /// </summary>
        /// <param name="Pin">The pin number</param>
        /// <param name="InitialState">It's initial state</param>
        public IntegratedGPO(Cpu.Pin Pin, bool InitialState = false)
        {
            this._Port = new OutputPort(Pin, InitialState);
        }

        /// <summary>Writes the pin value</summary>
        /// <param name="State">True for high, false for low</param>
        public void Write(bool State)
        {
            this._Port.Write(State);
            this.State = State;
        }

        /// <summary>
        /// Disposes this object
        /// </summary>
        public void Dispose()
        {
            this._Port.Dispose();
        }
    }
}
