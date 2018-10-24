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
        private TcpClient _client;
        private NetworkStream _stream;
        private List<byte> _writeBuffer;
        private int _offset;

        public IPAddress Address { get; }
        public int Port { get; }

        public MineQuery(IPAddress address, int port)
        {
            Address = address;
            Port = port;
            _client = new TcpClient();
        }

        public async Task<MineQueryResult> GetServerInfoAsync()
        {
            await _client.ConnectAsync(Address, Port);
            if (!_client.Connected)
                return null;
            _writeBuffer = new List<byte>();
            _stream = _client.GetStream();

            //Send a "Handshake" packet http://wiki.vg/Server_List_Ping#Ping_Process

            WriteVarInt(47);
            WriteString(Address.ToString());
            WriteShort(25565);
            WriteVarInt(1);
            Flush(0);

            // Send a "Status Request" packet http://wiki.vg/Server_List_Ping#Ping_Process

            Flush(0);

            var readBuffer = new byte[Int16.MaxValue];
            _stream.Read(readBuffer, 0, readBuffer.Length);

            try
            {
                var length = ReadVarInt(readBuffer);
                var packet = ReadVarInt(readBuffer);
                var jsonLength = ReadVarInt(readBuffer);

                var json = ReadString(readBuffer, jsonLength);
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
                _client.Close();
                _stream.Dispose();
            }
        }

        internal byte ReadByte(byte[] buffer)
        {
            var b = buffer[_offset];
            _offset += 1;
            return b;
        }

        internal byte[] Read(byte[] buffer, int length)
        {
            var data = new byte[length];
            Array.Copy(buffer, _offset, data, 0, length);
            _offset += length;
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
                _writeBuffer.Add((byte)(value & 127 | 128));
                value = (int)((uint)value) >> 7;
            }
            _writeBuffer.Add((byte)value);
        }

        internal void WriteShort(short value)
        {
            _writeBuffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void WriteString(string data)
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            WriteVarInt(buffer.Length);
            _writeBuffer.AddRange(buffer);
        }

        internal void Write(byte b)
        {
            _stream.WriteByte(b);
        }

        internal void Flush(int id = -1)
        {
            var buffer = _writeBuffer.ToArray();
            _writeBuffer.Clear();

            var add = 0;
            var packetData = new[] { (byte)0x00 };
            if (id >= 0)
            {
                WriteVarInt(id);
                packetData = _writeBuffer.ToArray();
                add = packetData.Length;
                _writeBuffer.Clear();
            }

            WriteVarInt(buffer.Length + add);
            var bufferLength = _writeBuffer.ToArray();
            _writeBuffer.Clear();

            _stream.Write(bufferLength, 0, bufferLength.Length);
            _stream.Write(packetData, 0, packetData.Length);
            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Dispose()
        {
            _client.Close();
            _client.Dispose();
        }
    }
}