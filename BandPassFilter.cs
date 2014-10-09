using System;
using Microsoft.SPOT;

namespace TwoBySixAntennaSwitch
{

    public enum BandPassFilterType
    {
        None = 0,
        YaesuBCD = 1,
        SeparateBands = 2
    }

    public partial class Utilities
    {
        public static string BandPassFilterTypeToString(BandPassFilterType bandPassFilterType)
        {
            switch (bandPassFilterType)
            {
                case BandPassFilterType.None:
                    return "None";
                case BandPassFilterType.YaesuBCD:
                    return "Yaesu (BCD)";
                case BandPassFilterType.SeparateBands:
                    return "Separate Bands";

            }

            return "UNKNOWN";

        }

        public static string BandPassFilterTypeToString(string bandPassFilterType)
        {
            var x = (BandPassFilterType)Convert.ToInt16(bandPassFilterType);
            return BandPassFilterTypeToString(x);
        }
    }
}
