using System;

namespace TwoBySixAntennaSwitch
{
    public class BandMask
    {

        public BandMask(string mask)
        {
            if (mask.Length > 6)
                throw new Exception("Mask is too long.  Should be 6 chars");

            if (mask.Length < 6)
                throw new Exception("Mask is too short.  Should be 6 chars");

            // TODO: Should really validate that all chars are either 2, 1 or zero.

            if (mask.Substring(0, 1).Equals("2"))
            {
                Is160M = true;
                IsPriority160M = true;
            }
            if (mask.Substring(0, 1).Equals("1"))
            {
                Is160M = true;
            }
            if (mask.Substring(1, 1).Equals("2"))
            {
                Is80M = true;
                IsPriority80M = true;
            }
            if (mask.Substring(1, 1).Equals("1"))
            {
                Is80M = true;
            }
            if (mask.Substring(2, 1).Equals("2"))
            {
                Is40M = true;
                IsPriority40M = true;
            }
            if (mask.Substring(2, 1).Equals("1"))
            {
                Is40M = true;
            }
            if (mask.Substring(3, 1).Equals("2"))
            {
                Is20M = true;
                IsPriority20M = true;
            }
            if (mask.Substring(3, 1).Equals("1"))
            {
                Is20M = true;
            }
            if (mask.Substring(4, 1).Equals("2"))
            {
                Is15M = true;
                IsPriority15M = true;
            }
            if (mask.Substring(4, 1).Equals("1"))
            {
                Is15M = true;
            }
            if (mask.Substring(5, 1).Equals("2"))
            {
                Is10M = true;
                IsPriority10M = true;
            }
            if (mask.Substring(5, 1).Equals("1"))
            {
                Is10M = true;
            }

        }

        public bool Is160M { get; set; }
        public bool Is80M { get; set; }
        public bool Is40M { get; set; }
        public bool Is20M { get; set; }
        public bool Is15M { get; set; }
        public bool Is10M { get; set; }

        public bool IsPriority160M { get; set; }
        public bool IsPriority80M { get; set; }
        public bool IsPriority40M { get; set; }
        public bool IsPriority20M { get; set; }
        public bool IsPriority15M { get; set; }
        public bool IsPriority10M { get; set; }

        public override string ToString()
        {
            var mask = CountTrue(Is160M, IsPriority160M).ToString();
            mask = mask + CountTrue(Is80M, IsPriority80M);
            mask = mask + CountTrue(Is40M, IsPriority40M);
            mask = mask + CountTrue(Is20M, IsPriority20M);
            mask = mask + CountTrue(Is15M, IsPriority15M);
            mask = mask + CountTrue(Is10M, IsPriority10M);
            return mask;
        }

        public static int CountTrue(params bool[] args)
        {
            var count = 0;
            foreach (var b in args)
            {
                if (b)
                    count++;
            }
            return count;
        }
    }
}
