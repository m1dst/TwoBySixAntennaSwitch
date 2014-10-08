using System;
using System.Text;

namespace TwoBySixAntennaSwitch
{
    public static class StringExtension
    {

        public static string PadLeft(this string text, int totalWidth)
        {
            return PadLeft(text, totalWidth, ' ');
        }

        public static string PadLeft(this string text, int totalWidth, char paddingChar)
        {
            if (totalWidth < 0)
                throw new ArgumentOutOfRangeException("totalWidth", "< 0");

            if (totalWidth < text.Length)
                return text;
            if (totalWidth == 0)
                return string.Empty;

            while (totalWidth > text.Length)
            {
                text = paddingChar + text;
            }

            return text;
        }


        public static string PadRight(this string text, int totalWidth)
        {
            return PadRight(text, totalWidth, ' ');
        }

        public static string PadRight(this string text, int totalWidth, char paddingChar)
        {
            if (totalWidth < 0)
                throw new ArgumentOutOfRangeException("totalWidth", "< 0");

            if (totalWidth < text.Length)
                return text;
            if (totalWidth == 0)
                return string.Empty;

            while (totalWidth > text.Length)
            {
                text = text + paddingChar;
            }

            return text;
        }

        /// <summary>
        /// Replace all occurances of the 'find' string with the 'replace' string.
        /// </summary>
        /// <param name="content">Original string to operate on</param>
        /// <param name="find">String to find within the original string</param>
        /// <param name="replace">String to be used in place of the find string</param>
        /// <returns>Final string after all instances have been replaced.</returns>
        public static string Replace(this string content, string find, string replace)
        {
            int startFrom = 0;
            int findItemLength = find.Length;

            int firstFound = content.IndexOf(find, startFrom);
            StringBuilder returning = new StringBuilder();

            string workingString = content;

            while ((firstFound = workingString.IndexOf(find, startFrom)) >= 0)
            {
                returning.Append(workingString.Substring(0, firstFound));
                returning.Append(replace);

                // the remaining part of the string.
                workingString = workingString.Substring(firstFound + findItemLength, workingString.Length - (firstFound + findItemLength));
            }

            returning.Append(workingString);

            return returning.ToString();

        }


    }
}
