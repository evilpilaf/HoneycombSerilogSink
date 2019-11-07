using System;
using System.Net.Http;
using FluentAssertions;
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
            Action action = () => new HoneycombSerilogSink(teamId, apiKey);

            action.Should().Throw<ArgumentNullException>()
                  .Which.Message.Should().Contain(nameof(teamId));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Create_WhenInvalidApiKeyIsProvided_ThrowsArgumentException(string apiKey)
        {
            const string teamId = nameof(teamId);
            Action action = () => new HoneycombSerilogSink(teamId, apiKey);

            action.Should().Throw<ArgumentNullException>()
                  .Which.Message.Should().Contain(nameof(apiKey));
        }

        private HoneycombSerilogSink CreateSut(HttpClient client, string teamId, string apiKey)
        {
            return new HoneycombSerilogSinkStub(client, teamId, apiKey);
        }
    }
}
