using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Honeycomb.Serilog.Sink.Tests.Helpers
{
    public sealed class HttpMessageHandlerStub : HttpMessageHandler
    {
        private HttpStatusCode _statusCodeToReturn = HttpStatusCode.NotImplemented;
        private HttpRequestMessage? _requestMessage;
        private string? _requestContent;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requestMessage = request;
            _requestContent = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(_statusCodeToReturn)
            {
                Content = new StringContent("")
            };
        }

        public HttpRequestMessage? GetRequestMessage() => _requestMessage;
        public string? GetRequestContent() => _requestContent;

        public void ReturnsStatusCode(HttpStatusCode statusCode)
        {
            _statusCodeToReturn = statusCode;
        }
    }
}
