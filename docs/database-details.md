# RideHailing Database Details

## 1. Storage Strategy

| Concern | Store | Source of truth |
|---|---|---|
| Users, drivers, vehicles | SQL Server | SQL Server |
| Ride lifecycle and history | SQL Server | SQL Server |
| Payments and ratings | SQL Server | SQL Server |
| Pricing rules and snapshots | SQL Server | SQL Server |
| Outbox messages | SQL Server | SQL Server |
| Live driver GPS | Redis | Redis is disposable |
| Available-driver geo index | Redis GEO commands | SQL driver state remains authoritative |
| Ride offers | Redis or broker-backed ephemeral store | SQL assignment remains authoritative |

The current local implementation also provides an in-memory `ApplicationStore` and `InMemoryRideRepository` so the API and frontend can run without external infrastructure. These adapters are for development and tests, not production durability.

## 2. SQL Server Tables

### 2.1 Users

```sql
CREATE TABLE Users (
	Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
	PhoneNumber NVARCHAR(30) NOT NULL,
	Email NVARCHAR(320) NULL,
	PasswordHash NVARCHAR(500) NOT NULL,
	FirstName NVARCHAR(100) NOT NULL,
	LastName NVARCHAR(100) NULL,
	Role TINYINT NOT NULL,
	Status TINYINT NOT NULL,
	CreatedAtUtc DATETIME2 NOT NULL,
	UpdatedAtUtc DATETIME2 NOT NULL,
	CONSTRAINT UQ_Users_PhoneNumber UNIQUE (PhoneNumber)
);
CREATE INDEX IX_Users_Email ON Users(Email);
```

Roles: `1 Rider`, `2 Driver`, `3 Admin`.
Statuses: `1 Active`, `2 Suspended`.

### 2.2 Drivers

```sql
CREATE TABLE Drivers (
	Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Drivers PRIMARY KEY,
	UserId UNIQUEIDENTIFIER NOT NULL,
	LicenseNumber NVARCHAR(100) NOT NULL,
	OnboardingStatus TINYINT NOT NULL,
	AvailabilityStatus TINYINT NOT NULL,
	Rating DECIMAL(3,2) NULL,
	CreatedAtUtc DATETIME2 NOT NULL,
	UpdatedAtUtc DATETIME2 NOT NULL,
	CONSTRAINT UQ_Drivers_UserId UNIQUE (UserId),
	CONSTRAINT FK_Drivers_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
CREATE INDEX IX_Drivers_AvailabilityStatus ON Drivers(AvailabilityStatus);
CREATE INDEX IX_Drivers_OnboardingStatus ON Drivers(OnboardingStatus);
```

Onboarding: `1 Pending`, `2 Approved`, `3 Rejected`.
Availability: `1 Offline`, `2 Available`, `3 OnTrip`.

### 2.3 Vehicles

```sql
CREATE TABLE Vehicles (
	Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Vehicles PRIMARY KEY,
	DriverId UNIQUEIDENTIFIER NOT NULL,
	VehicleType TINYINT NOT NULL,
	Make NVARCHAR(100) NOT NULL,
	Model NVARCHAR(100) NOT NULL,
	RegistrationNumber NVARCHAR(50) NOT NULL,
	Color NVARCHAR(50) NULL,
	Status TINYINT NOT NULL,
	CreatedAtUtc DATETIME2 NOT NULL,
	CONSTRAINT UQ_Vehicles_RegistrationNumber UNIQUE (RegistrationNumber),
	CONSTRAINT FK_Vehicles_Drivers FOREIGN KEY (DriverId) REFERENCES Drivers(Id)
);
CREATE INDEX IX_Vehicles_DriverId ON Vehicles(DriverId);
CREATE INDEX IX_Vehicles_Type_Status ON Vehicles(VehicleType, Status);
```

Vehicle types: `1 Standard`, `2 Premium`, `3 XL`.

### 2.4 Rides

