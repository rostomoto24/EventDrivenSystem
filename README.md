# ReliableEvents.Sample

A .NET 8 Clean Architecture sample implementing the **Transactional Outbox** pattern with SQL Server, RabbitMQ, and Redis idempotency checks.

## Solution Structure

```text
src/
  Core/
    Domain/
    Application/
  Infrastructure/
    Persistence/
    Infrastructure/
  Presentation/
    API/
    Worker/
tests/
  Application.Tests/
  Integration.Tests/
```

## Architecture Overview

- **Domain**: Core entities (`Order`, `OutboxMessage`).
- **Application**: MediatR use cases and abstractions.
  - `CreateOrderCommand` stores the `Order` and a serialized `OutboxMessage` in a single EF transaction.
- **Persistence**: EF Core `AppDbContext` using SQL Server.
- **Infrastructure**:
  - `RabbitMqPublisher` for publishing events.
  - `RedisIdempotencyStore` for deduplication.
- **API**:
  - `POST /orders` sends `CreateOrderCommand` through MediatR.
- **Worker**:
  - `OutboxPublisherWorker` polls unpublished outbox rows and publishes them.
  - `SampleOrderConsumerWorker` consumes queue messages and checks Redis before processing.

## Prerequisites

- .NET SDK 8.0+
- Docker + Docker Compose

## Run Dependencies

```bash
docker compose up -d
```

Services:
- SQL Server: `localhost:1433`
- RabbitMQ broker: `localhost:5672`
- RabbitMQ management UI: `http://localhost:15672` (`guest` / `guest`)
- Redis: `localhost:6379`

## Restore / Build / Run

```bash
dotnet restore
dotnet build ReliableEvents.Sample.sln
```

Run API:

```bash
dotnet run --project src/Presentation/API/API.csproj
```

Run Worker:

```bash
dotnet run --project src/Presentation/Worker/Worker.csproj
```

## API Example

```bash
curl -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{"customerEmail":"alice@example.com","totalAmount":120.50}'
```

The API stores an order + outbox row atomically. The worker publishes outbox messages to RabbitMQ and marks them as published. The sample consumer checks Redis idempotency keys before processing.
