using System;
using System.Collections.Generic;
using System.Text;

namespace MonkeyBot.Common.Extensions
{
    public static class StringExt
    {
        public static string Truncate(this string text, int maxLength, string suffix = "...")
        {
            string str = text;
            if (maxLength > 0)
            {
                int length = maxLength - suffix.Length;
                if (length <= 0)
                {
                    return str;
                }
                if ((text != null) && (text.Length > maxLength))
                {
                    return (text.Substring(0, length).TrimEnd(new char[0]) + suffix);
                }
            }
            return str;
        }
    }
}