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
        public string RequestContent { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestMessage = request;
            RequestContent = await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(_statusCodeToReturn)
            {
                Content = new StringContent("")
            };
        }

        public void ReturnsStatusCode(HttpStatusCode statusCode)
        {
            _statusCodeToReturn = statusCode;
        }
    }
}
