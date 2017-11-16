using System;
using System.Linq;
using System.Text;

namespace MonkeyBot.Services.Common.SteamServerQuery
{
    internal class Parser
    {
        private byte[] Data = null;
        private int CurrentPosition = -1;
        private int LastPosition = 0;

        internal bool HasUnParsedBytes
        {
            get { return CurrentPosition <= LastPosition; }
        }

        internal Parser(byte[] data)
        {
            Data = data;
            CurrentPosition = -1;
            LastPosition = Data.Length - 1;
        }

        internal byte ReadByte()
        {
            CurrentPosition++;
            if (CurrentPosition > LastPosition)
                throw new ParseException("Index was outside the bounds of the byte array.");

            return Data[CurrentPosition];
        }

        internal ushort ReadUShort()
        {
            ushort num = 0;

            CurrentPosition++;
            if (CurrentPosition + 3 > LastPosition)
                throw new ParseException("Unable to parse bytes to ushort.");

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(Data, CurrentPosition, 2);

            num = BitConverter.ToUInt16(Data, CurrentPosition);
            CurrentPosition++;

            return num;
        }

        internal int ReadInt()
        {
            int num = 0;

            CurrentPosition++;
            if (CurrentPosition + 3 > LastPosition)
                throw new ParseException("Unable to parse bytes to int.");

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(Data, CurrentPosition, 4);

            num = BitConverter.ToInt32(Data, CurrentPosition);
            CurrentPosition += 3;

            return num;
        }

        internal ulong ReadULong()
        {
            ulong num = 0;

            CurrentPosition++;
            if (CurrentPosition + 7 > LastPosition)
                throw new ParseException("Unable to parse bytes to ulong.");

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(Data, CurrentPosition, 8);

            num = BitConverter.ToUInt64(Data, CurrentPosition);
            CurrentPosition += 7;

            return num;
        }

        internal float ReadFloat()
        {
            float Num = 0;

            CurrentPosition++;
            if (CurrentPosition + 3 > LastPosition)
                throw new ParseException("Unable to parse bytes to float.");

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(Data, CurrentPosition, 4);

            Num = BitConverter.ToSingle(Data, CurrentPosition);
            CurrentPosition += 3;

            return Num;
        }

        internal string ReadString()
        {
            string str = string.Empty;
            int temp = 0;

            CurrentPosition++;
            temp = CurrentPosition;

            while (Data[CurrentPosition] != 0x00)
            {
                CurrentPosition++;
                if (CurrentPosition > LastPosition)
                    throw new ParseException("Unable to parse bytes to string.");
            }

            str = Encoding.UTF8.GetString(Data, temp, CurrentPosition - temp);

            return str;
        }

        internal void SkipBytes(byte count)
        {
            CurrentPosition += count;
            if (CurrentPosition > LastPosition)
                throw new ParseException("skip count was outside the bounds of the byte array.");
        }

        internal byte[] GetUnParsedBytes()
        {
            return Data.Skip(CurrentPosition + 1).ToArray();
        }
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
    }
}