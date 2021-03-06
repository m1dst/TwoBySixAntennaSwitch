﻿using System;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using TwoBySixAntennaSwitch.Lcd;
using TwoBySixAntennaSwitch.ShiftRegister;

namespace TwoBySixAntennaSwitch
{
    public class Program
    {

        /*
         * NETDUINO PORTS
         * ===============================
         * SCL: EEPROM
         * SDA: EEPROM
         * D13: SH_CP (4 x 74HC595, 2 x IC74HC165) (clock)
         * D12: (2 x IC74HC165) (data)
         * D11: DS (4 x 74HC595) (data in)
         * D10: (2 x IC74HC165) (latch)
         *  D9: ST_CP (4 x 74HC595) (latch)
         *  D8:
         *  D7: TX3 (Configuration) Bluetooth?
         *  D6: TX3 (Configuration) Bluetooth?
         *  D5: 
         *  D4: 
         *  D3: TX2 (RADIO B)
         *  D2: RX2 (RADIO B)
         *  D1: TX1 (RADIO A)
         *  D0: RX1 (RADIO A)
         *  A5:
         *  A4:
         *  A3: RADIO A (Icom voltage in)
         *  A2: RADIO B (Icom voltage in)
         *  A1:
         *  A0: BUTTON MATRIX (5 BUTTONS)
         * ===============================

         * 74HC595 Configuration
         * ========================================================================================================
         * 0: RADIO A (ANT1)         8: RADIO B (ANT1)        16: RADIO A (BPF1/BCD A)     24: RADIO A (BPF1/BCD A)
         * 1: RADIO A (ANT2)         9: RADIO B (ANT2)        17: RADIO A (BPF2/BCD B)     25: RADIO A (BPF2/BCD B)
         * 2: RADIO A (ANT3)        10: RADIO B (ANT3)        18: RADIO A (BPF3/BCD C)     26: RADIO A (BPF3/BCD C)
         * 3: RADIO A (ANT4)        11: RADIO B (ANT4)        19: RADIO A (BPF4/BCD D)     27: RADIO A (BPF4/BCD D)
         * 4: RADIO A (ANT5)        12: RADIO B (ANT5)        20: RADIO A (BPF5)           28: RADIO A (BPF5)
         * 5: RADIO A (ANT6)        13: RADIO B (ANT6)        21: RADIO A (BPF6)           29: RADIO A (BPF6)
         * 6: RADIO A (TXInhibit)   14: RADIO B (TXInhibit)   22:                         30:
         * 7:                       15:                       23:                         31:

         ** IC74HC165 Configuration
         * ==========================
         * 0: RADIO A (BCD A)
         * 1: RADIO A (BCD B)
         * 2: RADIO A (BCD C)
         * 3: RADIO A (BCD D)
         * 4: RADIO B (BCD A)
         * 5: RADIO B (BCD B)
         * 6: RADIO B (BCD C)
         * 7: RADIO B (BCD D)
         */

        const int MAX_ANTENNA_NAME_LENGTH = 11;
        const int MAX_ANTENNA_MASK_LENGTH = 6;

        private const int PIN_RADIOA_ANT1 = 0;
        private const int PIN_RADIOA_ANT2 = 1;
        private const int PIN_RADIOA_ANT3 = 2;
        private const int PIN_RADIOA_ANT4 = 3;
        private const int PIN_RADIOA_ANT5 = 4;
        private const int PIN_RADIOA_ANT6 = 5;
        private const int PIN_RADIOA_TXINHIBIT = 6;
        private const int PIN_SPARE_7 = 7;
        private const int PIN_RADIOB_ANT1 = 8;
        private const int PIN_RADIOB_ANT2 = 9;
        private const int PIN_RADIOB_ANT3 = 10;
        private const int PIN_RADIOB_ANT4 = 11;
        private const int PIN_RADIOB_ANT5 = 12;
        private const int PIN_RADIOB_ANT6 = 13;
        private const int PIN_RADIOB_TXINHIBIT = 14;
        private const int PIN_SPARE_15 = 15;
        private const int PIN_RADIOA_BPF1 = 16;
        private const int PIN_RADIOA_BPF2 = 17;
        private const int PIN_RADIOA_BPF3 = 18;
        private const int PIN_RADIOA_BPF4 = 19;
        private const int PIN_RADIOA_BPF5 = 20;
        private const int PIN_RADIOA_BPF6 = 21;
        private const int PIN_SPARE_22 = 22;
        private const int PIN_SPARE_23 = 23;
        private const int PIN_RADIOB_BPF1 = 24;
        private const int PIN_RADIOB_BPF2 = 25;
        private const int PIN_RADIOB_BPF3 = 26;
        private const int PIN_RADIOB_BPF4 = 27;
        private const int PIN_RADIOB_BPF5 = 28;
        private const int PIN_RADIOB_BPF6 = 29;
        private const int PIN_SPARE_30 = 30;
        private const int PIN_SPARE_31 = 31;

