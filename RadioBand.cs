using System;
using Microsoft.SPOT;

namespace TwoBySixAntennaSwitch
{
    public enum RadioBand
    {
        Unknown = 0,
        B160 = 1,
        B80 = 2,
        B40 = 3,
        B30 = 4,
        B20 = 5,
        B17 = 6,
        B15 = 7,
        B12 = 8,
        B10 = 9,
        B06 = 10,

    }

    public partial class Utilities
    {
        public static string RadioBandToString(RadioBand band)
        {
            switch (band)
            {
                case RadioBand.B06:
                    return "6M";
                case RadioBand.B10:
                    return "10M";
                case RadioBand.B12:
                    return "12M";
                case RadioBand.B15:
                    return "15M";
                case RadioBand.B17:
                    return "17M";
                case RadioBand.B20:
                    return "20M";
                case RadioBand.B30:
                    return "30M";
                case RadioBand.B40:
                    return "40M";
                case RadioBand.B80:
                    return "80M";
                case RadioBand.B160:
                    return "160M";
            }

            return "---";

        }

        public static RadioBand BcdStringToBand(string bcd)
        {
            switch (bcd)
            {
                case "1000": // 160M
                    return RadioBand.B160;
                case "0100": //  80M
                    return RadioBand.B80;
                case "1100": //  40M
                    return RadioBand.B40;
                case "0010": //  30M
                    return RadioBand.B30;
                case "1010": //  20M
                    return RadioBand.B20;
                case "0110": //  17M
                    return RadioBand.B17;
                case "1110": //  15M
                    return RadioBand.B15;
                case "0001": //  12M
                    return RadioBand.B12;
                case "1001": //  10M
                    return RadioBand.B10;
                case "0101": //   6M
                    return RadioBand.B06;
            }

            return RadioBand.Unknown;

        }
    }
}

