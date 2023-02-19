using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using FluentAssertions;
using FluentAssertions.Execution;

using Honeycomb.Serilog.Sink.Tests.Builders;
using Honeycomb.Serilog.Sink.Tests.Helpers;

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
        public void Create_WhenInvalidTeamIdIsProvided_ThrowsArgumentException(string dataset)
        {
            const string apiKey = nameof(apiKey);
            HttpClientStub clientStub = A.HttpClient();

            Action action = () => CreateSut(dataset, apiKey, clientStub);

            action.Should().Throw<ArgumentNullException>()
                  .Which.Message.Should().Contain(nameof(dataset));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Create_WhenInvalidApiKeyIsProvided_ThrowsArgumentException(string apiKey)
        {
            const string dataset = nameof(dataset);
            HttpClientStub clientStub = A.HttpClient();

            Action action = () => CreateSut(dataset, apiKey, clientStub);

            action.Should().Throw<ArgumentNullException>()
                  .Which.Message.Should().Contain(nameof(apiKey));
        }

        [Fact]
        public async Task Emit_AlwaysSendsApiKeyAsync()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            await sut.EmitTestable(new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("", Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>()));

            clientStub.RequestSubmitted.Should().NotBeNull();
            clientStub.RequestSubmitted!.Headers.Should().ContainSingle(h => h.Key == "X-Honeycomb-Team");
            clientStub.RequestSubmitted.Headers.GetValues("X-Honeycomb-Team").Should().ContainSingle().Which.Should().Be(apiKey);
        }

        [Fact]
        public async Task Emit_WhenNoCustomSinkUriIsSet_UsesDefaultUri()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            await sut.EmitTestable(new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("", Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>()));

            clientStub.RequestSubmitted.Should().NotBeNull();
            clientStub.RequestSubmitted!.RequestUri!.Host.Should().Be("api.honeycomb.io");
            clientStub.RequestSubmitted!.RequestUri!.Scheme.Should().Be("https");
        }

        [Fact]
        public async Task Emit_WhenCustomSinkUriIsSet_UsesCustomUri()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);
            var customUri = new UriBuilder("https", "dummyhost");

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub, customUri.ToString());

            await sut.EmitTestable(new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("", Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>()));

            clientStub.RequestSubmitted.Should().NotBeNull();
            clientStub.RequestSubmitted!.RequestUri!.Scheme.Should().Be(customUri.Scheme);
            clientStub.RequestSubmitted!.RequestUri!.Host.Should().Be(customUri.Host);
        }

        [Fact]
        public async Task Emit_CallsEndpointUsingTeamId()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            await sut.EmitTestable(new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("", Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>()));

            clientStub.RequestSubmitted.Should().NotBeNull();
            clientStub.RequestSubmitted!.RequestUri!.ToString().Should().EndWith(dataset);
        }

        [Fact]
        public async Task Emit_AlwaysSetsMetaAnnotationType_As_SpanEvent()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            await sut.EmitTestable(new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null,
                new MessageTemplate("", Enumerable.Empty<MessageTemplateToken>()),
                Enumerable.Empty<LogEventProperty>()));

            var requestContent = clientStub.RequestContent!;
            using (var document = JsonDocument.Parse(requestContent))
            using (new AssertionScope())
            {
                JsonElement sentEvent = document.RootElement.EnumerateArray().Single();
                JsonElement data = sentEvent.GetProperty("data");

                data.GetProperty("meta.annotation_type").Should().NotBeNull();
                data.GetProperty("meta.annotation_type").GetString().Should().Be("span_event");
            }
        }

        [Fact]
        public async Task Emit_GivenNoExceptionIsLogged_SerializesLogMessageAsJson_HasNoExceptionInMessageAsync()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            const LogEventLevel level = LogEventLevel.Fatal;

            const string? messageTemplateString = "Testing message {message}";

            var eventToSend = Some.LogEvent(level, messageTemplateString);

            await sut.EmitTestable(eventToSend);

            var requestContent = clientStub.RequestContent!;
            using (var document = JsonDocument.Parse(requestContent))
            using (new AssertionScope())
            {
                document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
                document.RootElement.GetArrayLength().Should().Be(1);
                JsonElement sentEvent = document.RootElement.EnumerateArray().Single();

                sentEvent.GetProperty("time").GetDateTimeOffset().Should().Be(eventToSend.Timestamp);
                sentEvent.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);

                JsonElement data = sentEvent.GetProperty("data");
                data.GetProperty("level").GetString().Should().Be(level.ToString());
                data.GetProperty("messageTemplate").GetString().Should().Be(messageTemplateString);
                data.TryGetProperty("exception", out var ex);
                ex.ValueKind.Should().Be(JsonValueKind.Undefined);
            }
        }

        [Fact]
        public async Task Emit_GivenAnExceptionToLog_SerializesLogMessageAsJson_IncludesExceptionInMessageAsync()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            var level = LogEventLevel.Fatal;

            var messageTemplateString = "Testing message {message}";
            var ex = new TestException("TestException");

            var eventToSend = Some.LogEvent(level, ex, messageTemplateString);

            await sut.EmitTestable(eventToSend);

            var requestContent = clientStub.RequestContent!;
            using (var document = JsonDocument.Parse(requestContent))
            using (new AssertionScope())
            {
                document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
                document.RootElement.GetArrayLength().Should().Be(1);
                JsonElement sentEvent = document.RootElement.EnumerateArray().Single();

                sentEvent.GetProperty("time").GetDateTimeOffset().Should().Be(eventToSend.Timestamp);
                sentEvent.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);
                JsonElement data = sentEvent.GetProperty("data");

                data.GetProperty("level").GetString().Should().Be(level.ToString());
                data.GetProperty("exception.type").GetString().Should().Be(ex.GetType().ToString());
                data.GetProperty("exception.message").GetString().Should().Be(ex.ToString());
                data.GetProperty("exception.stacktrace").GetString().Should().Be(ex.StackTrace);

            }
        }

        [Fact]
        public async Task Emit_GivenAMessageWithProperties_SendsThemAllAsync()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            var level = LogEventLevel.Fatal;

            const string property = nameof(property);

            var messageTemplateString = $"Testing message property {{{nameof(property)}}}";

            var eventToSend = Some.LogEvent(level, messageTemplateString, property);

            await sut.EmitTestable(eventToSend);

            var requestContent = clientStub.RequestContent!;
            using (var document = JsonDocument.Parse(requestContent))
            using (new AssertionScope())
            {
                document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
                document.RootElement.GetArrayLength().Should().Be(1);
                JsonElement sentEvent = document.RootElement.EnumerateArray().Single();

                sentEvent.GetProperty("time").GetDateTimeOffset().Should().Be(eventToSend.Timestamp);
                sentEvent.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);

                JsonElement data = sentEvent.GetProperty("data");

                data.GetProperty(nameof(property)).GetString().Should().Be(property);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Emit_GivenAMessageWithEmptyPropertyValue_SkipsSendingProperty(string property)
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            var level = LogEventLevel.Fatal;

            var messageTemplateString = $"Testing message property {{{nameof(property)}}}";

            var eventToSend = Some.LogEvent(level, messageTemplateString, property);

            await sut.EmitTestable(eventToSend);

            var requestContent = clientStub.RequestContent!;
            using (var document = JsonDocument.Parse(requestContent))
            using (new AssertionScope())
            {
                document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
                document.RootElement.GetArrayLength().Should().Be(1);
                JsonElement sentEvent = document.RootElement.EnumerateArray().Single();

                sentEvent.GetProperty("time").GetDateTimeOffset().Should().Be(eventToSend.Timestamp);
                sentEvent.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);

                JsonElement data = sentEvent.GetProperty("data");

                var exists = data.TryGetProperty(nameof(property), out _);

                exists.Should().BeFalse();
            }
        }

        private static HoneycombSerilogSinkStub CreateSut(string dataset, string apiKey, HttpClient client)
            => new(client, dataset, apiKey);

        private static HoneycombSerilogSinkStub CreateSut(string dataset, string apiKey, HttpClient client, string honeycombUrl)
            => new(client, dataset, apiKey, honeycombUrl);
    }

    internal class TestException : Exception
    {
        public TestException(string message) : base(message)
        {
        }

        public override string StackTrace => nameof(StackTrace);
    }
}