        static Lcd.Lcd _lcd;
        static Antenna[] _antennas;
        static Radio[] _radios;

        static readonly OutputPort Led1 = new OutputPort(Pins.ONBOARD_LED, false);
        private static readonly Ic74hc595 IcChain = new Ic74hc595(SPI_Devices.SPI1, Pins.GPIO_PIN_D9, 2);

        //static readonly I2CBus CommonI2CBus = new I2CBus();
        //static readonly Eeprom.Eeprom Eeprom = new Eeprom.Eeprom(TwoBySixAntennaSwitch.Eeprom.Eeprom.IC._24LC256, CommonI2CBus, 0, 100) { BigEndian = true };
        static readonly I2CBus CommonI2CBus = null;
        static readonly Eeprom.Eeprom Eeprom = null;

        private static InputPort RadioA_BCD_A;
        private static InputPort RadioA_BCD_B;
        private static InputPort RadioA_BCD_C;
        private static InputPort RadioA_BCD_D;


        private static bool _showWelcomeMessage = true;
        private static bool _showMainMenu = true;

        private static SerialUserInterface _serialUI = new SerialUserInterface();

        private const string Divider = "-------------------------------------------------------------\r\n";



        public static void Main()
        {

            for (var counter = (IcChain.Pins.Length / 2) - 1; counter >= 0; --counter)
            {
                IcChain.Pins[counter].Write(true);
                Thread.Sleep(25);
                IcChain.Pins[counter].Write(false);
            }
            for (var counter = IcChain.Pins.Length - 1; counter >= IcChain.Pins.Length / 2; --counter)
            {
                IcChain.Pins[counter].Write(true);
                Thread.Sleep(25);
                IcChain.Pins[counter].Write(false);
            }
            for (var counter = IcChain.Pins.Length / 2; counter <= IcChain.Pins.Length - 1; ++counter)
            {
                IcChain.Pins[counter].Write(true);
                Thread.Sleep(25);
                IcChain.Pins[counter].Write(false);
            }
            for (var counter = 0; counter <= (IcChain.Pins.Length / 2) - 1; ++counter)
            {
                IcChain.Pins[counter].Write(true);
                Thread.Sleep(25);
                IcChain.Pins[counter].Write(false);
            }

            _lcd = new Lcd.Lcd(0x27, 20, 4);

            RadioA_BCD_A = new InputPort(Pins.GPIO_PIN_D5, false, Port.ResistorMode.PullUp);
            RadioA_BCD_B = new InputPort(Pins.GPIO_PIN_D6, false, Port.ResistorMode.PullUp);
            RadioA_BCD_C = new InputPort(Pins.GPIO_PIN_D7, false, Port.ResistorMode.PullUp);
            RadioA_BCD_D = new InputPort(Pins.GPIO_PIN_D8, false, Port.ResistorMode.PullUp);

            _antennas = new Antenna[6];

            _radios = new Radio[2]
            {
                new Radio(),
                new Radio()
            };

            // I've commented out the write functions which will need to be uncommented when the eeprom is reconnected.
            FactoryReset();

            _radios[1].CurrentBand = RadioBand.B10;
            _radios[1].RadioState = RadioState.Tx;
            _radios[0].CurrentAntenna = 0;
            _radios[1].CurrentAntenna = 5;

            //CheckIfWeNeedToFactoryResetTheEeprom();
            //ReadConfigurationFromEeprom();
            DisplaySplash();
            Random rnd = new Random();

            do
            {
                if (_serialUI.SerialErrorReceived)
                {
                    _serialUI.Dispose();
                    _serialUI = new SerialUserInterface();
                    _showMainMenu = true;
                }

                if (_showMainMenu)
                {
                    _showMainMenu = false;
                    MainMenu(null);
                }

                int counter = rnd.Next(15);
                bool isOn = (DateTime.Now.Millisecond % 2 == 0);
                IcChain.Pins[counter].Write(isOn);
                counter = rnd.Next(15);
                isOn = (DateTime.Now.Millisecond + 1 % 2 == 0);
                IcChain.Pins[counter].Write(isOn);
                counter = rnd.Next(15);
                isOn = (DateTime.Now.Millisecond + 2 % 2 == 0);
                IcChain.Pins[counter].Write(isOn);
                counter = rnd.Next(15);
                isOn = (DateTime.Now.Millisecond + 3 % 2 == 0);
                IcChain.Pins[counter].Write(isOn);

                CheckRadioABand();

                InhibitRadiosIfRequired();
                UpdateDisplay();
                Thread.Sleep(50);
            }
            while (true);

        }

