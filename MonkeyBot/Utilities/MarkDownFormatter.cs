namespace MonkeyBot.Utilities
{
    /// <summary>
    /// Provides helper functions to format a string in markdown syntax
    /// See https://www.markdownguide.org/cheat-sheet/
    /// </summary>
    public class MarkDownFormatter : IDocumentFormatter
    {
        public string Em(string input) => $"*{input}*";

        public string H1(string input) => $"# {input}";

        public string H2(string input) => $"## {input}";

        public string H3(string input) => $"### {input}";

        public string H4(string input) => $"#### {input}";

        public string H5(string input) => $"##### {input}";

        public string H6(string input) => $"###### {input}";

        public string HorizontalRule() => "---";

        public string InlineCode(string input) => $"`{input}`";

        public string NewLine(string input) => $"{input}  ";

        public string Strong(string input) => $"**{input}**";
    }
}