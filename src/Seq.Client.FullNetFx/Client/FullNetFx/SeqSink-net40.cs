﻿// Seq Client for .NET - Copyright 2013 Continuous IT Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Seq.Client.FullNetFx
{
    class SeqSink : PeriodicBatchingSink
    {
        readonly HttpClient _httpClient;
        const string BulkUploadResource = "/api/events/raw";

        public const int DefaultBatchPostingLimit = 50;
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(2);

        public SeqSink(string serverUrl, int batchPostingLimit, TimeSpan period)
            : base(batchPostingLimit, period)
        {
            if (serverUrl == null) throw new ArgumentNullException("serverUrl");
            _httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                _httpClient.Dispose();
        }

        protected override void EmitBatch(IEnumerable<LogEvent> events)
        {
            var payload = new StringWriter();
            payload.Write("{\"events\":[");

            var formatter = new JsonFormatter();
            var delimStart = "";
            foreach (var logEvent in events)
            {
                payload.Write(delimStart);
                formatter.Format(logEvent, payload);
                delimStart = ",";
            }

            payload.Write("]}");

            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            var result = _httpClient.PostAsync(BulkUploadResource, content).Result;
            if (!result.IsSuccessStatusCode)
                SelfLog.WriteLine("Received failed result {0}: {1}", result.StatusCode, result.Content.ReadAsStringAsync().Result);
        }
    }
}