        private static void CheckRadioABand()
        {
            string bcd = !RadioA_BCD_A.Read() ? "1" : "0";
            bcd += !RadioA_BCD_B.Read() ? "1" : "0";
            bcd += !RadioA_BCD_C.Read() ? "1" : "0";
            bcd += !RadioA_BCD_D.Read() ? "1" : "0";
            _radios[0].CurrentBand = Utilities.BcdStringToBand(bcd);
        }

        private static void ReadConfigurationFromEeprom()
        {
            // Loop through the six antennas and load the name and mask into memory.
            for (var i = 0; i < 6; i++)
            {
                var address = (i + 1) * 100; // Starting address.
                _antennas[i] = new Antenna
                {
                    Name = Eeprom.ReadString(address, MAX_ANTENNA_NAME_LENGTH),
                    BandMask = new BandMask(Eeprom.ReadString((address + MAX_ANTENNA_NAME_LENGTH + 1), MAX_ANTENNA_MASK_LENGTH))
                };
            }

            // Loop through the two radios and load their config.
            for (var i = 0; i < 2; i++)
            {
                var address = (i + 1) * 10;
                _radios[i].BandDecodingMethod = (BandDecodingMethod)Eeprom.ReadInt16(address);
                _radios[i].BandPassFilterType = (BandPassFilterType)Eeprom.ReadInt16(address + 1);

                // Read the BandDecodingMethod in the eeprom.
                Eeprom.WriteInt16(address, (int)_radios[i].BandDecodingMethod);

                // Read the BandPassFilterType in the eeprom.
                Eeprom.WriteInt16((address + 1), (int)_radios[i].BandPassFilterType);

            }
        }

        /// <summary>
        /// Checks whether the eeprom is programmed correctly and clears it if now.
        /// </summary>
        private static void CheckIfWeNeedToFactoryResetTheEeprom()
        {
            // We need to check if the eeprom has been programmed before.
            // When it is programmed we write 255 to address zero of the eeprom.
            // If we read byte zero and don't get 255 then we should factory reset.

            var b = Eeprom.ReadByte(0);
            if (b != 255)
            {
                FactoryReset();
            }
        }

        private static void InhibitRadiosIfRequired()
        {
            _radios[0].RadioState = GetCountOfSuitableAntennas(_radios[0].CurrentBand) == 0 ? RadioState.Inhibit : RadioState.Rx;
            IcChain.Pins[PIN_RADIOA_TXINHIBIT].Write(_radios[0].RadioState == RadioState.Inhibit);

            _radios[1].RadioState = GetCountOfSuitableAntennas(_radios[1].CurrentBand) == 0 ? RadioState.Inhibit : RadioState.Rx;
            IcChain.Pins[PIN_RADIOB_TXINHIBIT].Write(_radios[1].RadioState == RadioState.Inhibit);

        }


