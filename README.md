# AeroCloud PPS — Mock Passenger Processing API

A backend REST API built in **C# / .NET 8** as a supplementary project alongside my application to AeroCloud for a Software Engineer role.

I have no prior commercial .NET experience — my background is TypeScript, Node.js and AWS. I built this project over two days to get hands-on with the core stack (C#, ASP.NET Core, EF Core, LINQ, xUnit) and to demonstrate that backend fundamentals transfer quickly across ecosystems.

---

## What it does

Simulates a simplified Passenger Processing System (PPS) — the kind of system that supports airline staff at check-in, bag drop, and boarding gates.

| Resource | Description |
|---|---|
| **Passengers** | Look up by booking reference, check in, mark as boarded |
| **Bag Drop** | Register bags against a checked-in passenger, enforce weight limits |
| **Flights** | Read flight status and gate information |
| **Flight Manifest** | LINQ-aggregated full manifest with per-passenger bag stats and hold summary |

---

## Tech stack

| Layer | Choice | Notes |
|---|---|---|
| Framework | .NET 8 / ASP.NET Core | Web API with controller-based routing |
| ORM | Entity Framework Core 8 | SQLite locally; production-ready for SQL Server swap |
| Testing | xUnit + FluentAssertions + Moq | EF In-Memory provider for isolated unit tests |
| Docs | Swagger / Swashbuckle | Served at root in development |
| CI | GitHub Actions | Build + test on every push |

---

## Getting started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run the API
```bash
git clone https://github.com/YOUR_USERNAME/aerocloud-pps.git
cd aerocloud-pps/AeroCloud.PPS
dotnet run
```

Swagger UI loads at `https://localhost:5001`. Two seed flights and one passenger are pre-loaded.

### Run the tests
```bash
dotnet test
```

---

## API overview

### Passengers
```
GET    /api/passengers/{bookingReference}        Look up passenger by PNR
POST   /api/passengers/check-in                 Check in (validated request body)
PATCH  /api/passengers/{bookingReference}/board  Mark as boarded
```

### Bag Drop
```
GET  /api/bagdrop/passenger/{passengerId}   List bags for a passenger
POST /api/bagdrop                           Register a new bag
```

### Flights
```
GET /api/flights                            All flights
GET /api/flights/{flightNumber}             Single flight by IATA number
GET /api/flights/{flightNumber}/manifest    Full passenger manifest with LINQ aggregations
```

### Example: end-to-end check-in and manifest

```bash
# Check in seeded passenger
curl -X POST https://localhost:5001/api/passengers/check-in \
  -H "Content-Type: application/json" \
  -d '{"bookingReference": "ABC123", "seatNumber": "14A"}'

# Register a bag
curl -X POST https://localhost:5001/api/bagdrop \
  -H "Content-Type: application/json" \
  -d '{"passengerId": 1, "bagTagNumber": "0123456789", "weightKg": 18.5}'

# Board the passenger
curl -X PATCH https://localhost:5001/api/passengers/ABC123/board

# Pull the full manifest (see LINQ aggregations in action)
curl https://localhost:5001/api/flights/EZY1234/manifest
```

---

## ASP.NET Core features demonstrated

### Middleware — `RequestLoggingMiddleware`
Custom middleware registered in the pipeline before routing, timing every request and emitting structured log entries (method / path / status / elapsed ms). Uses `ILogger<T>` message templates so log fields remain queryable in Azure Monitor or any structured log sink.

```
info: AeroCloud.PPS.Middleware.RequestLoggingMiddleware[0]
      GET /api/flights/EZY1234/manifest responded 200 in 14ms
```

### Action filter — `ValidateBookingReferenceFilter`
`IActionFilter` registered with the DI container (`AddScoped`) and applied via `[ServiceFilter]`. Validates IATA PNR format (6 alphanumeric characters) before the action runs, short-circuiting with a structured 400 if malformed. Centralises the check rather than repeating it across controllers.

### Model validation — Data Annotations
Request DTOs use `[Required]`, `[StringLength]`, `[Range]` and `[RegularExpression]` attributes. ASP.NET's model validation pipeline intercepts invalid requests automatically and returns a `ValidationProblemDetails` 400 — no controller guard code needed.

```json
// POST /api/passengers/check-in with bad seat "XXXX"
// → 400 Bad Request (automatic, before controller runs)
{
  "errors": {
    "SeatNumber": ["Seat number must be row + letter, e.g. 14A."]
  }
}
```

---

## LINQ showcase — Flight Manifest

`GET /api/flights/{flightNumber}/manifest` is built entirely in `FlightManifestService` using LINQ across the Passengers and Bags tables. Patterns covered:

- **`Include`** — eager loading of navigation properties (bags per passenger) in a single LEFT JOIN query
- **`Where` / `OrderBy` / `ThenBy`** — filtering and multi-level ordering
- **`Select`** — projection from EF entities into DTOs
- **`Count(predicate)`** — conditional counts (checked-in, boarded, no-show)
- **`SelectMany`** — flattening nested collections (all bags across all passengers)
- **`Sum`** — total hold weight aggregation
- **Guard against divide-by-zero** — boarding % computed excluding no-shows

Example manifest response:
```json
{
  "flightNumber": "EZY1234",
  "origin": "MAN",
  "destination": "AMS",
  "gate": "B14",
  "summary": {
    "totalPassengers": 1,
    "checkedIn": 0,
    "boarded": 1,
    "noShow": 0,
    "boardingCompletionPct": 100.0,
    "totalBags": 1,
    "totalHoldWeightKg": 18.5
  },
  "passengers": [
    {
      "fullName": "Jane Smith",
      "bookingReference": "ABC123",
      "seatNumber": "14A",
      "checkInStatus": "Boarded",
      "bagCount": 1,
      "totalBagWeightKg": 18.5
    }
  ]
}
```

---

## Project structure

```
AeroCloud.PPS/
├── Controllers/
│   ├── PassengersController.cs      # check-in, lookup, boarding
│   ├── BagDropController.cs         # bag registration
│   └── FlightsController.cs         # flight status + manifest endpoint
├── Services/
│   ├── PassengerService.cs
│   ├── BagDropService.cs
│   ├── FlightService.cs
│   └── FlightManifestService.cs     # LINQ aggregation showcase
├── Middleware/
│   └── RequestLoggingMiddleware.cs  # request timing + structured logging
├── Filters/
│   └── ValidateBookingReferenceFilter.cs  # IActionFilter for PNR format
├── Models/                          # EF Core entities
├── DTOs/                            # request/response records with Data Annotations
├── Data/
│   └── AppDbContext.cs              # DbContext with seed data
├── Tests/
│   ├── PassengerServiceTests.cs     # 9 tests
│   ├── BagDropServiceTests.cs       # 6 tests
│   ├── FlightManifestServiceTests.cs  # 7 tests — LINQ correctness
│   └── ValidateBookingReferenceFilterTests.cs  # 9 tests — filter behaviour
└── Program.cs
```

---

## Design decisions

**Middleware before routing** — `UseRequestLogging()` is registered before `MapControllers()` so it wraps the full controller execution, including routing and model binding, giving accurate end-to-end timing.

**ServiceFilter vs TypeFilter** — `[ServiceFilter(typeof(...))]` is used rather than `[TypeFilter]` because the filter is registered in DI, allowing it to receive `ILogger<T>` without manually wiring constructor arguments.

**Structured logging throughout** — message templates (`{Property}`) rather than string interpolation mean log fields are named properties in any structured log sink, not buried in a flat string.

**EF In-Memory for tests** — each test class gets a fresh `Guid`-named in-memory database, so tests are fully isolated and can run in parallel without state leakage.

---

## What I'd add with more time

- **EF Migrations** — currently `EnsureCreated()` for simplicity; production uses `Migrate()` with a migration history table
- **Integration tests** — `WebApplicationFactory<Program>` to test the full HTTP pipeline including middleware
- **FluentValidation** — richer validation rules with better separation from DTO definitions
- **Health check endpoint** — `/health` with database connectivity probe
- **Pagination** — cursor-based for the manifest at scale
- **Azure deployment** — App Service + GitHub Actions release pipeline

---

## Background

My current stack is TypeScript / Node.js / AWS. The patterns here — dependency injection, service interfaces, async/await, structured logging, automated testing, middleware pipelines — are familiar. The syntax and ecosystem are new. This project was built to close that gap and demonstrate that the fundamentals transfer.
