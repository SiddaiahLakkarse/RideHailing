# RideHailing

RideHailing is a full-stack ride-hailing MVP that connects riders with nearby drivers. It includes rider, driver, and administration workspaces backed by a .NET 10 API and a React/Vite frontend.

## Features

### Rider

- Registration and login
- Pickup and destination entry
- Fare estimation
- Ride requests
- OpenStreetMap and Leaflet map view
- Active ride tracking view
- Ride history
- Payment-ready and rating-ready flows

### Driver

- Driver onboarding
- Admin approval
- Vehicle registration
- Online/offline availability
- Ride offers and trip transitions
- Active trip view
- Earnings view

### Administration

- Operations dashboard
- Ride monitoring
- Rider and driver management
- Payment reconciliation
- Revenue and performance metrics

### Platform

- Ride lifecycle state machine
- Fare calculation
- Assignment concurrency guard
- Authentication and registration APIs
- SignalR ride hub
- In-memory development store
- EF Core infrastructure model
- Background worker project

## Technology Stack

- .NET 10
- ASP.NET Core Web API
- SignalR
- Entity Framework Core
- React
- TypeScript
- Vite
- Leaflet and OpenStreetMap
- xUnit unit tests

## Solution Structure

| Project | Purpose |
| --- | --- |
| `src/RideHailing.Domain` | Domain entities, enums, value objects, lifecycle rules, and domain errors |
| `src/RideHailing.Application` | Authentication, pricing, driver, and ride workflow services |
| `src/RideHailing.Contracts` | API request and response models |
| `src/RideHailing.Infrastructure` | EF Core context, repositories, and provider adapters |
| `src/RideHailing.Api` | REST API, SignalR hub, dependency injection, and error handling |
| `src/RideHailing.Workers` | Background worker hosts for platform jobs |
| `tests/RideHailing.UnitTests` | Unit and workflow tests |
| `frontend/ridehailing-web` | React, TypeScript, and Vite frontend |

## Prerequisites

Install the following before running the application:

- .NET 10 SDK
- Node.js 22 or later
- npm
- Visual Studio 2026 or the .NET CLI

## Running the Application

### Visual Studio

1. Open `RideHailing.slnx` in Visual Studio.
2. Use the `RideHailing.Api` project as the startup project.
3. Start the application with **F5**.
4. In Development mode, the API starts the Vite frontend automatically.
5. Open `http://localhost:5173` if the browser does not open automatically.

### Command line

Start the backend:

```powershell
dotnet run --project .\src\RideHailing.Api\RideHailing.Api.csproj
```

Start the frontend separately when needed:

```powershell
Set-Location .\frontend\ridehailing-web
npm install
npm run dev
```

The default development URLs are:

- Frontend: `http://localhost:5173`
- API: `http://localhost:5274`
- SignalR hub: `http://localhost:5274/hubs/rides`

## Development Accounts

The API seeds these development accounts when running in Development mode:

| Role | Phone | Password |
| --- | --- | --- |
| Rider | `+1 555 0101` | `password` |
| Driver | `+1 555 0102` | `password` |
| Administrator | `+1 555 0103` | `password` |

New registrations must use a password with:

- At least 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one number
- At least one special character

Example: `RideFlow@2026`

> Development credentials and the in-memory store are not suitable for production.

## Testing

Run the test suite with:

```powershell
dotnet test .\tests\RideHailing.UnitTests\RideHailing.UnitTests.csproj
```

Build the backend solution:

```powershell
dotnet build .\RideHailing.slnx
```

Build the frontend:

```powershell
Set-Location .\frontend\ridehailing-web
npm run build
```

## API Overview

The API base path is `/api/v1`.

### Authentication

- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/refresh`
- `POST /auth/logout`

### Rides

- `POST /rides/estimate`
- `POST /rides`
- `GET /rides/{rideId}`
- `GET /rides/history?userId={userId}`
- `GET /rides/active?userId={userId}`
- `POST /rides/{rideId}/cancel`
- `POST /rides/{rideId}/accept`
- `POST /rides/{rideId}/complete`
- `POST /rides/{rideId}/payment`
- `POST /rides/{rideId}/rating`

### Drivers

- `POST /drivers/onboarding`
- `PUT /drivers/{driverId}/availability`
- `GET /drivers/{driverId}/rides/active`
- `GET /drivers/{driverId}/earnings`

### Administration

- `GET /admin/riders`
- `GET /admin/drivers`
- `GET /admin/rides`
- `GET /admin/payments`

## Configuration

Frontend development configuration is stored in:

```text
frontend/ridehailing-web/.env.development
```

Important settings include:

```text
VITE_API_URL=http://localhost:5274
VITE_USE_MOCKS=false
```

The application currently uses deterministic in-memory development adapters. Production deployment still requires SQL Server, JWT validation, Redis, a message broker, payment provider configuration, managed secrets, TLS, and production map/routing services.

## Documentation

Additional project documentation is available in:

- [Project information](docs/project-information.md)
- [Database details](docs/database-details.md)
- [UML diagrams](docs/uml-diagrams.md)

## License

No license has been selected for this repository yet.
