using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyBot.Services.Common.MineCraftServerQuery
{
    public class MineQuery
    {
        private const ushort dataSize = 512;
        private const ushort numFields = 6;

        public string Address { get; }
        public ushort Port { get; }

        public MineQuery(string address, ushort port)
        {
            Address = address;
            Port = port;
        }

        public async Task<MineStats> GetStatsAsync()
        {
            var rawServerData = new byte[dataSize];

            try
            {
                using (var tcpclient = new TcpClient())
                {
                    await tcpclient.ConnectAsync(Address, Port);
                    if (!tcpclient.Connected)
                        return null;
                    var stream = tcpclient.GetStream();
                    var payload = new byte[] { 0xFE, 0x01 };
                    await stream.WriteAsync(payload, 0, payload.Length);
                    int bytesRead = await stream.ReadAsync(rawServerData, 0, dataSize);
                    tcpclient.Close();
                }
            }
            catch (Exception)
            {
                return null;
            }

            if (rawServerData != null && rawServerData.Length > 0)
            {
                var serverData = Encoding.Unicode.GetString(rawServerData).Split("\u0000\u0000\u0000".ToCharArray());
                if (serverData != null && serverData.Length >= numFields)
                {
                    return new MineStats(serverData[2], serverData[3], serverData[4], serverData[5]);
                }
            }

            return null;
        }
    }
}