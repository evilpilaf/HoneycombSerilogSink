using System.Net;
using System.Net.Http;
using Honeycomb.Serilog.Sink.Tests.Helpers;

namespace Honeycomb.Serilog.Sink.Tests.Builders
{
    public class HttpClientBuilder
    {
        private HttpStatusCode _statusCode;

        public static implicit operator HttpClient(HttpClientBuilder instance)
        {
            var handler = new HttpMessageHandlerStub();
            handler
            return new HttpClient(handler);
        }

        public HttpClientBuilder ThatReturnsStatusCode(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
            return this;
        }
    }
}
