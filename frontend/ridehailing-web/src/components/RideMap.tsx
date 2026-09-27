import { useEffect } from 'react';
import { MapContainer, Marker, Polyline, TileLayer, useMap } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

export type MapPoint = [number, number];

type RideMapProps = {
  pickup: MapPoint;
  destination?: MapPoint;
  driver?: MapPoint;
  className?: string;
  label?: string;
};

const pickupIcon = L.divIcon({
  className: 'ride-map-marker pickup-marker',
  html: '<span>●</span>',
  iconSize: [30, 30],
  iconAnchor: [15, 15],
});

const destinationIcon = L.divIcon({
  className: 'ride-map-marker destination-marker',
  html: '<span>◆</span>',
  iconSize: [30, 30],
  iconAnchor: [15, 15],
});

const driverIcon = L.divIcon({
  className: 'ride-map-marker driver-marker',
  html: '<span>●</span>',
  iconSize: [30, 30],
  iconAnchor: [15, 15],
});

function FitRideBounds({ points }: { points: MapPoint[] }) {
  const map = useMap();

  useEffect(() => {
    if (points.length > 1) {
      map.fitBounds(L.latLngBounds(points), { padding: [40, 40] });
    } else {
      map.setView(points[0], 13);
    }
  }, [map, points]);

  return null;
}

export default function RideMap({ pickup, destination, driver, className = '', label }: RideMapProps) {
  const points = [pickup, destination, driver].filter((point): point is MapPoint => Boolean(point));

  return (
    <div className={`ride-map ${className}`}>
      <MapContainer center={pickup} zoom={13} scrollWheelZoom className="ride-map-container">
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <FitRideBounds points={points} />
        <Marker position={pickup} icon={pickupIcon} title="Pickup location" />
        {destination && <Marker position={destination} icon={destinationIcon} title="Destination" />}
        {driver && <Marker position={driver} icon={driverIcon} title="Driver location" />}
        {destination && <Polyline positions={[pickup, destination]} pathOptions={{ color: '#278ef0', weight: 5, opacity: 0.85 }} />}
      </MapContainer>
      {label && <span className="ride-map-label">{label}</span>}
    </div>
  );
}
