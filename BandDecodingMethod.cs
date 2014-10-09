
using System;

namespace TwoBySixAntennaSwitch
{
    public enum BandDecodingMethod
    {
        YaesuBcd = 0,
        IcomVoltage = 1,
        Civ = 2,
        Kenwood = 3
    }

    public partial class Utilities
    {
        public static string BandDecodingMethodToString(BandDecodingMethod bandDecodingMethod)
        {
            switch (bandDecodingMethod)
            {
                case BandDecodingMethod.YaesuBcd:
                    return "Yaesu (BCD)";
                case BandDecodingMethod.IcomVoltage:
                    return "Icom (Voltage)";
                case BandDecodingMethod.Civ:
                    return "Icom (CI-V)";
                case BandDecodingMethod.Kenwood:
                    return "Kenwood (CAT)";

            }

            return "UNKNOWN";

        }

        public static string BandDecodingMethodToString(string bandDecodingMethod)
        {
            var x = (BandDecodingMethod) Convert.ToInt16(bandDecodingMethod);
            return BandDecodingMethodToString(x);
        }
    }

}
