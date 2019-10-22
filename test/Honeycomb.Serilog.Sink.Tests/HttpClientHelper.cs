using System.Net.Http;

using System.Threading;
using System.Threading.Tasks;

namespace Honeycomb.Serilog.Sink.Tests
{
    internal sealed class HttpClientHelper
    {
        private readonly DummyHttpMessageHandler _messageHandler;

        public HttpClient GetHttpClient()
        {
            return new HttpClient(_messageHandler);
        }
    }

    internal sealed class DummyHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage RequestMessage { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage, CancellationToken _)
        {
            RequestMessage = requestMessage;
            return Task.FromResult(new HttpResponseMessage());
        }
    }
}
