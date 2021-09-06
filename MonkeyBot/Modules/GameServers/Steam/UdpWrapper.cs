using SteamQueryNet.Interfaces;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MonkeyBot.Services.Implementations.GameServers.SteamServerQuery
{
    internal class UdpWrapper : IUdpClient
    {
        private readonly UdpClient _udpClient;
        private readonly int _sendTimeout;
        private readonly int _receiveTimeout;

        public UdpWrapper(IPEndPoint localIpEndPoint, int sendTimeout, int receiveTimeout)
        {
            _udpClient = new UdpClient(localIpEndPoint);
            _sendTimeout = sendTimeout;
            _receiveTimeout = receiveTimeout;
        }

        public UdpWrapper() : this(new IPEndPoint(IPAddress.Any, 0), 5000, 5000) { }

        public bool IsConnected
            => _udpClient.Client.Connected;

        public void Close()
            => _udpClient.Close();

        public void Connect(IPEndPoint remoteIpEndpoint)
            => _udpClient.Connect(remoteIpEndpoint);

        public void Dispose()
            => _udpClient.Dispose();

        public Task<UdpReceiveResult> ReceiveAsync()
        {
            IAsyncResult asyncResult = _udpClient.BeginReceive(null, null);
            asyncResult.AsyncWaitHandle.WaitOne(_receiveTimeout);
            if (asyncResult.IsCompleted)
            {
                IPEndPoint remoteEP = null;
                byte[] receivedData = _udpClient.EndReceive(asyncResult, ref remoteEP);
                return Task.FromResult(new UdpReceiveResult(receivedData, remoteEP));
            }
            else
            {
                return Task.FromException<UdpReceiveResult>(new TimeoutException());
            }
        }

        public Task<int> SendAsync(byte[] datagram, int bytes)
        {
            IAsyncResult asyncResult = _udpClient.BeginSend(datagram, bytes, null, null);
            asyncResult.AsyncWaitHandle.WaitOne(_sendTimeout);
            if (asyncResult.IsCompleted)
            {
                int num = _udpClient.EndSend(asyncResult);
                return Task.FromResult(num);
            }
            else
            {
                return Task.FromException<int>(new TimeoutException());
            }
        }
    }
}
