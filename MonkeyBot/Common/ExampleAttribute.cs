using System;

namespace MonkeyBot.Common
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class ExampleAttribute : Attribute
    {
        public string ExampleText { get; private set; }

        public ExampleAttribute(string exampleText)
        {
            ExampleText = exampleText;
        }
    }
}