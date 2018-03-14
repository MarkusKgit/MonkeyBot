namespace MonkeyBot.Utilities
{
    public class MarkDownFormatter : IDocumentFormatter
    {
        public string Em(string input)
        {
            return $"*{input}*";
        }

        public string H1(string input)
        {
            return $"# {input}";
        }

        public string H2(string input)
        {
            return $"## {input}";
        }

        public string H3(string input)
        {
            return $"### {input}";
        }

        public string H4(string input)
        {
            return $"#### {input}";
        }

        public string H5(string input)
        {
            return $"##### {input}";
        }

        public string H6(string input)
        {
            return $"###### {input}";
        }

        public string InlineCode(string input)
        {
            return $"`{input}`";
        }

        public string NewLine(string input)
        {
            return $"{input}  ";
        }

        public string Strong(string input)
        {
            return $"**{input}**";
        }
    }
}