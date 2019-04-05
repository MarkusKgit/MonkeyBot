using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Services.Common.MineCraftServerQuery
{
    public class MineQuery : IDisposable
    {
        private readonly TcpClient client;
        private List<byte> writeBuffer;
        private int offset;

        public IPAddress Address { get; }
        public int Port { get; }

        public MineQuery(IPAddress address, int port)
        {
            Address = address;
            Port = port;
            client = new TcpClient();
        }

        public async Task<MineQueryResult> GetServerInfoAsync()
        {
            await client.ConnectAsync(Address, Port);
            if (!client.Connected)
                return null;
            writeBuffer = new List<byte>();
            NetworkStream stream = client.GetStream();

            //Send a "Handshake" packet http://wiki.vg/Server_List_Ping#Ping_Process

            WriteVarInt(47);
            WriteString(Address.ToString());
            WriteShort(25565);
            WriteVarInt(1);
            Flush(stream, 0);

            // Send a "Status Request" packet http://wiki.vg/Server_List_Ping#Ping_Process

            Flush(stream, 0);

            var readBuffer = new byte[client.ReceiveBufferSize];
            var completeBuffer = new List<byte>();
            int numberOfBytesRead;
            do
            {
                numberOfBytesRead = await stream.ReadAsync(readBuffer);
                completeBuffer.AddRange(readBuffer);
            }
            while (stream.DataAvailable && numberOfBytesRead > 0);

            try
            {
                var b = completeBuffer.ToArray();
                var length = ReadVarInt(b);
                var packet = ReadVarInt(b);
                var jsonLength = ReadVarInt(b);

                var json = ReadString(b, jsonLength);
                var result = JsonConvert.DeserializeObject<MineQueryResult>(json);
                return result;
            }
            catch (IOException ex)
            {
                //TODO: Add logging
                return null;
            }
            finally
            {
                client.Close();
                stream.Dispose();
            }
        }

        internal byte ReadByte(byte[] buffer)
        {
            var b = buffer[offset];
            offset += 1;
            return b;
        }

        internal byte[] Read(byte[] buffer, int length)
        {
            var data = new byte[length];
            Array.Copy(buffer, offset, data, 0, length);
            offset += length;
            return data;
        }

        internal int ReadVarInt(byte[] buffer)
        {
            var value = 0;
            var size = 0;
            int b;
            while (((b = ReadByte(buffer)) & 0x80) == 0x80)
            {
                value |= (b & 0x7F) << (size++ * 7);
                if (size > 5)
                {
                    throw new IOException("This VarInt is an imposter!");
                }
            }
            return value | ((b & 0x7F) << (size * 7));
        }

        internal string ReadString(byte[] buffer, int length)
        {
            var data = Read(buffer, length);
            return Encoding.UTF8.GetString(data);
        }

        internal void WriteVarInt(int value)
        {
            while ((value & 128) != 0)
            {
                writeBuffer.Add((byte)(value & 127 | 128));
                value = (int)((uint)value) >> 7;
            }
            writeBuffer.Add((byte)value);
        }

        internal void WriteShort(short value)
        {
            writeBuffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void WriteString(string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            WriteVarInt(buffer.Length);
            writeBuffer.AddRange(buffer);
        }


        internal void Flush(NetworkStream stream, int id = -1)
        {
            var buffer = writeBuffer.ToArray();
            writeBuffer.Clear();

            var add = 0;
            var packetData = new[] { (byte)0x00 };
            if (id >= 0)
            {
                WriteVarInt(id);
                packetData = writeBuffer.ToArray();
                add = packetData.Length;
                writeBuffer.Clear();
            }

            WriteVarInt(buffer.Length + add);
            var bufferLength = writeBuffer.ToArray();
            writeBuffer.Clear();

            stream.Write(bufferLength, 0, bufferLength.Length);
            stream.Write(packetData, 0, packetData.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        public void Dispose()
        {
            client.Close();
            client.Dispose();
        }
    }
}