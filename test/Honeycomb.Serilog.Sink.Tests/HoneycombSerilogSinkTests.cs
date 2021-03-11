using System;
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
            Action action = () => CreateSut(dataset, apiKey);

            action.Should().Throw<ArgumentNullException>()
                  .Which.Message.Should().Contain(nameof(dataset));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Create_WhenInvalidApiKeyIsProvided_ThrowsArgumentException(string apiKey)
        {
            const string dataset = nameof(dataset);
            Action action = () => CreateSut(dataset, apiKey);

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

            clientStub.RequestSubmitted.Headers.Should().ContainSingle(h => h.Key == "X-Honeycomb-Team");
            clientStub.RequestSubmitted.Headers.GetValues("X-Honeycomb-Team").Should().ContainSingle().Which.Should().Be(apiKey);
        }

        [Fact]
        public async Task Emit_CallsEndpointUsingTeamId()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            await sut.EmitTestable(new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate("", Enumerable.Empty<MessageTemplateToken>()), Enumerable.Empty<LogEventProperty>()));

            clientStub.RequestSubmitted.RequestUri.ToString().Should().EndWith(dataset);
        }

        [Fact]
        public async Task Emit_GivenNoExceptionIsLogged_SerializesLogMessageAsJson_HasNoExceptionInMessageAsync()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            var level = LogEventLevel.Fatal;

            var messageTempalteString = "Testing message {message}";

            var eventToSend = Some.LogEvent(level, messageTempalteString);

            await sut.EmitTestable(eventToSend);

            var requestContent = clientStub.RequestContent;
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
                data.GetProperty("messageTemplate").GetString().Should().Be(messageTempalteString);
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

            var messageTempalteString = "Testing message {message}";
            var ex = new Exception("TestException");

            var eventToSend = Some.LogEvent(level, ex, messageTempalteString);

            await sut.EmitTestable(eventToSend);

            var requestContent = clientStub.RequestContent;
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
                data.GetProperty("messageTemplate").GetString().Should().Be(messageTempalteString);
                data.GetProperty("exception").GetString().Should().Be(ex.ToString());
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

            var messageTempalteString = $"Testing message property {{{nameof(property)}}}";

            var eventToSend = Some.LogEvent(level, messageTempalteString, property);

            await sut.EmitTestable(eventToSend);

            var requestContent = clientStub.RequestContent;
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

            var requestContent = clientStub.RequestContent;
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

        [Fact]
        public async Task Emit_GivenAMessageWithTraceId_WritesItAsOTELStandard()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            var level = LogEventLevel.Information;

            var property = 1;
            const string spanId = nameof(spanId);

            var messageTemplateString = $"Testing message property {{{nameof(property)}}} {{TraceId}}";

            var eventToSend = Some.LogEvent(level, messageTemplateString, property, spanId);


            await sut.EmitTestable(eventToSend);

            var requestContent = clientStub.RequestContent;
            using (var document = JsonDocument.Parse(requestContent))
            using (new AssertionScope())
            {
                document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
                document.RootElement.GetArrayLength().Should().Be(1);
                JsonElement sentEvent = document.RootElement.EnumerateArray().Single();

                sentEvent.GetProperty("time").GetDateTimeOffset().Should().Be(eventToSend.Timestamp);
                sentEvent.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);
                sentEvent.GetProperty("data").GetProperty("trace.trace_id").ValueKind.Should().Be(JsonValueKind.String);
                sentEvent.GetProperty("data").GetProperty("trace.trace_id").GetString().Should().Be(spanId);
            }
        }

        [Fact]
        public async Task Emit_GivenAMessageWithParentId_WritesItAsOTELStandard()
        {
            const string dataset = nameof(dataset);
            const string apiKey = nameof(apiKey);

            HttpClientStub clientStub = A.HttpClient();

            var sut = CreateSut(dataset, apiKey, clientStub);

            var level = LogEventLevel.Information;

            var property = 1;
            const string parentId = nameof(parentId);

            var messageTemplateString = $"Testing message property {{{nameof(property)}}} {{SpanId}}";

            var eventToSend = Some.LogEvent(level, messageTemplateString, property, parentId);


            await sut.EmitTestable(eventToSend);

            var requestContent = clientStub.RequestContent;
            using (var document = JsonDocument.Parse(requestContent))
            using (new AssertionScope())
            {
                document.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
                document.RootElement.GetArrayLength().Should().Be(1);
                JsonElement sentEvent = document.RootElement.EnumerateArray().Single();

                sentEvent.GetProperty("time").GetDateTimeOffset().Should().Be(eventToSend.Timestamp);
                sentEvent.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);
                sentEvent.GetProperty("data").GetProperty("trace.parent_id").ValueKind.Should().Be(JsonValueKind.String);
                sentEvent.GetProperty("data").GetProperty("trace.parent_id").GetString().Should().Be(parentId);
            }
        }

        private HoneycombSerilogSinkStub CreateSut(string dataset, string apiKey, HttpClient client = null)
        {
            return new HoneycombSerilogSinkStub(client, dataset, apiKey);
        }
    }
}
