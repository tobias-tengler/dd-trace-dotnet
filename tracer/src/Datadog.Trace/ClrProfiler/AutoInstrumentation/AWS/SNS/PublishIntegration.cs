// <copyright file="PublishIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.ComponentModel;
using Datadog.Trace.ClrProfiler.CallTarget;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Propagators;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS.SNS
{
    /// <summary>
    /// AWSSDK.SNS Publish CallTarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = "AWSSDK.SimpleNotificationService",
        TypeName = "Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient",
        MethodName = "Publish",
        ReturnTypeName = "Amazon.SimpleNotificationService.Model.PublishResponse",
        ParameterTypeNames = new[] { "Amazon.SimpleNotificationService.Model.PublishRequest" },
        MinimumVersion = "3.0.0",
        MaximumVersion = "3.*.*",
        IntegrationName = AwsSnsCommon.IntegrationName)]
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PublishIntegration
    {
        private const string Operation = "Publish";

        /// <summary>
        /// OnMethodBegin callback
        /// </summary>
        /// <typeparam name="TTarget">Type of the target</typeparam>
        /// <typeparam name="TPublishRequest">Type of the request object</typeparam>
        /// <param name="instance">Instance value, aka `this` of the instrumented method</param>
        /// <param name="request">The request for the SNS operation</param>
        /// <returns>CallTarget state value</returns>
        internal static CallTargetState OnMethodBegin<TTarget, TPublishRequest>(TTarget instance, TPublishRequest request)
            where TPublishRequest : IPublishRequest, IDuckType
        {
            if (request.Instance is null)
            {
                return CallTargetState.GetDefault();
            }

            var scope = AwsSnsCommon.CreateScope(Tracer.Instance, Operation, SpanKinds.Producer, out var tags);
            if (tags is not null && request.TopicArn is not null)
            {
                tags.TopicArn = request.TopicArn;
                tags.TopicName = AwsSnsCommon.GetTopicName(request.TopicArn);
            }

            var context = new PropagationContext(scope?.Span.Context, Baggage.Current);
            ContextPropagation.InjectHeadersIntoMessage<TTarget, TPublishRequest>(request, context);

            return new CallTargetState(scope);
        }

        internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
        {
            state.Scope.DisposeWithException(exception);
            return new CallTargetReturn<TReturn>(returnValue);
        }
    }
}
