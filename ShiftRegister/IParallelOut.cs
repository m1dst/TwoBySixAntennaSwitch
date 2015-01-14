using System;

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
    /// <summary>Generic Purpose Output Array interface</summary>
    public interface IParallelOut : IDisposable
    {
        /// <summary>Amount of bits in the array</summary>
        uint Size { get; }

        /// <summary>Writes a block of data to the array</summary>
        /// <param name="Value">The block of data to write</param>
        void Write(uint Value);

        /// <summary>Returns the last written block of data</summary>
        /// <returns>The last written block of data</returns>
        uint Read();
    }
}