// <copyright file="IntegrationSettingsTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Datadog.Trace.Configuration;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.Configuration
{
    public class IntegrationSettingsTests
    {
        [Theory]
        [InlineData("DD_TRACE_FOO_ENABLED", "true", true)]
        [InlineData("DD_TRACE_FOO_ENABLED", "false", false)]
        [InlineData("DD_FOO_ENABLED", "true", true)]
        [InlineData("DD_FOO_ENABLED", "false", false)]
        public void IntegrationEnabled(string settingName, string settingValue, bool expected)
        {
            var source = new NameValueConfigurationSource(new NameValueCollection
                                                          {
                                                              { settingName, settingValue }
                                                          });

            var settings = new IntegrationSettings("FOO", source);
            Assert.Equal(expected, settings.Enabled);
        }

        [Theory]
        [InlineData("DD_TRACE_MYSQL_ENABLED", "false", false)]
        [InlineData("DD_TRACE_MySql_ENABLED", "false", false)]
        [InlineData("DD_TRACE_MYSQL_ENABLED", "true", true)]
        [InlineData("DD_TRACE_MySql_ENABLED", "true", true)]
        public void CaseInsenstiveIntegrationEnabled(string settingName, string settingValue, bool expected)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { settingName, settingValue },
            };
            var src = new DictionaryConfigurationSource(dict);
            var settings = new IntegrationSettings(nameof(IntegrationId.MySql), src);

            settings.EnabledInternal.Should().Be(expected);
        }

        [Theory]
        [InlineData("DD_TRACE_FOO_ANALYTICS_ENABLED", "true", true)]
        [InlineData("DD_TRACE_FOO_ANALYTICS_ENABLED", "false", false)]
        [InlineData("DD_FOO_ANALYTICS_ENABLED", "true", true)]
        [InlineData("DD_FOO_ANALYTICS_ENABLED", "false", false)]
        public void IntegrationAnalyticsEnabled(string settingName, string settingValue, bool expected)
        {
            var source = new NameValueConfigurationSource(new NameValueCollection
                                                          {
                                                              { settingName, settingValue }
                                                          });

            var settings = new IntegrationSettings("FOO", source);
            Assert.Equal(expected, settings.AnalyticsEnabled);
        }

        [Theory]
        [InlineData("DD_TRACE_FOO_ANALYTICS_SAMPLE_RATE", "0.2", 0.2)]
        [InlineData("DD_FOO_ANALYTICS_SAMPLE_RATE", "0.6", 0.6)]
        public void IntegrationAnalyticsSampleRate(string settingName, string settingValue, double expected)
        {
            var source = new NameValueConfigurationSource(new NameValueCollection
                                                          {
                                                              { settingName, settingValue }
                                                          });

            var settings = new IntegrationSettings("FOO", source);
            Assert.Equal(expected, settings.AnalyticsSampleRate);
        }
    }
}
