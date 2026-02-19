# AeroCloud PPS — Mock Passenger Processing API

A backend REST API built in **C# / .NET 10** as a supplementary project alongside my application to AeroCloud for a Software Engineer role.

I have no prior commercial .NET experience — my background is TypeScript, Node.js and AWS. I built this project over two days to get hands-on with the core stack (C#, ASP.NET Core, EF Core, LINQ, xUnit) and to demonstrate that backend fundamentals transfer quickly across ecosystems.

---

## What it does

Simulates a simplified Passenger Processing System (PPS) — the kind of system that supports airline staff at check-in, bag drop, and boarding gates.

| Resource | Description |
|---|---|
| **Passengers** | Look up by booking reference, check in, mark as boarded |
| **Bag Drop** | Register bags against a checked-in passenger, enforce weight limits |
| **Flights** | Read flight status and gate information |
| **Flight Manifest** | Full manifest with per-passenger bag stats, ordered by seat |
| **Flight Stats** | Aggregate check-in and baggage statistics via LINQ |

---

## Tech stack

| Layer | Choice | Notes |
|---|---|---|
| Framework | .NET 10 / ASP.NET Core | Web API with controller-based routing |
| ORM | Entity Framework Core 9 | SQL Server with EF migrations |
| Messaging | Azure Service Bus | Boarding events published to a topic on passenger board |
| Testing | xUnit + FluentAssertions + Moq | EF In-Memory provider for isolated unit tests |
| Docs | Swagger / Swashbuckle | Served at root in Development environment |
| CI | GitHub Actions | Build + test on every push |

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

### 1. Start SQL Server in Docker

```bash
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=YourStr0ng!Password' \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Configure the connection string

```bash
cp appsettings.example.json appsettings.json
```

Edit `appsettings.json` and fill in your SA password:

```json
"DefaultConnection": "Server=localhost,1433;Database=AeroCloudPPS;User Id=sa;Password=YourStr0ng!Password;TrustServerCertificate=True;"
```

> Note: `Trusted_Connection=True` does not work on macOS — use `User Id=sa;Password=...` instead.

### 3. Create the initial migration

```bash
dotnet ef migrations add InitialCreate
```

This generates the migration files that describe the schema. Only needed once, or after model changes.

### 4. Run the API

```bash
dotnet run
```

EF Core applies pending migrations and seeds the database automatically on startup. Swagger UI loads at `http://localhost:5000`.

Two seed flights and one checked-in passenger (booking ref `ABC123`) are pre-loaded.

### 5. Run the tests

Tests use an in-memory database and don't require SQL Server to be running.

```bash
cd Tests && dotnet test
```

---

## API overview

### Passengers
```
GET    /api/passengers/{bookingReference}        Look up passenger by PNR
POST   /api/passengers/check-in                 Check in (validated request body)
PATCH  /api/passengers/{bookingReference}/board  Mark as boarded → publishes Service Bus event
```

### Bag Drop
```
GET  /api/bagdrop/passenger/{passengerId}   List bags for a passenger
POST /api/bagdrop                           Register a new bag
```

### Flights
```
GET /api/flights                              All flights
GET /api/flights/{flightNumber}               Single flight by IATA number
GET /api/flights/{flightNumber}/manifest      Full passenger manifest ordered by seat
GET /api/flights/{flightNumber}/stats         Aggregate check-in and baggage statistics
```

### Example: end-to-end flow

```bash
# Register a bag for the seeded passenger
curl -X POST http://localhost:5000/api/bagdrop \
  -H "Content-Type: application/json" \
  -d '{"passengerId": 1, "bagTagNumber": "0123456789", "weightKg": 18.5}'

# Board the passenger (triggers Service Bus publish)
curl -X PATCH http://localhost:5000/api/passengers/ABC123/board

# Pull the flight manifest
curl http://localhost:5000/api/flights/EZY1234/manifest

# Pull aggregate stats
curl http://localhost:5000/api/flights/EZY1234/stats
```

---

## ASP.NET Core features demonstrated

### Middleware — `RequestLoggingMiddleware`
Custom middleware registered in the pipeline before routing, timing every request and emitting structured log entries (method / path / status / elapsed ms). Uses `ILogger<T>` message templates so log fields remain queryable in any structured log sink.

```
info: AeroCloud.PPS.Middleware.RequestLoggingMiddleware[0]
      GET /api/flights/EZY1234/manifest responded 200 in 14ms
```

### Action filter — `ValidateBookingReferenceFilter`
`IActionFilter` registered with the DI container and applied via `[ServiceFilter]`. Validates IATA PNR format (6 alphanumeric characters) before the action runs, short-circuiting with a structured 400 if malformed.

### Model validation — Data Annotations
Request DTOs use `[Required]`, `[StringLength]`, `[Range]` and `[RegularExpression]` attributes. ASP.NET's validation pipeline intercepts invalid requests and returns a `ValidationProblemDetails` 400 automatically — no controller guard code needed.

### Azure Service Bus — `ServiceBusBoardingEventPublisher`
When a passenger boards, a `BoardingEvent` message is published to a Service Bus topic. Downstream systems (baggage, catering, departure control) each subscribe independently — fan-out without coupling. A `NullBoardingEventPublisher` is registered automatically when no connection string is configured, so the app runs locally without Azure credentials.

---

## LINQ showcase

`GET /api/flights/{flightNumber}/manifest` and `/stats` are built entirely with LINQ. Patterns covered:

- **`Include`** — eager loading of navigation properties (bags per passenger) in a single JOIN query
- **`Where` / `OrderBy` / `ThenBy`** — filtering and multi-level ordering
- **`Select`** — projection from EF entities into response DTOs
- **`Count(predicate)`** — conditional counts (checked-in, boarded, no-show)
- **`SelectMany`** — flattening nested collections (all bags across all passengers)
- **`Sum`** — total hold weight aggregation
- **`GroupBy`** — status breakdown dictionary
- **`All`** — checking every passenger is accounted for

---

## Project structure

```
AeroCloud.PPS/
├── Controllers/
│   ├── PassengersController.cs
│   ├── BagDropController.cs
│   └── FlightsController.cs
├── Services/
│   ├── PassengerService.cs
│   ├── BagDropService.cs
│   └── FlightService.cs           # manifest + stats LINQ aggregations
├── Messaging/
│   ├── IBoardingEventPublisher.cs
│   ├── ServiceBusBoardingEventPublisher.cs
│   └── NullBoardingEventPublisher.cs
├── Middleware/
│   └── RequestLoggingMiddleware.cs
├── Filters/
│   └── ValidateBookingReferenceFilter.cs
├── Models/                         # EF Core entities
├── DTOs/                           # request/response records with Data Annotations
├── Data/
│   └── AppDbContext.cs             # DbContext with SQL Server config and seed data
├── Migrations/                     # EF Core migration history
├── Tests/
│   ├── PassengerServiceTests.cs
│   ├── BagDropServiceTests.cs
│   └── ValidateBookingReferenceFilterTests.cs
└── Program.cs
```

---

## Design decisions

**SQL Server over SQLite** — matched to the AeroCloud stack. EF Core's `EnableRetryOnFailure` is configured for transient fault handling, which is relevant for cloud SQL connections.

**Conditional Service Bus registration** — `ServiceBusBoardingEventPublisher` is only registered when a real connection string is present. Otherwise `NullBoardingEventPublisher` is used — the Null Object pattern keeps the service layer clean without scattered null checks or feature flags.

**ServiceFilter over TypeFilter** — `[ServiceFilter(typeof(...))]` is used so the filter is resolved from the DI container, allowing it to receive `ILogger<T>` without manual constructor wiring.

**Structured logging throughout** — `ILogger<T>` message templates rather than string interpolation, so log fields are named properties in any structured sink rather than buried in a flat string.

**EF In-Memory for tests** — each test class gets a fresh `Guid`-named in-memory database, so tests are fully isolated and can run in parallel without state leakage. `Moq` is used to verify the Service Bus publisher is called correctly on boarding without needing a real Azure connection.

---

## What I'd add with more time

- **Integration tests** — `WebApplicationFactory<Program>` to test the full HTTP pipeline including middleware and filters end-to-end
- **FluentValidation** — richer validation rules with better separation from DTO definitions
- **Health check endpoint** — `/health` with SQL Server and Service Bus connectivity probes
- **Pagination** — cursor-based paging on the manifest for large flights
- **Azure deployment** — App Service + Azure SQL + GitHub Actions release pipeline

---

## Background

My current stack is TypeScript / Node.js / AWS. The patterns here — dependency injection, service interfaces, async/await, structured logging, automated testing, middleware pipelines — are all familiar. The syntax and ecosystem are new. This project was built to close that gap.