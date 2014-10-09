using System;
using Microsoft.SPOT;

namespace TwoBySixAntennaSwitch
{
    public enum RadioBand
    {

        B160 = 1,
        B80 = 2,
        B40 = 3,
        B20 = 5,
        B15 = 7,
        B10 = 9,

    }

    public partial class Utilities
    {
        public static string RadioBandToString(RadioBand band)
        {
            switch (band)
            {
                case RadioBand.B10:
                    return "10M";
                case RadioBand.B15:
                    return "15M";
                case RadioBand.B20:
                    return "20M";
                case RadioBand.B40:
                    return "40M";
                case RadioBand.B80:
                    return "80M";
                case RadioBand.B160:
                    return "160M";
            }

            return "---";

        }
    }
}

