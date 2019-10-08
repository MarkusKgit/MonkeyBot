namespace System
{
    public static class StringExtensions
    {
        public static bool IsEmpty(this string s) => string.IsNullOrEmpty(s);

        public static bool IsEmptyOrWhiteSpace(this string s) => string.IsNullOrWhiteSpace(s);

        public static string TruncateTo(this string s, int maxLength, string append = "…")
            => s.Length < maxLength ? s : s.Substring(0, maxLength) + append;
    }
}
