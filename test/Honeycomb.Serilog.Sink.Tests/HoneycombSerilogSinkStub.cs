﻿using System;
using System.Net.Http;
using System.Threading.Tasks;

using Honeycomb.Serilog.Sink.Sink;

using Serilog.Events;

namespace Honeycomb.Serilog.Sink.Tests
{
    internal class HoneycombSerilogSinkStub : HoneycombSerilogSink
    {
        private readonly HttpClient _client;

        public HoneycombSerilogSinkStub(HttpClient? client, string dataset, string apiKey)
            : base(dataset, apiKey)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected override HttpClient Client => _client;

        public Task EmitTestable(params LogEvent[] events)
        {
            return EmitBatchAsync(events);
        }
    }
}
