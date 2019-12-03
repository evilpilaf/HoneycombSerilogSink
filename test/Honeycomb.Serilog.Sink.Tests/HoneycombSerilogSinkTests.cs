using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

using FluentAssertions;
using FluentAssertions.Execution;

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
        }

        [Fact]
        public void Emit_SerializesLogMessageAsJson_ExcludesRenderedMessageAsync()
        {
            const string teamId = nameof(teamId);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(teamId, apiKey, clientStub);

            var level = LogEventLevel.Fatal;
            var eventTime = DateTimeOffset.Now;

            var messageTemplateParser = new MessageTemplateParser();
            var messageTempalteString = "Testing message {message}";
            var messageTemplate = messageTemplateParser.Parse(messageTempalteString);

            sut.Emit(new LogEvent(eventTime, level, null, messageTemplate, new[] { new LogEventProperty("message", new ScalarValue("hello")) }));

            var requestContent = clientStub.RequestContent;
            using (new AssertionScope())
            using (JsonDocument document = JsonDocument.Parse(requestContent))
            {
                document.RootElement.GetProperty("Level").GetString().Should().Be(level.ToString());
                document.RootElement.GetProperty("Timestamp").GetDateTimeOffset().Should().Be(eventTime);
                document.RootElement.GetProperty("MessageTemplate").GetString().Should().Be(messageTempalteString);
            }
        }

        private HoneycombSerilogSink CreateSut(string teamId, string apiKey, HttpClient client = null)
        {
            return new HoneycombSerilogSinkStub(client, teamId, apiKey);
        }
    }
}
