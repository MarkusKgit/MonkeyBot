namespace MonkeyBot.Utilities
{
    public interface IDocumentFormatter
    {
        string Strong(string input);

        string Em(string input);

        string H1(string input);

        string H2(string input);

        string H3(string input);

        string H4(string input);

        string H5(string input);

        string H6(string input);

        string NewLine(string input);

        string InlineCode(string input);
    }
}