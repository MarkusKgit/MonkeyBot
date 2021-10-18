using System;

namespace MonkeyBot.Services
{
    [Serializable]
    internal class MessageComponentLinkNotFoundException : Exception
    {
        public MessageComponentLinkNotFoundException() : base()
        {
        }

        public MessageComponentLinkNotFoundException(string message) : base(message)
        {
        }
    }
}