        public static void MainMenu(SerialInputItem item)
        {
            _serialUI.Stop();

            if (_showWelcomeMessage)
            {
                _serialUI.AddDisplayItem("Welcome to the M1DST 2x6 Antenna Switcher\r\n");
                _serialUI.AddDisplayItem(Divider);
                _serialUI.AddDisplayItem("\r\n");
                _serialUI.Go();

            }
            _serialUI.Stop();
            _serialUI.AddDisplayItem("Main Menu:\r\n");
            _serialUI.AddDisplayItem("==========\r\n\r\n");

            _serialUI.AddInputItem(new SerialInputItem { Option = "1", Label = ": Configure Antenna 1", Callback = ConfigureAntenna, Context = 1 });
            _serialUI.AddInputItem(new SerialInputItem { Option = "2", Label = ": Configure Antenna 2", Callback = ConfigureAntenna, Context = 2 });
            _serialUI.AddInputItem(new SerialInputItem { Option = "3", Label = ": Configure Antenna 3", Callback = ConfigureAntenna, Context = 3 });
            _serialUI.AddInputItem(new SerialInputItem { Option = "4", Label = ": Configure Antenna 4", Callback = ConfigureAntenna, Context = 4 });
            _serialUI.AddInputItem(new SerialInputItem { Option = "5", Label = ": Configure Antenna 5", Callback = ConfigureAntenna, Context = 5 });
            _serialUI.AddInputItem(new SerialInputItem { Option = "6", Label = ": Configure Antenna 6", Callback = ConfigureAntenna, Context = 6 });
            _serialUI.AddInputItem(new SerialInputItem());

            _serialUI.AddInputItem(new SerialInputItem { Option = "A", Label = ": Configure Radio A", Callback = ConfigureRadio, Context = 1 });
            _serialUI.AddInputItem(new SerialInputItem { Option = "B", Label = ": Configure Radio B", Callback = ConfigureRadio, Context = 2 });

            _serialUI.AddInputItem(new SerialInputItem());
            _serialUI.AddInputItem(new SerialInputItem { Option = "S", Label = ": Show Status", Callback = ShowStatus });
            _serialUI.AddInputItem(new SerialInputItem { Option = "H", Label = ": Show Help", Callback = DisplayHelp });
            _serialUI.AddInputItem(new SerialInputItem { Option = "R", Label = ": Factory Reset", Callback = FactoryReset });

            _serialUI.AddInputItem(new SerialInputItem { Callback = RefreshMainMenu });

            _serialUI.Go();
            _showWelcomeMessage = false;
        }

        public static void DisplayHelp()
        {
            _serialUI.DisplayLine("\r\n\r\n");

            _serialUI.DisplayLine("Heeeeeeeeeeeeeeelp!");
            _serialUI.DisplayLine("===================\r\n");

            _serialUI.DisplayLine("There are two configuration items per antenna. The first being");
            _serialUI.DisplayLine("the name.  This should be meaningful and ideally no longer than");
            _serialUI.DisplayLine("11 characters.  If you enter a name which is longer it will be");
            _serialUI.DisplayLine("truncated automatically.\r\n");

            _serialUI.DisplayLine("Antenna Matrix Help");
            _serialUI.DisplayLine("===================\r\n");
            _serialUI.DisplayLine("An antenna matrix is a string of numbers with a length of 6.");
            _serialUI.DisplayLine("It is made up of numbers between 0 and 2 for each.");
            _serialUI.DisplayLine("Each of the six numbers represents one of the main contest bands.\r\n");
            _serialUI.DisplayLine(" * 0 means that the antenna is NOT suitable for the band.");
            _serialUI.DisplayLine(" * 1 means that the antenna IS suitable for the band.");
            _serialUI.DisplayLine(" * 2 means that the antenna IS suitable for the band and should have priority.\r\n");
            _serialUI.DisplayLine("You might have multiple antennas which work on the same bands.");
            _serialUI.DisplayLine("Some will be better than others on certain bands.");
            _serialUI.DisplayLine("\r\nExamples");
            _serialUI.DisplayLine("========\r\n");
            _serialUI.DisplayLine(" * 20/15/10 triband might be :     000221");
            _serialUI.DisplayLine(" * 160M dipole might be :          200000");
            _serialUI.DisplayLine(" * 80/40M nested dipole might be : 022000");
            _serialUI.DisplayLine(" * 10M 5 element yagi might be :   000002");
            _serialUI.DisplayLine(" * 80M-10M vertical might be :     011111");
            _serialUI.DisplayLine(" * No antenna connected would be : 000000");
            _serialUI.DisplayLine("\r\nThe example above gave a higher priority to the 10M 5ele than the triband.\r\n");

            _serialUI.DisplayLine("Radios");
            _serialUI.DisplayLine("======\r\n");
            _serialUI.DisplayLine("You will need to configure the method that this unit will use to decode");
            _serialUI.DisplayLine("which band each antenna is on.\r\n");
            _serialUI.DisplayLine("If you have bandpass filters you can trigger them from this unit too.");
            _serialUI.DisplayLine("You will just need to configure the output type your BPF requires.\r\n\r\n");

        }

        public static void RefreshMainMenu(SerialInputItem item)
        {
            _showMainMenu = true;
        }

