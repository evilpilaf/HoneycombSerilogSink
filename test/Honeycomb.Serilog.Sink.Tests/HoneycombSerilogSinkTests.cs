using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Honeycomb.Serilog.Sink.Tests.Builders;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Honeycomb.Serilog.Sink.Tests
{
    public class HoneycombSerilogSinkTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Create_WhenInvalidTeamIdIsProvided_ThrowsArgumentException(string teamId)
        {
            const string apiKey = nameof(apiKey);
            Action action = () => CreateSut(teamId, apiKey);

            action.Should().Throw<ArgumentNullException>()
                  .Which.Message.Should().Contain(nameof(teamId));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Create_WhenInvalidApiKeyIsProvided_ThrowsArgumentException(string apiKey)
        {
            const string teamId = nameof(teamId);
            Action action = () => CreateSut(teamId, apiKey);

            action.Should().Throw<ArgumentNullException>()
                  .Which.Message.Should().Contain(nameof(apiKey));
        }

        [Fact]
        public void Emit_AlwaysSendsApiKey()
        {
            const string teamId = nameof(teamId);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(teamId, apiKey, clientStub);

            sut.Emit(new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("", Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>()));

            clientStub.RequestSubmitted.Headers.Should().ContainSingle(h => h.Key == "X-Honeycomb-Team");
            clientStub.RequestSubmitted.Headers.GetValues("X-Honeycomb-Team").Should().ContainSingle().Which.Should().Be(apiKey);
        }

        [Fact]
        public void Emit_CallsEndpointUsingTeamId()
        {
            const string teamId = nameof(teamId);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(teamId, apiKey, clientStub);

            sut.Emit(new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("", Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>()));

            clientStub.RequestSubmitted.RequestUri.ToString().Should().EndWith(teamId);

            clientStub.RequestSubmitted.Headers.Should().ContainSingle(h => h.Key == "X-Honeycomb-Team");
            clientStub.RequestSubmitted.Headers.GetValues("X-Honeycomb-Team").Should().ContainSingle().Which.Should().Be(apiKey);
        }

        private HoneycombSerilogSink CreateSut(string teamId, string apiKey, HttpClient client = null)
        {
            return new HoneycombSerilogSinkStub(client, teamId, apiKey);
        }
    }
}
