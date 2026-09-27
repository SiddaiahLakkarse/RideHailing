# RideHailing Architecture

## 1. Architecture Summary

RideHailing is a modular ASP.NET Core monolith with clear domain, application, infrastructure, API, worker, and frontend boundaries. The architecture keeps SQL Server authoritative for transactional data and uses Redis/broker infrastructure for high-frequency or asynchronous work.

```text
React Web Frontend
	| REST/JSON + SignalR
ASP.NET Core API
	|-- API controllers and SignalR hub
	|-- Application services
	|-- Domain model and state rules
	|-- Infrastructure adapters
	|-- Background worker host
	|
	|-- SQL Server: durable transactions
	|-- Redis: live locations and ephemeral matching state
	|-- Broker: outbox events and notifications
	|-- Maps: routes, distance, ETA
	|-- Payment provider: online payments and refunds
```

## 2. Repository Structure

```text
RideHailing/
├── RideHailing.slnx
├── src/
│   ├── RideHailing.Api/             REST API, SignalR, composition root
│   ├── RideHailing.Application/     Use cases, orchestration, interfaces
│   ├── RideHailing.Contracts/       Request and response contracts
│   ├── RideHailing.Domain/          Entities, enums, invariants
│   ├── RideHailing.Infrastructure/  EF Core and external adapters
│   └── RideHailing.Workers/         Matching, payment, notification workers
├── tests/
│   └── RideHailing.UnitTests/
├── frontend/
│   └── ridehailing-web/             React + TypeScript + Vite application
└── docs/
```

## 3. Layer Responsibilities

### Domain

Framework-independent business rules:

- `Ride` lifecycle and legal transitions.
- Atomic assignment intent and fare validation.
- Driver onboarding and availability rules.
- Payment and rating invariants.
- Domain error codes.

The domain must not call SQL, Redis, HTTP, or SignalR directly.

### Application

Coordinates business use cases:

- Registration, login, refresh, and logout.
- Driver onboarding, vehicle management, and availability.
- Ride creation, cancellation, matching offers, acceptance, and lifecycle commands.
- Fare calculation.
- Payment creation and rating submission.
- Location validation and publication.

Interfaces such as `IRideRepository`, `IPaymentProvider`, and location abstractions isolate external technology.

### Infrastructure

Implements persistence and providers:

- `RideDbContext` and EF Core mappings.
- SQL Server migrations and repositories.
- Redis location and geo-index adapter.
- Message-broker publisher.
- Maps/routing adapter.
- Payment provider adapter.
- Notification provider adapter.
- Local in-memory and fake adapters for development.

### API

Provides:

- `/api/v1/auth` endpoints.
- `/api/v1/rides` endpoints.
- `/api/v1/drivers` endpoints.
- `/api/v1/admin` endpoints.
- `/hubs/rides` SignalR endpoint.
- JSON error contract and HTTP status mapping.
- Authentication and authorization middleware in production configuration.

### Workers

Workers are independently scalable hosted services:

- `RideMatchingWorker`
- `PaymentReconciliationWorker`
- `NotificationWorker`
- `ExpiredRideOfferWorker`
- `DriverLocationCleanupWorker`
- `OutboxPublisherWorker`

Each worker should be idempotent, cancellation-aware, observable, and safe to run on multiple instances.

## 4. Request Flow

### Ride Creation

1. Rider submits pickup, destination, and vehicle type.
2. API validates the request and rider authorization.
3. Maps adapter calculates route distance and ETA.
4. Pricing service selects the active city/vehicle pricing rule.
5. Ride is inserted as `REQUESTED`.
6. Ride transitions to `MATCHING`.
7. Ride and outbox event commit in one SQL transaction.
8. Matching worker finds eligible drivers through Redis.
9. Driver offer is stored with a short expiration.
10. Driver accepts; SQL row lock or optimistic concurrency ensures one winner.
11. SignalR broadcasts assignment and status changes.

### Concurrent Acceptance

Use a SQL transaction with a row lock or `rowversion` check:

```sql
UPDATE Rides
SET DriverId = @DriverId,
	Status = @DriverAssigned,
	AssignedAtUtc = SYSUTCDATETIME()
WHERE Id = @RideId
  AND Status = @Matching
  AND DriverId IS NULL;
```

The application must verify that exactly one row was updated. A zero-row result is returned as `RIDE_ALREADY_ASSIGNED` or a stale-offer error.

### Location Flow

