using MonkeyBot.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyBot.Services
{
    internal sealed class SteamGameServer : IDisposable
    {
        public IPEndPoint EndPoint
        {
            get;
        }

        private byte[] playerChallengeId = null;

        private bool isPlayerChallengeId;

        private readonly UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));

        public SteamGameServer(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        public async Task<SteamServerInfo> GetServerInfoAsync()
        {
            byte[] response = await GetServerResponseAsync(QueryMsg.InfoQuery).ConfigureAwait(false);
            return SteamServerInfo.Parse(response); // Skip header
        }

        public async Task<ReadOnlyCollection<PlayerInfo>> GetPlayersAsync()
        {
            List<PlayerInfo> players;
            try
            {
                byte[] recvData;
                if (playerChallengeId == null)
                {
                    recvData = await GetPlayerChallengeIdAsync().ConfigureAwait(false);
                    if (isPlayerChallengeId)
                    {
                        playerChallengeId = null;
                    }
                }
                if (isPlayerChallengeId)
                {
                    byte[] combinedArray = new byte[QueryMsg.PlayerQuery.Length + playerChallengeId.Length];
                    Buffer.BlockCopy(QueryMsg.PlayerQuery, 0, combinedArray, 0, QueryMsg.PlayerQuery.Length);
                    Buffer.BlockCopy(playerChallengeId, 0, combinedArray, QueryMsg.PlayerQuery.Length, playerChallengeId.Length);
                    recvData = await GetServerResponseAsync(combinedArray).ConfigureAwait(false);
                }

                var parser = new Parser(null);
                if (parser.ReadByte() != (byte)ResponseMsgHeader.A2S_PLAYER)
                {
                    throw new Exception("A2S_PLAYER message header is not valid");
                }

                int playerCount = parser.ReadByte();
                players = new List<PlayerInfo>(playerCount);
                for (int i = 0; i < playerCount; i++)
                {
                    _ = parser.ReadByte();
                    players.Add(new PlayerInfo()
                    {
                        Name = parser.ReadString(),
                        Score = parser.ReadInt(),
                        Time = TimeSpan.FromSeconds(parser.ReadFloat())
                    });
                }
                if (playerCount == 1 && players[0].Name == "Max Players")
                {
                    players.Clear();
                }
            }
            catch (Exception e)
            {
                e.Data.Add("ReceivedData", (byte[])null == null ? new byte[1] : null);
                throw;
            }
            return new ReadOnlyCollection<PlayerInfo>(players);
        }

        private async Task<byte[]> GetPlayerChallengeIdAsync()
        {
            byte[] recvBytes = await GetServerResponseAsync(QueryMsg.PlayerChallengeQuery).ConfigureAwait(false);
            try
            {
                var parser = new Parser(recvBytes);
                byte header = parser.ReadByte();
                switch (header)
                {
                    case (byte)ResponseMsgHeader.A2S_SERVERQUERY_GETCHALLENGE: isPlayerChallengeId = true; return parser.GetUnParsedBytes();
                    case (byte)ResponseMsgHeader.A2S_PLAYER: isPlayerChallengeId = false; return recvBytes;
                    default: throw new Exception("A2S_SERVERQUERY_GETCHALLENGE message header is not valid");
                }
            }
            catch (Exception e)
            {
                e.Data.Add("ReceivedData", recvBytes ?? new byte[1]);
                throw;
            }
        }

        private async Task<byte[]> GetServerResponseAsync(byte[] Query)
        {
            if (!client.Client.Connected)
            {
                await client.Client.ConnectAsync(EndPoint).ConfigureAwait(false);
            }

            _ = await client.SendAsync(Query, Query.Length, EndPoint).ConfigureAwait(false);
            using var cts = new CancellationTokenSource();
            client.Client.ReceiveTimeout = 2000;
            cts.CancelAfter(2000);

            UdpReceiveResult response = await client.ReceiveAsync().WithCancellationAsync(cts.Token).ConfigureAwait(false);
            byte[] result = new byte[response.Buffer.Length - 4];
            response.Buffer.Skip(4).ToArray().CopyTo(result, 0);
            return result;
        }

        public void Dispose()
        {
            client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}