
namespace TwoBySixAntennaSwitch
{
    public enum RadioState
    {
        Rx = 0,
        Tx = 1,
        Inhibit = -1
    }

    public partial class Utilities
    {
        public static string RadioStateToString(RadioState band)
        {
            switch (band)
            {
                case RadioState.Rx:
                    return "RX";
                case RadioState.Tx:
                    return "TX";
                case RadioState.Inhibit:
                    return "TX INHIBIT";

            }

            return "UNKNOWN";

        }
    }
}