```sql
CREATE TABLE Rides (
	Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Rides PRIMARY KEY,
	RiderId UNIQUEIDENTIFIER NOT NULL,
	DriverId UNIQUEIDENTIFIER NULL,
	Status TINYINT NOT NULL,
	VehicleType TINYINT NOT NULL,
	PickupLocation GEOGRAPHY NOT NULL,
	PickupAddress NVARCHAR(500) NOT NULL,
	DestinationLocation GEOGRAPHY NOT NULL,
	DestinationAddress NVARCHAR(500) NOT NULL,
	EstimatedDistanceM INT NULL,
	ActualDistanceM INT NULL,
	EstimatedDurationSec INT NULL,
	ActualDurationSec INT NULL,
	EstimatedFare DECIMAL(12,2) NULL,
	FinalFare DECIMAL(12,2) NULL,
	Currency CHAR(3) NOT NULL,
	RequestedAtUtc DATETIME2 NOT NULL,
	AssignedAtUtc DATETIME2 NULL,
	StartedAtUtc DATETIME2 NULL,
	CompletedAtUtc DATETIME2 NULL,
	CancelledAtUtc DATETIME2 NULL,
	CreatedAtUtc DATETIME2 NOT NULL,
	UpdatedAtUtc DATETIME2 NOT NULL,
	RowVersion ROWVERSION NOT NULL,
	CONSTRAINT FK_Rides_Rider FOREIGN KEY (RiderId) REFERENCES Users(Id),
	CONSTRAINT FK_Rides_Driver FOREIGN KEY (DriverId) REFERENCES Drivers(Id)
);
CREATE INDEX IX_Rides_RiderId_RequestedAt ON Rides(RiderId, RequestedAtUtc DESC);
CREATE INDEX IX_Rides_DriverId_Status ON Rides(DriverId, Status);
CREATE INDEX IX_Rides_Status_RequestedAt ON Rides(Status, RequestedAtUtc);
CREATE SPATIAL INDEX IX_Rides_PickupLocation ON Rides(PickupLocation);
```

Ride statuses: `1 Requested`, `2 Matching`, `3 DriverAssigned`, `4 DriverArriving`, `5 DriverWaiting`, `6 TripStarted`, `7 TripCompleted`, `8 PaymentPending`, `9 Completed`, `10 Cancelled`, `11 NoDriverFound`.

`RowVersion` protects assignment and lifecycle updates from lost concurrent writes.

### 2.5 RideStatusHistory

```sql
CREATE TABLE RideStatusHistory (
	Id BIGINT IDENTITY(1,1) CONSTRAINT PK_RideStatusHistory PRIMARY KEY,
	RideId UNIQUEIDENTIFIER NOT NULL,
	FromStatus TINYINT NULL,
	ToStatus TINYINT NOT NULL,
	ChangedByUserId UNIQUEIDENTIFIER NULL,
	Reason NVARCHAR(500) NULL,
	CreatedAtUtc DATETIME2 NOT NULL,
	CONSTRAINT FK_RideStatusHistory_Rides FOREIGN KEY (RideId) REFERENCES Rides(Id),
	CONSTRAINT FK_RideStatusHistory_Users FOREIGN KEY (ChangedByUserId) REFERENCES Users(Id)
);
CREATE INDEX IX_RideStatusHistory_RideId_CreatedAt ON RideStatusHistory(RideId, CreatedAtUtc);
```

Every valid lifecycle transition should insert one history row in the same transaction as the ride update.

### 2.6 Payments

```sql
CREATE TABLE Payments (
	Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
	RideId UNIQUEIDENTIFIER NOT NULL,
	Provider NVARCHAR(50) NOT NULL,
	ProviderReference NVARCHAR(200) NULL,
	Method TINYINT NOT NULL,
	Status TINYINT NOT NULL,
	Amount DECIMAL(12,2) NOT NULL,
	Currency CHAR(3) NOT NULL,
	CreatedAtUtc DATETIME2 NOT NULL,
	UpdatedAtUtc DATETIME2 NOT NULL,
	CONSTRAINT FK_Payments_Rides FOREIGN KEY (RideId) REFERENCES Rides(Id),
	CONSTRAINT UQ_Payments_ProviderReference UNIQUE (Provider, ProviderReference)
);
CREATE INDEX IX_Payments_RideId ON Payments(RideId);
CREATE INDEX IX_Payments_Status ON Payments(Status);
```

Methods: `1 Cash`, `2 Online`.
Statuses: `1 Pending`, `2 Processing`, `3 Succeeded`, `4 Failed`, `5 RefundPending`, `6 Refunded`.

### 2.7 Ratings

