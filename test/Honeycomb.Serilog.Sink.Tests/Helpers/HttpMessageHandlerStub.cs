using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Honeycomb.Serilog.Sink.Tests.Helpers
{
    public sealed class HttpMessageHandlerStub : HttpMessageHandler
    {
        private HttpStatusCode _statusCodeToReturn = HttpStatusCode.NotImplemented;
        public HttpRequestMessage RequestMessage { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestMessage = request;
            return Task.FromResult(new HttpResponseMessage(_statusCodeToReturn));
        }

        public void ReturnsStatusCode(HttpStatusCode statusCode)
        {
            _statusCodeToReturn = statusCode;
        }
    }
}
