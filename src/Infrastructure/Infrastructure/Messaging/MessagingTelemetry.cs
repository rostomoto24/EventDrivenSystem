using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;

namespace ReliableEvents.Sample.Infrastructure.Messaging;

public static class MessagingTelemetry
{
    public const string ActivitySourceName = "ReliableEvents.Sample.Messaging";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public static void InjectTraceContext(IBasicProperties properties, PropagationContext context)
    {
        Propagator.Inject(context, properties, InjectHeaderValue);
    }

    public static PropagationContext ExtractTraceContext(IBasicProperties properties)
    {
        return Propagator.Extract(default, properties, ExtractHeaderValues);
    }

    private static void InjectHeaderValue(IBasicProperties properties, string key, string value)
    {
        properties.Headers ??= new Dictionary<string, object>();
        properties.Headers[key] = value;
    }

    private static IEnumerable<string> ExtractHeaderValues(IBasicProperties properties, string key)
    {
        if (properties.Headers is null || !properties.Headers.TryGetValue(key, out var headerValue))
        {
            return Array.Empty<string>();
        }

        return headerValue switch
        {
            byte[] bytes => [System.Text.Encoding.UTF8.GetString(bytes)],
            string text => [text],
            _ => Array.Empty<string>()
        };
    }
}