```sql
CREATE TABLE Ratings (
	Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Ratings PRIMARY KEY,
	RideId UNIQUEIDENTIFIER NOT NULL,
	RaterUserId UNIQUEIDENTIFIER NOT NULL,
	RatedUserId UNIQUEIDENTIFIER NOT NULL,
	Score TINYINT NOT NULL,
	Comment NVARCHAR(1000) NULL,
	CreatedAtUtc DATETIME2 NOT NULL,
	CONSTRAINT FK_Ratings_Rides FOREIGN KEY (RideId) REFERENCES Rides(Id),
	CONSTRAINT FK_Ratings_Rater FOREIGN KEY (RaterUserId) REFERENCES Users(Id),
	CONSTRAINT FK_Ratings_RatedUser FOREIGN KEY (RatedUserId) REFERENCES Users(Id),
	CONSTRAINT UQ_Ratings_Ride_Rater_Rated UNIQUE (RideId, RaterUserId, RatedUserId),
	CONSTRAINT CK_Ratings_Score CHECK (Score BETWEEN 1 AND 5)
);
```

### 2.8 PricingRules

```sql
CREATE TABLE PricingRules (
	Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PricingRules PRIMARY KEY,
	CityId UNIQUEIDENTIFIER NULL,
	VehicleType TINYINT NOT NULL,
	BaseFare DECIMAL(12,2) NOT NULL,
	PerKmRate DECIMAL(12,2) NOT NULL,
	PerMinuteRate DECIMAL(12,2) NOT NULL,
	MinimumFare DECIMAL(12,2) NOT NULL,
	BookingFee DECIMAL(12,2) NOT NULL,
	CancellationFee DECIMAL(12,2) NOT NULL,
	TaxRate DECIMAL(6,4) NOT NULL DEFAULT 0,
	Currency CHAR(3) NOT NULL,
	EffectiveFromUtc DATETIME2 NOT NULL,
	EffectiveToUtc DATETIME2 NULL,
	IsActive BIT NOT NULL
);
CREATE INDEX IX_PricingRules_Lookup ON PricingRules(CityId, VehicleType, IsActive, EffectiveFromUtc);
```

The ride should store a pricing-rule version/snapshot so historical fares remain reproducible after an administrator changes configuration.

### 2.9 OutboxMessages

```sql
CREATE TABLE OutboxMessages (
	Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OutboxMessages PRIMARY KEY,
	EventType NVARCHAR(200) NOT NULL,
	AggregateId UNIQUEIDENTIFIER NOT NULL,
	Payload NVARCHAR(MAX) NOT NULL,
	CreatedAtUtc DATETIME2 NOT NULL,
	PublishedAtUtc DATETIME2 NULL,
	RetryCount INT NOT NULL DEFAULT 0,
	LastError NVARCHAR(2000) NULL
);
CREATE INDEX IX_OutboxMessages_Unpublished
ON OutboxMessages(CreatedAtUtc)
WHERE PublishedAtUtc IS NULL;
```

## 3. Geography Rules

Use SQL Server SRID 4326:

```sql
geography::Point(@Latitude, @Longitude, 4326)
```

Validate latitude `[-90, 90]`, longitude `[-180, 180]`, accuracy, timestamp freshness, and movement speed before accepting driver GPS updates.

## 4. Redis Model

```text
drivers:available                         GEOADD longitude latitude driverId
driver:{driverId}:status                  JSON status snapshot
driver:{driverId}:location                JSON latest GPS location
ride:{rideId}:matching                    matching lease/lock
ride:{rideId}:offer:{driverId}            offer with expiration
```

Redis is a candidate index and ephemeral state store. SQL Server remains authoritative for ride assignment, payment status, and durable lifecycle history.

## 5. EF Core and Migrations

The current `RideDbContext` is migration-ready and contains sets/mappings for rides, users, drivers, vehicles, payments, and ratings. Before production deployment:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/RideHailing.Infrastructure --startup-project src/RideHailing.Api
dotnet ef database update --project src/RideHailing.Infrastructure --startup-project src/RideHailing.Api
```

Use reviewed, version-controlled migrations in CI/CD. Do not automatically migrate a production database during API startup.

## 6. Operational Requirements

- Automated SQL backups and point-in-time recovery.
- Query/index monitoring for ride matching and history queries.
- Retention policy for GPS data and audit records.
- Encryption at rest and TLS in transit.
- Secrets stored outside source control.
- Regular recovery and migration tests.
- Separate development, staging, and production databases.
