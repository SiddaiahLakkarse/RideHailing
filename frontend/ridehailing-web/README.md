# RideFlow React Frontend

The RideFlow frontend is a React, TypeScript, and Vite web application for the RideHailing platform.

## Run Locally

```powershell
npm install
npm run dev
```

The default development mode uses local mock data. To connect to the ASP.NET Core API, copy `.env.example` to `.env` and configure:

```env
VITE_USE_MOCKS=false
VITE_API_URL=http://localhost:5274
```

## Build

```powershell
npm run build
```

## Workspaces

- `/admin` — operations overview, rides, drivers, riders, and payments
- `/rider` — booking, fare estimation, active ride, maps, and history
- `/driver` — availability, ride offers, active trips, and earnings

The workspace switcher in the sidebar makes each role available during development. Authentication tokens are stored in browser local storage, and API requests use the bearer token when API mode is enabled.

## Maps

The frontend uses Leaflet with OpenStreetMap tiles. An internet connection is required to load map tiles. OpenStreetMap attribution is displayed on the map as required by the tile service.

## Related Documentation

See the repository [README](../../README.md) for the complete project overview, solution structure, API information, development accounts, and testing commands.
