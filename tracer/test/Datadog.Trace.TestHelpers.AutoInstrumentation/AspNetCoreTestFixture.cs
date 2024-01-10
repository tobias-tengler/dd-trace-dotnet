// <copyright file="AspNetCoreTestFixture.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Datadog.Trace.TestHelpers
{
    public sealed class AspNetCoreTestFixture : IDisposable
    {
        private const string TracingHeaderName1WithMapping = "datadog-header-name";
        private const string TracingHeaderValue1 = "asp-net-core";
        private const string TracingHeaderName2 = "sample.correlation.identifier";
        private const string TracingHeaderValue2 = "0000-0000-0000";

        private readonly HttpClient _httpClient;
        private ITestOutputHelper _currentOutput;
        private object _outputLock = new();

        public AspNetCoreTestFixture()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add(HttpHeaderNames.TracingEnabled, "false");
            _httpClient.DefaultRequestHeaders.Add(HttpHeaderNames.UserAgent, "testhelper");
            _httpClient.DefaultRequestHeaders.Add(TracingHeaderName1WithMapping, TracingHeaderValue1);
            _httpClient.DefaultRequestHeaders.Add(TracingHeaderName2, TracingHeaderValue2);

#if NETCOREAPP2_1
            // Keep-alive is causing some weird failures on aspnetcore 2.1
            _httpClient.DefaultRequestHeaders.ConnectionClose = true;
#endif
        }

        public Process Process { get; private set; }

        public MockTracerAgent.TcpUdpAgent Agent { get; private set; }

        public int HttpPort { get; private set; }

        public void SetOutput(ITestOutputHelper output)
        {
            lock (_outputLock)
            {
                _currentOutput = output;
            }
        }

        /// <summary>
        /// Starts the test application and, if sendHealthCheck=true, sends an HTTP request
        /// with retries to application endpoint "/alive-check" and returns only after receiving
        /// a 200 status code.
        /// </summary>
        /// <param name="helper">test helper</param>
        /// <param name="enableSecurity">should asm be enabled</param>
        /// <param name="externalRulesFile">should we provide a static rule file for asm</param>
        /// <param name="sendHealthCheck">WARNING: Setting sendHealthCheck=false may potentially cause flake because we return without confirming that the application is ready to receive requests, so do this sparingly!
        /// Cases where this is needed includes testing WAF Initialization (only the first span generated by the application carries WAF information tags) and testing IAST sampling.</param>
        /// <param name="packageVersion">package version</param>
        /// <exception cref="Exception">exception, dont timeout</exception>
        /// <returns>Awaits the Response to the alive check</returns>
        public async Task TryStartApp(TestHelper helper, bool? enableSecurity = null, string externalRulesFile = null, bool sendHealthCheck = true, string packageVersion = "")
        {
            if (Process is not null)
            {
                return;
            }

            lock (this)
            {
                if (Process is null)
                {
                    var initialAgentPort = TcpPortProvider.GetOpenPort();

                    Agent = MockTracerAgent.Create(_currentOutput, initialAgentPort);
                    WriteToOutput($"Starting aspnetcore sample, agentPort: {Agent.Port}");
                    Process = helper.StartSample(Agent, arguments: null, packageVersion: packageVersion, aspNetCorePort: 0, enableSecurity: enableSecurity, externalRulesFile: externalRulesFile);

                    var mutex = new ManualResetEventSlim();

                    int? port = null;

                    Process.OutputDataReceived += (_, args) =>
                    {
                        if (args.Data != null)
                        {
                            if (args.Data.Contains("Now listening on:"))
                            {
                                var splitIndex = args.Data.LastIndexOf(':');
                                port = int.Parse(args.Data.Substring(splitIndex + 1));
                            }

                            if (args.Data.Contains("Unable to start Kestrel"))
                            {
                                mutex.Set();
                            }

                            if (args.Data.Contains("Webserver started") || args.Data.Contains("Application started"))
                            {
                                mutex.Set();
                            }

                            WriteToOutput($"[webserver][stdout] {args.Data}");
                        }
                    };

                    Process.ErrorDataReceived += (_, args) =>
                    {
                        if (args.Data != null)
                        {
                            WriteToOutput($"[webserver][stderr] {args.Data}");
                        }
                    };

                    Process.BeginOutputReadLine();
                    Process.BeginErrorReadLine();

                    if (!mutex.Wait(TimeSpan.FromSeconds(60)))
                    {
                        WriteToOutput("Timeout while waiting for the proces to start");
                    }

                    if (port == null)
                    {
                        WriteToOutput("Unable to determine port application is listening on");
                        throw new Exception("Unable to determine port application is listening on");
                    }

                    HttpPort = port.Value;
                    WriteToOutput($"Started aspnetcore sample, listening on {HttpPort}");
                }
            }

            await EnsureServerStarted(sendHealthCheck);
            Agent.SpanFilters.Add(IsNotServerLifeCheck);
        }

        public void Dispose()
        {
            if (HttpPort is not 0)
            {
                var request = WebRequest.CreateHttp($"http://localhost:{HttpPort}/shutdown");
                request.GetResponse().Close();
            }

            if (Process is not null)
            {
                try
                {
                    if (!Process.HasExited)
                    {
                        if (!Process.WaitForExit(5000))
                        {
                            Process.Kill();
                        }
                    }
                }
                catch
                {
                    // in some circumstances the HasExited property throws, this means the process probably hasn't even started correctly
                }

                Process.Dispose();
            }

            Agent?.Dispose();
        }

        public async Task<IImmutableList<MockSpan>> WaitForSpans(string path, bool post = false)
        {
            var testStart = DateTime.UtcNow;

            await SubmitRequest(path, post);
            return Agent.WaitForSpans(count: 1, minDateTime: testStart, returnAllOperations: true);
        }

        private async Task EnsureServerStarted(bool sendHealthCheck)
        {
            var maxMillisecondsToWait = 30_000;
            var intervalMilliseconds = 500;
            var intervals = maxMillisecondsToWait / intervalMilliseconds;
            var serverReady = false;

            if (sendHealthCheck)
            {
                // wait for server to be ready to receive requests
                while (intervals-- > 0)
                {
                    try
                    {
                        var dateTime = DateTime.UtcNow;
                        var respondedOk = await SubmitRequest("/alive-check") == HttpStatusCode.OK;
                        if (respondedOk)
                        {
                            Agent.WaitForSpans(1, minDateTime: dateTime);
                            serverReady = true;
                        }
                    }
                    catch
                    {
                        // ignore
                    }

                    if (serverReady)
                    {
                        break;
                    }

                    Thread.Sleep(intervalMilliseconds);
                }
            }
            else
            {
                // To minimize flake, add a large wait just to make sure the application starts up
                Thread.Sleep(maxMillisecondsToWait);
                serverReady = true;
            }

            if (!serverReady)
            {
                throw new Exception("Couldn't verify the application is ready to receive requests.");
            }
        }

        private bool IsNotServerLifeCheck(MockSpan span)
        {
            span.Tags.TryGetValue(Tags.HttpUrl, out var url);
            if (url == null)
            {
                return true;
            }

            return !url.Contains("alive-check") && !url.Contains("shutdown");
        }

        private async Task<HttpStatusCode> SubmitRequest(string path, bool post = false)
        {
            HttpResponseMessage response;
            if (!post)
            {
                response = await _httpClient.GetAsync($"http://localhost:{HttpPort}{path}");
            }
            else
            {
                response = await _httpClient.PostAsync($"http://localhost:{HttpPort}{path}", null);
            }

            string responseText = await response.Content.ReadAsStringAsync();
            WriteToOutput($"[http] {response.StatusCode} {responseText}");
            return response.StatusCode;
        }

        private void WriteToOutput(string line)
        {
            lock (_outputLock)
            {
                _currentOutput?.WriteLine(line);
            }
        }
    }
}
