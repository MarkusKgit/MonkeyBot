using MonkeyBot.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyBot.Services.Common.SteamServerQuery
{
    public class SteamGameServer
    {
        public IPEndPoint EndPoint
        {
            get;
            private set;
        }

        private byte[] PlayerChallengeId = null;

        private bool IsPlayerChallengeId;

        private UdpClient client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));

        public SteamGameServer(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        public async Task<SteamServerInfo> GetServerInfoAsync()
        {
            var response = await GetServerResponseAsync(QueryMsg.InfoQuery);
            return SteamServerInfo.Parse(response); // Skip header
        }

        public async Task<ReadOnlyCollection<PlayerInfo>> GetPlayersAsync()
        {
            byte[] recvData = null;
            List<PlayerInfo> players = null;
            Parser parser = null;
            try
            {
                if (PlayerChallengeId == null)
                {
                    recvData = await GetPlayerChallengeIdAsync();
                    if (IsPlayerChallengeId)
                        PlayerChallengeId = recvData;
                }
                if (IsPlayerChallengeId)
                {
                    byte[] combinedArray = new byte[QueryMsg.PlayerQuery.Length + PlayerChallengeId.Length];
                    Buffer.BlockCopy(QueryMsg.PlayerQuery, 0, combinedArray, 0, QueryMsg.PlayerQuery.Length);
                    Buffer.BlockCopy(PlayerChallengeId, 0, combinedArray, QueryMsg.PlayerQuery.Length, PlayerChallengeId.Length);
                    recvData = await GetServerResponseAsync(combinedArray);
                }

                parser = new Parser(recvData);
                if (parser.ReadByte() != (byte)ResponseMsgHeader.A2S_PLAYER)
                    throw new Exception("A2S_PLAYER message header is not valid");
                int playerCount = parser.ReadByte();
                players = new List<PlayerInfo>(playerCount);
                for (int i = 0; i < playerCount; i++)
                {
                    parser.ReadByte();
                    players.Add(new PlayerInfo()
                    {
                        Name = parser.ReadString(),
                        Score = parser.ReadInt(),
                        Time = TimeSpan.FromSeconds(parser.ReadFloat())
                    });
                }
                if (playerCount == 1 && players[0].Name == "Max Players")
                    players.Clear();
            }
            catch (Exception e)
            {
                e.Data.Add("ReceivedData", recvData == null ? new byte[1] : recvData);
                throw;
            }
            return new ReadOnlyCollection<PlayerInfo>(players);
        }

        private async Task<byte[]> GetPlayerChallengeIdAsync()
        {
            byte[] recvBytes = null;
            byte header = 0;
            Parser parser = null;
            recvBytes = await GetServerResponseAsync(QueryMsg.PlayerChallengeQuery);
            try
            {
                parser = new Parser(recvBytes);
                header = parser.ReadByte();
                switch (header)
                {
                    case (byte)ResponseMsgHeader.A2S_SERVERQUERY_GETCHALLENGE: IsPlayerChallengeId = true; return parser.GetUnParsedBytes();
                    case (byte)ResponseMsgHeader.A2S_PLAYER: IsPlayerChallengeId = false; return recvBytes;
                    default: throw new Exception("A2S_SERVERQUERY_GETCHALLENGE message header is not valid");
                }
            }
            catch (Exception e)
            {
                e.Data.Add("ReceivedData", recvBytes == null ? new byte[1] : recvBytes);
                throw;
            }
        }

        private async Task<byte[]> GetServerResponseAsync(byte[] Query)
        {
            if (!client.Client.Connected)
                await client.Client.ConnectAsync(EndPoint);
            await client.SendAsync(Query, Query.Length, EndPoint);
            using (var cts = new CancellationTokenSource())
            {
                client.Client.ReceiveTimeout = 2000;
                cts.CancelAfter(2000);

                var response = await client.ReceiveAsync().WithCancellation(cts.Token);
                byte[] result = new byte[response.Buffer.Length - 4];
                response.Buffer.Skip(4).ToArray().CopyTo(result, 0);
                return result;
            }
        }
    }
}