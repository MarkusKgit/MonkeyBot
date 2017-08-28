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
                    int pos = text.LastIndexOf(" ", length);
                    if (pos > 0)
                        return (text.Substring(0, pos).TrimEnd(new char[0]) + suffix);
                    else
                        return (text.Substring(0, length).TrimEnd(new char[0]) + suffix);
                }
            }
            return str;
        }
    }
}