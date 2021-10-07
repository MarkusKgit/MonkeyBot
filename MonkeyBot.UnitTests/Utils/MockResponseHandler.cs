using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyBot.UnitTests.Utils
{
    public class MockResponseHandler : DelegatingHandler
    {
        private readonly Dictionary<Uri, HttpResponseMessage> _fakeResponses = new Dictionary<Uri, HttpResponseMessage>();

        public void AddMockResponse<T>(List<T> data, string requestUri)
        {
            AddMockResponse(new Uri(requestUri), new HttpResponseMessage(HttpStatusCode.OK), JsonSerializer.Serialize(data));
        }

        public void AddMockResponse<T>(T data, string requestUri)
        {
            AddMockResponse(new Uri(requestUri), new HttpResponseMessage(HttpStatusCode.OK), JsonSerializer.Serialize(data));
        }

        public void AddMockResponse(Uri uri, HttpResponseMessage responseMessage, string content = "")
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                responseMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }
            _fakeResponses.Add(uri, responseMessage);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var emptyContent = string.Empty;

            return Task.FromResult(_fakeResponses.ContainsKey(request.RequestUri) ?
                _fakeResponses[request.RequestUri] :
                new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    RequestMessage = request,
                    Content = new StringContent(emptyContent)
                });
        }

    }
}
