using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using TwoBySixAntennaSwitch.Lcd;

namespace TwoBySixAntennaSwitch
{
    public class Program
    {

        const int MAX_ANTENNA_NAME_LENGTH = 11;

        static DfRobotLcdShield _lcdshield;
        static Antenna[] _antennas;
        static Radio[] _radios;

        static readonly OutputPort Led1 = new OutputPort(Pins.ONBOARD_LED, false);


        private static bool _showWelcomeMessage = true;

        static SerialUserInterface _serialUI = new SerialUserInterface();

        private const string Divider = "-------------------------------------------------------------\r\n";

        private static bool _showMainMenu = true;

        public static void Main()
        {
            _lcdshield = new DfRobotLcdShield(20, 4);
            _antennas = new Antenna[6];

            _radios = new Radio[2]
            {
                new Radio(),
                new Radio()
            };

            _radios[1].CurrentBand = RadioBand.B10;
            _radios[1].RadioState = RadioState.Tx;

            for (int i = 0; i < 6; i++)
            {
                _antennas[i] = new Antenna { Name = "ANTENNA " + (i + 1), BandMask = new BandMask("000000") };
            }

            DisplaySplash();

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

                UpdateDisplay();
                Thread.Sleep(200);
            }
            while (true);

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
            _serialUI.AddInputItem(new SerialInputItem { Callback = RefreshMainMenu });

            _serialUI.Go();
            _showWelcomeMessage = false;
        }

        public static void DisplayMatrixHelp()
        {
            _serialUI.DisplayLine("\r\n\r\nAntanna Matrix Help");
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
        }

        public static void RefreshMainMenu(SerialInputItem item)
        {
            _showMainMenu = true;
        }

        private static void ShowStatus(SerialInputItem inputItem)
        {
            _serialUI.DisplayLine("\r\n\r\nStatus");
            _serialUI.DisplayLine("======\r\n\r\n");

            _serialUI.DisplayLine("ANTENNA NAME   160 80  40  20  15  10 ");
            for (int i = 0; i < _antennas.Length; i++)
            {
                _serialUI.Display(i + 1 + ") " + StringExtension.PadRight(_antennas[i].Name, MAX_ANTENNA_NAME_LENGTH) + " ");
                string mask = _antennas[i].BandMask.ToString();
                mask = mask.Replace("0", "-   ").Replace("1", "1   ").Replace("2", "2   ");
                _serialUI.Display(mask + "\r\n");
            }
            _serialUI.DisplayLine("\r\n\r\n");

            // 160   1   0  0   0  1
            // 80    0   1  0   0  2
            // 40    1   1  0   0  3
            // 20    1   0  1   0  5
            // 15    1   1  1   0  7
            // 10    1   0  0   1  9

            _serialUI.DisplayLine(ConvertYaesuBcdToBand("1000").ToString());
            _serialUI.DisplayLine(ConvertYaesuBcdToBand("0100").ToString());
            _serialUI.DisplayLine(ConvertYaesuBcdToBand("1100").ToString());
            _serialUI.DisplayLine(ConvertYaesuBcdToBand("1010").ToString());
            _serialUI.DisplayLine(ConvertYaesuBcdToBand("1110").ToString());
            _serialUI.DisplayLine(ConvertYaesuBcdToBand("1001").ToString());

            RefreshMainMenu(null);
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
            _serialUI.AddInputItem(new SerialInputItem { Option = "N", Label = ": Configure Radio Name", Callback = ConfigureAntenna, Context = inputItem.Context });
            _serialUI.AddInputItem(new SerialInputItem { Option = "M", Label = ": Configure Radio Mask", Callback = ConfigureAntenna, Context = inputItem.Context });
            _serialUI.AddInputItem(new SerialInputItem { Option = "..", Label = ": Back to the Main Menu", Callback = RefreshMainMenu });
            _serialUI.AddInputItem(new SerialInputItem { Callback = ConfigureAntenna, Context = inputItem.Context });

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
                        _antennas[antenna - 1].Name = _serialUI.Store["name"].ToString().Trim().ToUpper().Substring(0, MAX_ANTENNA_NAME_LENGTH);
                        _serialUI.DisplayLine("\r\nName changed to : " + _antennas[antenna - 1].Name + "\r\n\r\n");
                    }
                    else
                    {
                        _serialUI.DisplayLine("\r\nName NOT changed.   (" + _antennas[antenna - 1].Name + ")\r\n\r\n");
                    }

                    _serialUI.Stop();
                    ConfigureAntenna(new SerialInputItem() { Context = antenna });
                    return;

            }

            _serialUI.Go();

        }

        private static void DisplayHelp(SerialInputItem inputItem)
        {
            _serialUI.Stop();
            DisplayMatrixHelp();
            RefreshMainMenu(null);
            _serialUI.Go();
        }

        private static void ConfigureAntennaMask(SerialInputItem inputItem)
        {
            _serialUI.Stop();
            int antenna = 0;

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
                        _serialUI.DisplayLine("\r\nMask changed to : " + _antennas[antenna - 1].BandMask.ToString() + "\r\n\r\n");
                    }
                    else
                    {
                        _serialUI.DisplayLine("\r\nMask NOT changed.   (" + _antennas[antenna - 1].BandMask + ")\r\n\r\n");
                    }

                    _serialUI.Stop();
                    ConfigureAntenna(new SerialInputItem() { Context = antenna });
                    return;
            }

            _serialUI.Go();

        }


        static void UpdateDisplay()
        {
            _lcdshield.WriteLine(0, "Radio A : " + Utilities.RadioStateToString(_radios[0].RadioState));
            _lcdshield.WriteLine(1, Utilities.RadioBandToString(_radios[0].CurrentBand).PadLeft(3) + " - " + _antennas[0].Name.PadRight(MAX_ANTENNA_NAME_LENGTH + 1) + "1");
            _lcdshield.WriteLine(2, "Radio B : " + Utilities.RadioStateToString(_radios[1].RadioState));
            _lcdshield.WriteLine(3, Utilities.RadioBandToString(_radios[1].CurrentBand).PadLeft(3) + " - " + _antennas[1].Name.PadRight(MAX_ANTENNA_NAME_LENGTH + 1) + "2");
        }

        static void DisplaySplash()
        {
            _lcdshield.Clear();
            _lcdshield.WriteLine(0, "M1DST 2 x 6", TextAlign.Centre);
            _lcdshield.WriteLine(1, "ANTENNA SWITCH", TextAlign.Centre);
            _lcdshield.WriteLine(2, "Radio A : Elecraft");
            _lcdshield.WriteLine(3, "Radio B : Yaesu BCD");
            Thread.Sleep(2000);
            _lcdshield.Clear();
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


    }
}
