using ReliableEvents.Sample.Application.DependencyInjection;
using ReliableEvents.Sample.Infrastructure.DependencyInjection;
using ReliableEvents.Sample.Infrastructure.Messaging;
using ReliableEvents.Sample.Persistence.DependencyInjection;
using ReliableEvents.Sample.Worker.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);
var serviceName = "ReliableEvents.Sample.Worker";

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
    options.AddConsoleExporter();
});

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing.AddHttpClientInstrumentation();
        tracing.AddSource(MessagingTelemetry.ActivitySourceName);
        tracing.AddConsoleExporter();
    });
builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHostedService<SampleOrderConsumerWorker>();

var host = builder.Build();
await host.RunAsync();