        private static void ShowStatus(SerialInputItem inputItem)
        {
            _serialUI.DisplayLine("\r\n\r\nAntenna and Radio Status");
            _serialUI.DisplayLine("========================\r\n");

            _serialUI.DisplayLine("ANTENNA NAME   160 80  40  20  15  10 ");
            _serialUI.DisplayLine("======================================");
            for (int i = 0; i < _antennas.Length; i++)
            {
                _serialUI.Display(i + 1 + ") " + StringExtension.PadRight(_antennas[i].Name, MAX_ANTENNA_NAME_LENGTH) + " ");
                string mask = _antennas[i].BandMask.ToString();
                mask = mask.Replace("0", "-   ").Replace("1", "1   ").Replace("2", "2   ");
                _serialUI.Display(mask + "\r\n");
            }
            _serialUI.DisplayLine("======================================");

            _serialUI.DisplayLine("\r\n");

            _serialUI.DisplayLine("Radio A: " + Utilities.BandDecodingMethodToString(_radios[0].BandDecodingMethod));
            _serialUI.DisplayLine("  BPF A: " + Utilities.BandPassFilterTypeToString(_radios[0].BandPassFilterType) + "\r\n");
            _serialUI.DisplayLine("Radio B: " + Utilities.BandDecodingMethodToString(_radios[1].BandDecodingMethod));
            _serialUI.DisplayLine("  BPF B: " + Utilities.BandPassFilterTypeToString(_radios[1].BandPassFilterType) + "\r\n");

            _serialUI.DisplayLine("\r\n");

            RefreshMainMenu(null);
        }

        private static void FactoryReset()
        {

            _antennas = new Antenna[6];
            for (var i = 0; i < _antennas.Length; i++)
            {
                _antennas[i] = new Antenna { Name = "ANTENNA " + (i + 1), BandMask = new BandMask("000000") };

                var address = (i + 1) * 100;
                //Eeprom.WriteString((address), _antennas[i].Name);
                //Eeprom.WriteString((address + MAX_ANTENNA_NAME_LENGTH + 1), _antennas[i].BandMask.ToString());
            }

            _radios = new[]
            {
                new Radio { BandDecodingMethod = BandDecodingMethod.YaesuBcd, BandPassFilterType = BandPassFilterType.None},
                new Radio { BandDecodingMethod = BandDecodingMethod.YaesuBcd, BandPassFilterType = BandPassFilterType.None},
            };

            for (var i = 0; i < _radios.Length; i++)
            {
                var address = (i + 1) * 10;

                // Store the BandDecodingMethod in the eeprom.
                //Eeprom.WriteInt16(address, (int)_radios[i].BandDecodingMethod);

                // Store the BandPassFilterType in the eeprom.
                //Eeprom.WriteInt16(address + 1, (int)_radios[i].BandPassFilterType);
            }

            // Write 255 to address zero on the eeprom to confirm it has been configured.
            //Eeprom.WriteByte(0, 255);

        }

        private static void FactoryReset(SerialInputItem inputItem)
        {
            _serialUI.Stop();
            _serialUI.DisplayLine("Resetting...");

            FactoryReset();

            _serialUI.DisplayLine("Done...");
            _serialUI.DisplayLine("\r\n");

            RefreshMainMenu(null);

            _serialUI.Go();
        }

        private static void ConfigureAntenna(SerialInputItem inputItem)
        {
            _serialUI.Stop();
            _serialUI.DisplayLine("Configuring Antenna " + inputItem.Context);
            _serialUI.DisplayLine("=====================\r\n");
            _serialUI.AddInputItem(new SerialInputItem { Option = "N", Label = ": Configure Antenna Name", Callback = ConfigureAntennaName, Context = inputItem.Context });
            _serialUI.AddInputItem(new SerialInputItem { Option = "M", Label = ": Configure Antenna Mask", Callback = ConfigureAntennaMask, Context = inputItem.Context });
            _serialUI.AddInputItem(new SerialInputItem { Option = "..", Label = ": Back to the Main Menu", Callback = RefreshMainMenu });
            _serialUI.AddInputItem(new SerialInputItem { Callback = ConfigureAntenna, Context = inputItem.Context });

            _serialUI.Go();
        }

