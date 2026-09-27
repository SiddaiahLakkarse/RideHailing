# RideHailing Project Information

## 1. Project Purpose

RideHailing connects riders who need transportation with nearby drivers. The MVP targets one geographic market while keeping the domain model extensible for cities, vehicle categories, surge pricing, promotions, larger fleets, and provider integrations.

## 2. Implemented Product Areas

### Rider

- Registration/login flow.
- Pickup and destination input.
- Fare estimate view.
- Ride request.
- Active ride status and driver details.
- Ride history.
- Payment-ready flow.
- Rating-ready flow.

### Driver

- Driver onboarding.
- Admin approval.
- Vehicle registration.
- Online/offline availability.
- Ride offer area.
- Ride acceptance and trip transitions.
- Active trip view.
- Earnings view.
- Location update API/SignalR seam.

### Administration

- Operational dashboard.
- Ride monitoring.
- Rider management.
- Driver onboarding/availability visibility.
- Payment reconciliation view.
- Revenue and performance metrics.

### Platform

- Fare calculation.
- Ride state machine.
- Assignment concurrency guard.
- Payment abstraction.
- Ratings validation.
- SignalR ride groups.
- Background worker boundaries.
- EF Core SQL Server model.
- In-memory development mode.

## 3. Projects

| Project | Purpose |
|---|---|
| `RideHailing.Domain` | Entities, enums, value objects, lifecycle rules, domain errors |
| `RideHailing.Application` | Use-case orchestration, pricing, authentication, driver and ride services |
| `RideHailing.Contracts` | API request and response models |
| `RideHailing.Infrastructure` | EF Core context, SQL mappings, repositories, provider adapters |
| `RideHailing.Api` | REST API, SignalR hub, dependency injection, error handling |
| `RideHailing.Workers` | Matching, reconciliation, notification, cleanup, and outbox worker hosts |
| `RideHailing.UnitTests` | Domain, pricing, assignment, and end-to-end workflow tests |
| `frontend/ridehailing-web` | React + TypeScript + Vite admin, rider, and driver web workspaces |

## 4. Backend API Surface

Base path: `/api/v1`.

### Authentication

```text
POST /auth/register
POST /auth/login
POST /auth/refresh
POST /auth/logout
```

### Rides

```text
POST /rides/estimate
POST /rides
GET  /rides/{rideId}
POST /rides/{rideId}/cancel
POST /rides/{rideId}/accept
POST /rides/{rideId}/reject
POST /rides/{rideId}/arrived
POST /rides/{rideId}/start
POST /rides/{rideId}/complete
POST /rides/{rideId}/payment
POST /rides/{rideId}/rating
POST /rides/{rideId}/location
```

### Drivers

```text
POST /drivers/onboarding
POST /drivers/{driverId}/approve
PUT  /drivers/{driverId}/availability
POST /drivers/{driverId}/vehicles
GET  /drivers/{driverId}/rides/active
GET  /drivers/{driverId}/earnings
```

### Administration

```text
GET /admin/riders
GET /admin/drivers
GET /admin/rides
GET /admin/payments
```

### SignalR

```text
/hubs/rides
```

Supported hub operations include joining/leaving ride groups and broadcasting validated driver locations.

## 5. Domain Workflow

```text
Register rider
	↓
Register/onboard driver
	↓
Approve driver and add vehicle
	↓
Driver goes online
	↓
Rider requests estimate
	↓
Rider creates ride
	↓
Ride enters MATCHING
	↓
Eligible driver receives offer
	↓
Driver accepts atomically
	↓
Driver arrives
	↓
Trip starts
	↓
Trip completes
	↓
Payment is processed
	↓
Ride becomes COMPLETED
	↓
Rider submits rating
```

## 6. Pricing

The fare calculator supports:

```text
Base fare
+ Distance km × per-km rate
+ Duration minutes × per-minute rate
+ Booking fee
+ Taxes
- Discounts
```

The result is bounded by the configured minimum fare. Production pricing should be selected by city, vehicle type, effective time, and active version.

## 7. Testing

Current unit coverage includes:

- Minimum fare and tax calculation.
- Invalid ride transitions.
- Single-winner assignment.
- Complete rider-to-rating workflow.

Recommended additions before production:

- SQL Server integration tests.
- Redis location and geo-index tests.
- 100 concurrent acceptance test.
- Payment webhook idempotency tests.
- Outbox retry and duplicate publish tests.
- SignalR authorization tests.
- API authorization/object-access tests.
- Browser tests for each React workspace.

## 8. Milestones

### M1 Foundation

Solution, domain, API, EF Core, authentication, logging, and environments.

### M2 Rider and Driver Management

Profiles, onboarding, vehicles, availability, and admin operations.

### M3 Maps and Pricing

Routing provider, geocoding, configurable pricing, geography storage.

### M4 Ride Lifecycle

Ride creation, state machine, cancellation, matching, trip progression, history.

### M5 Real-Time Tracking

SignalR authorization, Redis locations, live rider tracking, reconnect recovery.

### M6 Payments

Cash, online provider, status reconciliation, webhook signatures, refunds, idempotency.

### M7 Production Hardening

JWT policies, OpenTelemetry, alerts, load testing, backups, recovery, security scans, and deployment automation.

## 9. Current Development Limitations

The repository includes deterministic development adapters so the system can run without external services. Production deployment still requires:

- SQL Server connection and reviewed EF migrations.
- JWT validation and claim-based authorization.
- Redis connection and geo commands.
- Message broker and outbox publisher.
- Maps/routing credentials.
- Payment provider credentials and webhook signing secret.
- Push notification provider.
- Managed secret storage and TLS.
- Real authorization for SignalR ride groups.

The frontend defaults to mock data and switches to the backend with environment variables. The development role switcher is not a replacement for server-side role authorization.

## 10. Related Documentation

- [README and runbook](../README.md)
- [UML diagrams](uml-diagrams.md)
- [Database details](database-details.md)
- [Architecture](architecture.md)