1. Driver sends location through SignalR.
2. Hub verifies the connection and ride/driver relationship.
3. Application validates coordinates, timestamp, accuracy, and movement.
4. Location service updates Redis GEO index and latest-location key.
5. Hub broadcasts `DriverLocationUpdated` to the rider’s ride group.
6. A cleanup worker removes stale driver locations.

### Payment Flow

1. Completed trip transitions to `PAYMENT_PENDING`.
2. Cash payment is confirmed by the authorized driver or operations workflow.
3. Online payment uses idempotency key `payment:{rideId}:final`.
4. Provider result is stored in SQL.
5. Webhook processing uses provider reference uniqueness and idempotent state transitions.
6. Ride completion and payment reconciliation remain separate so a payment failure does not erase the completed trip.

## 5. Frontend Architecture

The React frontend is located at `frontend/ridehailing-web`.

### Shared frontend services

- Typed API models.
- Fetch client with bearer token support.
- Mock fallback controlled by `VITE_USE_MOCKS`.
- React Router role workspaces.
- SignalR-ready live location integration seam.

### Workspaces

- `/admin`: overview, rides, drivers, riders, payments.
- `/rider`: booking, active trip, history.
- `/driver`: availability, offers, active trip, earnings.

The role switcher is a development convenience. Production authorization must derive role from validated server-issued claims rather than trusting a browser-selected role.

## 6. Security Architecture

Required production controls:

- JWT access tokens with short expiry.
- Refresh-token rotation and revocation.
- ASP.NET Core authorization policies for rider, driver, and admin roles.
- Object-level authorization: verify the authenticated user belongs to the ride.
- Rate limits on auth, ride creation, GPS, and payment endpoints.
- Password hashing through ASP.NET Core Identity or an approved password hasher.
- Payment webhook signature verification.
- HTTPS and secure headers.
- Secrets in managed secret storage.
- PII minimization and encrypted backups.
- Administrative audit events.
- No card data stored by RideHailing; use hosted/tokenized provider flows.

## 7. Reliability and Consistency

### Source of truth

- SQL Server owns ride, driver, payment, and rating truth.
- Redis is disposable and rebuilt by driver reconnect/presence flows.
- The broker is fed through the SQL outbox pattern.

### Idempotency

Use idempotency keys for:

- Payment creation.
- Webhook events.
- Outbox publishing.
- Retryable worker operations.

### Failure handling

- Retry transient provider and broker failures with backoff.
- Record retry count and last error.
- Do not retry validation or authorization failures.
- Route poison messages to a dead-letter queue.
- Keep completed rides independent from delayed payment reconciliation.

## 8. Scalability

API instances are stateless and can scale horizontally behind a load balancer. SignalR scale-out requires a managed SignalR service or Redis backplane. Matching workers use leases/locks so multiple instances do not duplicate offers. SQL indexes target rider history, driver active rides, status queues, and payment reconciliation. GPS updates must be throttled and should not be written to SQL on every update.

## 9. Observability

Instrument with OpenTelemetry and structured logging. Track:

- API latency and error rate.
- SQL and Redis latency.
- SignalR connections and location delivery latency.
- Matching latency and no-driver-found rate.
- Offer acceptance and timeout rates.
- Payment success/failure and webhook lag.
- Worker failures and broker backlog.
- Active rides, active drivers, and completed rides.

Alert on matching degradation, payment failure spikes, Redis/SQL failures, queue backlog, and SignalR delivery failures.

## 10. Deployment Topology

```text
Internet
   |
Load Balancer / TLS
   |
ASP.NET Core API instances (N)
   |-- SQL Server
   |-- Redis
   |-- Message Broker
   |-- Maps provider
   |-- Payment provider
   |
Worker instances (N)
   |-- Matching
   |-- Reconciliation
   |-- Notifications
   |-- Outbox
```

Use containers, managed SQL Server, managed Redis, managed broker, external object storage for documents, and separate staging/production configuration. Database migrations are an explicit release step.

## 11. Current Implementation vs Production Target

Implemented locally:

- Modular backend projects.
- Ride lifecycle and fare rules.
- API controllers and SignalR hub.
- React admin/rider/driver workspaces.
- In-memory runtime and fake payment adapter.
- Unit and workflow tests.

Production integrations still requiring deployment configuration or provider implementations:

- Full JWT claim-based authorization.
- SQL Server runtime connection and reviewed migrations.
- Redis geo/location adapter.
- Message broker and outbox publisher.
- Maps/routing provider.
- Online payment provider and signed webhooks.
- Push notifications.
- OpenTelemetry exporter and production dashboards.
