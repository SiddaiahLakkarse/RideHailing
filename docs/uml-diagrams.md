# RideHailing UML Diagrams

These diagrams describe the current modular-monolith implementation and the production target integrations. The diagrams use PlantUML and can be rendered with the PlantUML extension, `plantuml.jar`, or a compatible documentation pipeline.

## 1. System Context

```plantuml
@startuml
left to right direction
actor Rider
actor Driver
actor Administrator as Admin
rectangle "React Web Frontend" as Web
rectangle "ASP.NET Core API" as Api
rectangle "SignalR Hub" as Hub
database "SQL Server" as Sql
database "Redis" as Redis
queue "Message Broker" as Broker
cloud "Maps Provider" as Maps
cloud "Payment Provider" as Payments

Rider --> Web
Driver --> Web
Admin --> Web
Web --> Api : REST/JSON
Web --> Hub : WebSocket
Api --> Sql
Api --> Redis
Api --> Maps
Api --> Payments
Api --> Broker
Hub --> Redis
@enduml
```

## 2. Backend Components

```plantuml
@startuml
package "RideHailing.Api" {
  [AuthController]
  [RidesController]
  [DriversController]
  [AdminController]
  [RideHub]
}
package "RideHailing.Application" {
  [AuthenticationService]
  [DriverService]
  [RideWorkflowService]
  [FareCalculator]
  [IPaymentProvider]
  [ApplicationStore]
}
package "RideHailing.Domain" {
  [User]
  [DriverProfile]
  [Vehicle]
  [Ride]
  [RideOffer]
  [Payment]
  [Rating]
}
package "RideHailing.Infrastructure" {
  [RideDbContext]
  [InMemoryRideRepository]
  [FakePaymentProvider]
}
package "RideHailing.Workers" {
  [RideMatchingWorker]
  [PaymentReconciliationWorker]
  [ExpiredRideOfferWorker]
  [NotificationWorker]
  [DriverLocationCleanupWorker]
  [OutboxPublisherWorker]
}

[AuthController] --> [AuthenticationService]
[RidesController] --> [RideWorkflowService]
[DriversController] --> [DriverService]
[AdminController] --> [ApplicationStore]
[RideHub] --> [RideWorkflowService]
[AuthenticationService] --> [User]
[DriverService] --> [DriverProfile]
[DriverService] --> [Vehicle]
[RideWorkflowService] --> [Ride]
[RideWorkflowService] --> [RideOffer]
[RideWorkflowService] --> [Payment]
[RideWorkflowService] --> [Rating]
[RideWorkflowService] --> [FareCalculator]
[RideWorkflowService] --> [IPaymentProvider]
[RideWorkflowService] --> [ApplicationStore]
[RideDbContext] ..> [Ride]
[RideMatchingWorker] --> [RideWorkflowService]
[PaymentReconciliationWorker] --> [IPaymentProvider]
@enduml
```

## 3. Ride Booking and Matching Sequence

```plantuml
@startuml
actor Rider
participant "React Rider UI" as UI
participant "RidesController" as Api
participant "RideWorkflowService" as Workflow
participant "FareCalculator" as Pricing
database "Ride Store" as Store
participant "RideMatchingWorker" as Matching
actor Driver
participant "RideHub" as Hub

Rider -> UI: Enter pickup and destination
UI -> Api: POST /api/v1/rides/estimate
Api -> Pricing: Calculate fare
Pricing --> Api: FareBreakdown
Api --> UI: Estimate
Rider -> UI: Confirm ride
UI -> Api: POST /api/v1/rides
Api -> Workflow: Create()
Workflow -> Pricing: Calculate()
Workflow -> Store: Save ride as MATCHING
Workflow --> Api: RideResponse
Api --> UI: Ride created
Matching -> Store: Find MATCHING rides
Matching -> Store: Find eligible drivers
Matching -> Driver: Send offer
Driver -> Api: POST /api/v1/rides/{id}/accept
Api -> Workflow: Accept()
Workflow -> Store: Atomic assignment
Workflow -> Store: DRIVER_ASSIGNED
Workflow -> Hub: Broadcast DriverAssigned
Hub --> UI: Driver assigned
@enduml
```

## 4. Ride Lifecycle State Machine

