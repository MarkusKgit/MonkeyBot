using System;

namespace MonkeyBot.Modules
{
    internal sealed class ExampleAttribute : Attribute
    {
        public string ExampleText { get; private set; }

        public ExampleAttribute(string exampleText)
        {
            ExampleText = exampleText;
        }
    }
}