        private static void ConfigureRadio(SerialInputItem inputItem)
        {
            _serialUI.Stop();

            _serialUI.DisplayLine("Configuring Radio " + inputItem.Context);
            _serialUI.DisplayLine("=====================\r\n");
            _serialUI.AddInputItem(new SerialInputItem { Option = "D", Label = ": Configure Band Decoder", Callback = ConfigureBandDecoder, Context = inputItem.Context });
            _serialUI.AddInputItem(new SerialInputItem { Option = "B", Label = ": Configure Band Pass Filter Output", Callback = ConfigureBandPassFilter, Context = inputItem.Context });
            _serialUI.AddInputItem(new SerialInputItem { Option = "..", Label = ": Back to the Main Menu", Callback = RefreshMainMenu });
            _serialUI.AddInputItem(new SerialInputItem { Callback = ConfigureRadio, Context = inputItem.Context });

            _serialUI.Go();
        }

        private static void ConfigureAntennaName(SerialInputItem inputItem)
        {
            _serialUI.Stop();
            int antenna = 0;

            switch (inputItem.Context)
            {
                // Prompt user to see if we're gonna change the name.
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                    antenna = inputItem.Context;
                    _serialUI.Store.Clear();
                    _serialUI.DisplayLine("\r\nWe're now going to configure antenna " + antenna + ".");
                    _serialUI.DisplayLine("\r\nThe current name for the antenna is : " + _antennas[antenna - 1].Name);
                    _serialUI.DisplayLine("\r\nPlease enter a new name for the antenna. (Maximum " + MAX_ANTENNA_NAME_LENGTH + " characters)");
                    _serialUI.DisplayLine("Pressing ENTER leaves the current name configured.");
                    _serialUI.AddInputItem(new SerialInputItem { Label = "", Callback = ConfigureAntennaName, Context = inputItem.Context + 10, StoreKey = "name" });
                    break;

                // Check the change name response.
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:

                    antenna = inputItem.Context - 10;
                    if (_serialUI.Store["name"].ToString() != "")
                    {
                        var name = _serialUI.Store["name"].ToString().Trim().ToUpper();
                        if (name.Length > MAX_ANTENNA_NAME_LENGTH)
                        {
                            name = name.Substring(0, MAX_ANTENNA_NAME_LENGTH);
                        }
                        _antennas[antenna - 1].Name = name;
                        Eeprom.WriteString(antenna * 100, name);
                        _serialUI.DisplayLine("\r\nName changed to : " + _antennas[antenna - 1].Name + "\r\n\r\n");
                    }
                    else
                    {
                        _serialUI.DisplayLine("\r\nName NOT changed.   (" + _antennas[antenna - 1].Name + ")\r\n\r\n");
                    }

                    _serialUI.Stop();
                    ConfigureAntenna(new SerialInputItem { Context = antenna });
                    return;

            }

            _serialUI.Go();

        }

        private static void ConfigureBandDecoder(SerialInputItem inputItem)
        {
            _serialUI.Stop();
            int radio = 0;

            switch (inputItem.Context)
            {
                // Prompt user to see if we're gonna change the decoder type.
                case 1:
                case 2:
                    radio = inputItem.Context;
                    _serialUI.Store.Clear();
                    _serialUI.DisplayLine("\r\nWe're now going to configure the band decoder for radio " + radio + ".");
                    _serialUI.DisplayLine("\r\nThe current configuration is : " + Utilities.BandDecodingMethodToString(_radios[radio - 1].BandDecodingMethod) + "\r\n");
                    _serialUI.DisplayLine("Please select a new option : \r\n");

                    _serialUI.AddInputItem(new SerialInputItem { Option = "0", Label = ": " + Utilities.BandDecodingMethodToString(BandDecodingMethod.YaesuBcd), Callback = ConfigureBandDecoder, Context = inputItem.Context + 10, StoreKey = "band_decoder" });
                    _serialUI.AddInputItem(new SerialInputItem { Option = "1", Label = ": " + Utilities.BandDecodingMethodToString(BandDecodingMethod.IcomVoltage), Callback = ConfigureBandDecoder, Context = inputItem.Context + 10, StoreKey = "band_decoder" });
                    _serialUI.AddInputItem(new SerialInputItem { Option = "2", Label = ": " + Utilities.BandDecodingMethodToString(BandDecodingMethod.Civ), Callback = ConfigureBandDecoder, Context = inputItem.Context + 10, StoreKey = "band_decoder" });
                    _serialUI.AddInputItem(new SerialInputItem { Option = "3", Label = ": " + Utilities.BandDecodingMethodToString(BandDecodingMethod.Kenwood), Callback = ConfigureBandDecoder, Context = inputItem.Context + 10, StoreKey = "band_decoder" });
                    _serialUI.AddInputItem(new SerialInputItem { Option = "..", Label = ": Back to the Main Menu", Callback = RefreshMainMenu });
                    _serialUI.AddInputItem(new SerialInputItem { Callback = ConfigureBandDecoder, Context = inputItem.Context });
                    break;

                // Check the response.
                case 11:
                case 12:

                    radio = inputItem.Context - 10;
                    _radios[radio - 1].BandDecodingMethod = (BandDecodingMethod)Convert.ToInt16(_serialUI.Store["band_decoder"].ToString());

                    var address = radio * 10;
                    Eeprom.WriteInt16(address, (int)_radios[radio - 1].BandDecodingMethod);

                    _serialUI.DisplayLine("\r\nBand Decoder Type changed to : " + Utilities.BandDecodingMethodToString(_radios[radio - 1].BandDecodingMethod) + "\r\n\r\n");

                    _serialUI.Stop();
                    ConfigureRadio(new SerialInputItem { Context = radio });
                    return;

            }

            _serialUI.Go();

        }