```plantuml
@startuml
[*] --> REQUESTED
REQUESTED --> MATCHING
REQUESTED --> CANCELLED
MATCHING --> DRIVER_ASSIGNED
MATCHING --> NO_DRIVER_FOUND
MATCHING --> CANCELLED
DRIVER_ASSIGNED --> DRIVER_ARRIVING
DRIVER_ASSIGNED --> CANCELLED
DRIVER_ARRIVING --> DRIVER_WAITING
DRIVER_ARRIVING --> CANCELLED
DRIVER_WAITING --> TRIP_STARTED
DRIVER_WAITING --> CANCELLED
TRIP_STARTED --> TRIP_COMPLETED
TRIP_COMPLETED --> PAYMENT_PENDING
PAYMENT_PENDING --> COMPLETED
COMPLETED --> [*]
CANCELLED --> [*]
NO_DRIVER_FOUND --> [*]
@enduml
```

## 5. Payment State Machine

```plantuml
@startuml
[*] --> PENDING
PENDING --> PROCESSING
PROCESSING --> SUCCEEDED
PROCESSING --> FAILED
FAILED --> PROCESSING : retry
SUCCEEDED --> REFUND_PENDING
REFUND_PENDING --> REFUNDED
REFUNDED --> [*]
FAILED --> [*]
@enduml
```

## 6. Driver Location and SignalR Sequence

```plantuml
@startuml
actor Driver
participant "React Driver UI" as DriverUi
participant "RideHub" as Hub
participant "RideWorkflowService" as Workflow
database Redis as Redis
participant "React Rider UI" as RiderUi

Driver -> DriverUi: GPS update
DriverUi -> Hub: UpdateDriverLocation(rideId, driverId, coordinates)
Hub -> Workflow: Validate driver/ride/coordinates
Workflow -> Redis: Store latest location
Hub -> RiderUi: DriverLocationUpdated
RiderUi -> RiderUi: Update live map
DriverUi -> Hub: JoinRide(rideId)
Hub -> Workflow: Verify ride exists and membership
Hub --> DriverUi: Group joined
@enduml
```

## 7. Database Entity Relationship Diagram

```plantuml
@startuml
hide methods
hide stereotypes
entity Users {
  * Id : uniqueidentifier
  --
  PhoneNumber : nvarchar(30) UK
  Email : nvarchar(320)
  PasswordHash : nvarchar(500)
  Role : tinyint
  Status : tinyint
  CreatedAtUtc : datetime2
}
entity Drivers {
  * Id : uniqueidentifier
  --
  UserId : uniqueidentifier UK/FK
  LicenseNumber : nvarchar(100)
  OnboardingStatus : tinyint
  AvailabilityStatus : tinyint
  Rating : decimal(3,2)
}
entity Vehicles {
  * Id : uniqueidentifier
  --
  DriverId : uniqueidentifier FK
  VehicleType : tinyint
  RegistrationNumber : nvarchar(50) UK
  Status : tinyint
}
entity Rides {
  * Id : uniqueidentifier
  --
  RiderId : uniqueidentifier FK
  DriverId : uniqueidentifier FK nullable
  Status : tinyint
  PickupLocation : geography
  DestinationLocation : geography
  EstimatedFare : decimal(12,2)
  FinalFare : decimal(12,2)
  RowVersion : rowversion
}
entity RideStatusHistory {
  * Id : bigint identity
  --
  RideId : uniqueidentifier FK
  FromStatus : tinyint
  ToStatus : tinyint
  ChangedByUserId : uniqueidentifier
}
entity Payments {
  * Id : uniqueidentifier
  --
  RideId : uniqueidentifier FK
  ProviderReference : nvarchar(200)
  Method : tinyint
  Status : tinyint
  Amount : decimal(12,2)
}
entity Ratings {
  * Id : uniqueidentifier
  --
  RideId : uniqueidentifier FK
  RaterUserId : uniqueidentifier FK
  RatedUserId : uniqueidentifier FK
  Score : tinyint
}
Users ||--o| Drivers
Drivers ||--o{ Vehicles
Users ||--o{ Rides : rider
Drivers ||--o{ Rides : driver
Rides ||--o{ RideStatusHistory
Rides ||--o{ Payments
Rides ||--o{ Ratings
Users ||--o{ Ratings : submits
@enduml
```

## 8. Concurrent Assignment Sequence

```plantuml
@startuml
participant DriverA
participant DriverB
participant API
participant Workflow
 database SQL

DriverA -> API: Accept ride
DriverB -> API: Accept ride
API -> Workflow: Accept(ride, driverA)
API -> Workflow: Accept(ride, driverB)
Workflow -> SQL: Begin transaction + row lock
Workflow -> SQL: Check status=MATCHING and DriverId=NULL
SQL --> Workflow: Driver A may assign
Workflow -> SQL: Update DriverId and status
Workflow -> SQL: Commit
Workflow -> SQL: Begin transaction + row lock
Workflow -> SQL: Check status and DriverId
SQL --> Workflow: Driver B rejected
Workflow --> API: RIDE_ALREADY_ASSIGNED
@enduml
```
