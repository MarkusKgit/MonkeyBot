namespace MonkeyBot.Utilities
{
    public class HTMLFormatter : IDocumentFormatter
    {
        public string Em(string input)
        {
            return $"<em>{input}</em>";
        }

        public string H1(string input)
        {
            return $"<h1>{input}</h1>";
        }

        public string H2(string input)
        {
            return $"<h2>{input}</h2>";
        }

        public string H3(string input)
        {
            return $"<h3>{input}</h3>";
        }

        public string H4(string input)
        {
            return $"<h4>{input}</h4>";
        }

        public string H5(string input)
        {
            return $"<h5>{input}</h5>";
        }

        public string H6(string input)
        {
            return $"<h6>{input}</h6>";
        }

        public string InlineCode(string input)
        {
            return $"<code>{input}</code>";
        }

        public string NewLine(string input)
        {
            return $"{input}</br>";
        }

        public string Strong(string input)
        {
            return $"<strong>{input}</strong>";
        }
    }
}