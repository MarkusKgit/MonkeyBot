namespace MonkeyBot.Utilities
{
    /// <summary>
    /// Provides helper functions to format a string in html syntax
    /// See https://www.w3schools.com/tags/
    /// </summary>
    public class HTMLFormatter : IDocumentFormatter
    {
        public string Em(string input) => $"<em>{input}</em>";

        public string H1(string input) => $"<h1>{input}</h1>";

        public string H2(string input) => $"<h2>{input}</h2>";

        public string H3(string input) => $"<h3>{input}</h3>";

        public string H4(string input) => $"<h4>{input}</h4>";

        public string H5(string input) => $"<h5>{input}</h5>";

        public string H6(string input) => $"<h6>{input}</h6>";

        public string InlineCode(string input) => $"<code>{input}</code>";

        public string NewLine(string input) => $"{input}</br>";

        public string Strong(string input) => $"<strong>{input}</strong>";
    }
}