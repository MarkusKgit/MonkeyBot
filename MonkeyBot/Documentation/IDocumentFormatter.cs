namespace MonkeyBot.Documentation
{
    /// <summary>
    /// Interface for a class that can return formatted strings in a specific markdown language
    /// </summary>
    public interface IDocumentFormatter
    {
        string Em(string input);

        string H1(string input);

        string H2(string input);

        string H3(string input);

        string H4(string input);

        string H5(string input);

        string H6(string input);

        string HorizontalRule();

        string InlineCode(string input);

        string NewLine(string input);

        string Strong(string input);
    }
}