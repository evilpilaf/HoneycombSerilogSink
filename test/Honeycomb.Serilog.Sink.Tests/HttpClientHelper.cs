using System.Net.Http;

using System.Threading;
using System.Threading.Tasks;

namespace Honeycomb.Serilog.Sink.Tests
{
    internal sealed class HttpClientHelper
    {
        public HttpClient GetHttpClient()
        {
            return new HttpClient(new DummyHttpMessageHandler());
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
