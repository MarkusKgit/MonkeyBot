using System;
using System.Runtime.Serialization;

namespace MonkeyBot.Services
{
    [Serializable]
    internal class MessageComponentLinkAlreadyExistsException : Exception
    {
        public MessageComponentLinkAlreadyExistsException() : base("Message Component Link already exists!")
        {
        }

        public MessageComponentLinkAlreadyExistsException(string message) : base(message)
        {
        }
    }
}