        private static void ConfigureBandPassFilter(SerialInputItem inputItem)
        {
            _serialUI.Stop();
            int radio = 0;

            switch (inputItem.Context)
            {
                // Prompt user to see if we're gonna change the bandpass filter output type.
                case 1:
                case 2:
                    radio = inputItem.Context;
                    _serialUI.Store.Clear();
                    _serialUI.DisplayLine("\r\nWe're now going to configure the BPF for radio " + radio + ".");
                    _serialUI.DisplayLine("\r\nThe current configuration is : " + Utilities.BandPassFilterTypeToString(_radios[radio - 1].BandPassFilterType) + "\r\n");
                    _serialUI.DisplayLine("Please select a new option : \r\n");

                    _serialUI.AddInputItem(new SerialInputItem { Option = "0", Label = ": " + Utilities.BandPassFilterTypeToString(BandPassFilterType.None), Callback = ConfigureBandPassFilter, Context = inputItem.Context + 10, StoreKey = "bpf" });
                    _serialUI.AddInputItem(new SerialInputItem { Option = "1", Label = ": " + Utilities.BandPassFilterTypeToString(BandPassFilterType.YaesuBCD), Callback = ConfigureBandPassFilter, Context = inputItem.Context + 10, StoreKey = "bpf" });
                    _serialUI.AddInputItem(new SerialInputItem { Option = "2", Label = ": " + Utilities.BandPassFilterTypeToString(BandPassFilterType.SeparateBands), Callback = ConfigureBandPassFilter, Context = inputItem.Context + 10, StoreKey = "bpf" });
                    _serialUI.AddInputItem(new SerialInputItem { Option = "..", Label = ": Back to the Main Menu", Callback = RefreshMainMenu });
                    _serialUI.AddInputItem(new SerialInputItem { Callback = ConfigureBandPassFilter, Context = inputItem.Context });
                    break;

                // Check the response.
                case 11:
                case 12:

                    radio = inputItem.Context - 10;
                    _radios[radio - 1].BandPassFilterType = (BandPassFilterType)Convert.ToInt16(_serialUI.Store["bpf"].ToString());

                    var address = (radio * 10) + 1;
                    Eeprom.WriteInt16(address, (int)_radios[radio - 1].BandPassFilterType);

                    _serialUI.DisplayLine("\r\nBPF output changed to : " + Utilities.BandPassFilterTypeToString(_radios[radio - 1].BandPassFilterType) + "\r\n\r\n");

                    _serialUI.Stop();
                    ConfigureRadio(new SerialInputItem { Context = radio });
                    return;

            }

            _serialUI.Go();

        }

        private static void DisplayHelp(SerialInputItem inputItem)
        {
            _serialUI.Stop();
            DisplayHelp();
            RefreshMainMenu(null);
            _serialUI.Go();
        }

