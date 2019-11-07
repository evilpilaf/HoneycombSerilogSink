using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Honeycomb.Serilog.Sink.Tests.Helpers
{
    public sealed class HttpMessageHandlerStub : HttpMessageHandler
    {
        public Dictionary<string, HttpResponseMessage> EndpointResponses = new Dictionary<string, HttpResponseMessage>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var isResponseSetup = EndpointResponses.TryGetValue(request.RequestUri.LocalPath, out var responseMessage);

            if (isResponseSetup)
            {
                return Task.FromResult(responseMessage);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotImplemented));
        }
    }
}
