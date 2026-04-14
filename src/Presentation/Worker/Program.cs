using ReliableEvents.Sample.Application.DependencyInjection;
using ReliableEvents.Sample.Infrastructure.DependencyInjection;
using ReliableEvents.Sample.Persistence.DependencyInjection;
using ReliableEvents.Sample.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHostedService<SampleOrderConsumerWorker>();

var host = builder.Build();
await host.RunAsync();
