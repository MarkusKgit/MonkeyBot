using System;
using System.Linq;
using System.Text;

namespace MonkeyBot.Services
{
    internal class Parser
    {
        private readonly byte[] data = null;
        private int currentPosition = -1;
        private readonly int lastPosition = 0;

        internal bool HasUnParsedBytes => currentPosition <= lastPosition;

        internal Parser(byte[] data)
        {
            this.data = data;
            currentPosition = -1;
            lastPosition = data.Length - 1;
        }

        internal byte ReadByte()
        {
            currentPosition++;
            if (currentPosition > lastPosition)
            {
                throw new ParseException("Index was outside the bounds of the byte array.");
            }
            return data[currentPosition];
        }

        internal ushort ReadUShort()
        {
            currentPosition++;
            if (currentPosition + 3 > lastPosition)
            {
                throw new ParseException("Unable to parse bytes to ushort.");
            }
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data, currentPosition, 2);
            }
            ushort num = BitConverter.ToUInt16(data, currentPosition);
            currentPosition++;

            return num;
        }

        internal int ReadInt()
        {
            currentPosition++;
            if (currentPosition + 3 > lastPosition)
            {
                throw new ParseException("Unable to parse bytes to int.");
            }
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data, currentPosition, 4);
            }
            int num = BitConverter.ToInt32(data, currentPosition);
            currentPosition += 3;

            return num;
        }

        internal ulong ReadULong()
        {
            currentPosition++;
            if (currentPosition + 7 > lastPosition)
            {
                throw new ParseException("Unable to parse bytes to ulong.");
            }

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data, currentPosition, 8);
            }

            ulong num = BitConverter.ToUInt64(data, currentPosition);
            currentPosition += 7;

            return num;
        }

        internal float ReadFloat()
        {
            currentPosition++;
            if (currentPosition + 3 > lastPosition)
            {
                throw new ParseException("Unable to parse bytes to float.");
            }

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(data, currentPosition, 4);
            }

            float num = BitConverter.ToSingle(data, currentPosition);
            currentPosition += 3;

            return num;
        }

        internal string ReadString()
        {
            currentPosition++;
            int temp = currentPosition;
            while (data[currentPosition] != 0x00)
            {
                currentPosition++;
                if (currentPosition > lastPosition)
                {
                    throw new ParseException("Unable to parse bytes to string.");
                }
            }

            string str = Encoding.UTF8.GetString(data, temp, currentPosition - temp);

            return str;
        }

        internal void SkipBytes(byte count)
        {
            currentPosition += count;
            if (currentPosition > lastPosition)
            {
                throw new ParseException("skip count was outside the bounds of the byte array.");
            }
        }

        internal byte[] GetUnParsedBytes()
            => data.Skip(currentPosition + 1).ToArray();
    }

    /// <summary>
    /// The exception that is thrown when there is an error while parsing received packets.
    /// </summary>
    [Serializable]
    public class ParseException : Exception
    {
        public ParseException() : base()
        {
        }

        public ParseException(string message) : base(message)
        {
        }

        public ParseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ParseException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}