        private static void ConfigureAntennaMask(SerialInputItem inputItem)
        {
            _serialUI.Stop();
            int antenna;

            switch (inputItem.Context)
            {
                // Prompt user to see if we're gonna change the mask.
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:

                    antenna = inputItem.Context;
                    _serialUI.Store.Clear();
                    _serialUI.DisplayLine("\r\nWe're now going to configure antenna " + antenna + ".");
                    _serialUI.DisplayLine("\r\nThe current mask for the antenna is : " + _antennas[antenna - 1].BandMask.ToString());
                    _serialUI.DisplayLine("\r\nPlease enter a new mask for the antenna. (Maximum 6 characters)");
                    _serialUI.DisplayLine("Pressing ENTER leaves the current mask configured.");
                    _serialUI.AddInputItem(new SerialInputItem { Label = "", Callback = ConfigureAntennaMask, Context = inputItem.Context + 10, StoreKey = "mask" });
                    break;

                // Check the change mask response.
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:

                    antenna = inputItem.Context - 10;
                    if (_serialUI.Store["mask"].ToString() != "")
                    {
                        _antennas[antenna - 1].BandMask = new BandMask(_serialUI.Store["mask"].ToString());
                        Eeprom.WriteString(((antenna * 100) + MAX_ANTENNA_NAME_LENGTH + 1), _antennas[antenna - 1].BandMask.ToString());
                        _serialUI.DisplayLine("\r\nMask changed to : " + _antennas[antenna - 1].BandMask.ToString() + "\r\n\r\n");
                    }
                    else
                    {
                        _serialUI.DisplayLine("\r\nMask NOT changed.   (" + _antennas[antenna - 1].BandMask + ")\r\n\r\n");
                    }

                    _serialUI.Stop();
                    ConfigureAntenna(new SerialInputItem { Context = antenna });
                    return;
            }

            _serialUI.Go();

        }


        static void UpdateDisplay()
        {
            _lcd.WriteLine(0, "Radio A : " + Utilities.RadioStateToString(_radios[0].RadioState));
            _lcd.WriteLine(1, Utilities.RadioBandToString(_radios[0].CurrentBand).PadLeft(4) + " " + GetCountOfSuitableAntennas(_radios[0].CurrentBand) + " " + _antennas[_radios[0].CurrentAntenna].Name.PadRight(MAX_ANTENNA_NAME_LENGTH + 1) + (_radios[0].CurrentAntenna + 1));
            _lcd.WriteLine(2, "Radio B : " + Utilities.RadioStateToString(_radios[1].RadioState));
            _lcd.WriteLine(3, Utilities.RadioBandToString(_radios[1].CurrentBand).PadLeft(4) + " " + GetCountOfSuitableAntennas(_radios[1].CurrentBand) + " " + _antennas[_radios[1].CurrentAntenna].Name.PadRight(MAX_ANTENNA_NAME_LENGTH + 1) + (_radios[1].CurrentAntenna + 1));
        }

        static void DisplaySplash()
        {
            _lcd.SetBacklight(true);
            _lcd.Clear();
            _lcd.WriteLine(0, "M1DST 2 x 6", TextAlign.Centre);
            _lcd.WriteLine(1, "ANTENNA SWITCH", TextAlign.Centre);
            _lcd.WriteLine(2, "Radio A: " + Utilities.BandDecodingMethodToString(_radios[0].BandDecodingMethod));
            _lcd.WriteLine(3, "Radio B: " + Utilities.BandDecodingMethodToString(_radios[1].BandDecodingMethod));
            Thread.Sleep(2000);
            _lcd.Clear();
        }

        static int ConvertYaesuBcdToBand(string input)
        {
            int output = 0;
            if (input[0] == '1') output += 1;
            if (input[1] == '1') output += 2;
            if (input[2] == '1') output += 4;
            if (input[3] == '1') output += 8;
            return output;
        }

        static int GetCountOfSuitableAntennas(RadioBand band)
        {
            //TODO: Should probably take into account which antennas are in use?

            int result = 0;
            switch (band)
            {
                case RadioBand.B10:

                    foreach (var antenna in _antennas)
                    {
                        if (antenna.BandMask.Is10M) { result++; }
                    }
                    return result;

                case RadioBand.B15:

                    foreach (var antenna in _antennas)
                    {
                        if (antenna.BandMask.Is15M) { result++; }
                    }
                    return result;

                case RadioBand.B20:

                    foreach (var antenna in _antennas)
                    {
                        if (antenna.BandMask.Is20M) { result++; }
                    }
                    return result;

                case RadioBand.B40:

                    foreach (var antenna in _antennas)
                    {
                        if (antenna.BandMask.Is40M) { result++; }
                    }
                    return result;

                case RadioBand.B80:

                    foreach (var antenna in _antennas)
                    {
                        if (antenna.BandMask.Is80M) { result++; }
                    }
                    return result;

                case RadioBand.B160:

                    foreach (var antenna in _antennas)
                    {
                        if (antenna.BandMask.Is160M) { result++; }
                    }
                    return result;
            }

            return result;
        }


    }
}
