# 🚚 Delivery Truck Monitor

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Status](https://img.shields.io/badge/status-Active-brightgreen)](https://github.com)

A real-time delivery truck monitoring system that tracks GPS positions, computes proximity status, and broadcasts updates via **HTTP**, **WebSockets**, and **MQTT**.

## ✨ Features

- 📍 **Real-time GPS Tracking** – Truck simulators send position updates via HTTP every 2 seconds
- 📡 **Live Dashboard** – WebSocket-powered browser UI with interactive Leaflet map
- 🚨 **Smart Status Alerts** – Automatic OK/ALERT/STOP status based on distance to delivery nodes
- 📢 **MQTT Pub/Sub** – Backend publishes status changes to truck simulators for immediate feedback
- 🏗️ **Clean Architecture** – Dependency injection, interface-based design, circular dependency resolved
- 🎯 **Multi-truck Support** – Handle multiple simulators simultaneously

## 🛠️ Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | ASP.NET Core 8, C# |
| Frontend | HTML5, JavaScript, Leaflet 1.9.4 |
| Real-time | WebSockets (push to dashboard) |
| IoT Protocol | MQTT (Mosquitto broker) |
| Serialization | JSON (System.Text.Json) |
| Package Manager | NuGet (MQTTnet) |

## 📊 Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Browser Dashboard                         │
│  (HTML/JS + Leaflet Map + WebSocket client)                 │
└──────────────────────┬──────────────────────────────────────┘
                       │ WebSocket (ws://localhost:5295/ws)
                       │ Receives Vehicle JSON in real-time
┌──────────────────────▼──────────────────────────────────────┐
│           ASP.NET Backend (Program.cs + Controllers)         │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ DeliveryManager (in-memory truck state)                │ │
│  │  • Stores trucks, delivery nodes, WebSocket clients    │ │
│  │  • Computes distance & status (OK/ALERT/STOP)         │ │
│  │  • Broadcasts updates to all connected dashboards      │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ MqttPublisherService (INotificationPublisher)          │ │
│  │  • Publishes status changes to trucks/{id}/status      │ │
│  │  • Connects to MQTT broker (localhost:1883)            │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────┬──────────────────────────────────────────────────┘
           │ HTTP POST /api/vehicles/update
           │ (receives GPS updates every 2 sec)
           │
           │ MQTT (trucks/{id}/status topic)
           │ (receives status alerts)
┌──────────▼──────────────────────────────────────────────────┐
│              Truck Simulator (Console App)                   │
│  • Sends random/drifting GPS to backend via HTTP            │
│  • Subscribes to MQTT for status changes                    │
│  • Freezes when STOP status is received                     │
└──────────────────────────────────────────────────────────────┘
```

## 🚦 Status Logic

Each truck's status is determined by its distance to the **nearest delivery node**:

| Distance | Status | Indicator |
|----------|--------|-----------|
| ≤ 5 km   | OK     | ✅ Green  |
| 5–8 km   | ALERT  | ⚠️ Orange |
| > 8 km   | STOP   | 🛑 Red    |

## 📡 Communication Protocols

### HTTP (Truck → Backend)
**Endpoint:** `POST /api/vehicles/update`  
**Payload:**
```json
{
  "id": "TRUCK-003",
  "latitude": 34.0512,
  "longitude": -118.2521
}
```

### WebSocket (Backend → Dashboard)
**URL:** `ws://localhost:5295/ws`  
**Message:** Serialized `Vehicle` object with status and distance
```json
{
  "id": "TRUCK-003",
  "latitude": 34.0512,
  "longitude": -118.2521,
  "status": "ALERT",
  "distanceToNearestNode": 6.5,
  "nearestNodeName": "Downtown Warehouse",
  "lastUpdate": "2025-12-07T05:10:00Z"
}
```

### MQTT (Backend → Truck Simulators)
**Topic:** `trucks/{truckId}/status`  
**Payload:**
```json
{
  "truckId": "TRUCK-003",
  "status": "STOP",
  "distance": 9.2,
  "timestamp": "2025-12-07T05:10:00Z"
}
```

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- MQTT Broker (Mosquitto): `brew install mosquitto && mosquitto -c /usr/local/etc/mosquitto/mosquitto.conf`
- Browser (Chrome, Firefox, Safari)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/delivery-truck-monitor.git
   cd delivery-truck-monitor
   ```

2. **Start MQTT broker** (new terminal)
   ```bash
   mosquitto
   ```

3. **Run the backend** (new terminal)
   ```bash
   cd vehicule_tracker2._0
   dotnet run
   ```
   - Backend ready at `http://localhost:5295`
   - Dashboard at `http://localhost:5295`
   - WebSocket at `ws://localhost:5295/ws`

4. **Run truck simulator(s)** (new terminal)
   ```bash
   cd TruckSimulator
   dotnet run
   ```
   - Change `TRUCK_ID` in `Program.cs` to run multiple simulators

5. **Open dashboard**
   - Navigate to `http://localhost:5295` in your browser
   - Watch trucks move and change status on the map

## 🔧 API Reference

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/nodes` | GET | List all delivery nodes |
| `/api/vehicles/update` | POST | Update truck position |
| `/api/vehicles/status/{status}` | GET | Filter trucks by status (OK/ALERT/STOP) |
| `/ws` | WS | WebSocket for live updates |

## 🏗️ Design Patterns Used

### Dependency Injection (DI)
- `DeliveryManager` and `MqttPublisherService` registered as singletons in ASP.NET DI container
- Resolves circular dependency via `INotificationPublisher` interface

### Repository Pattern
- In-memory `Dictionary<string, Vehicle>` acts as truck repository
- Single source of truth for vehicle state

### Observer Pattern
- WebSocket clients subscribe to truck updates
- Backend broadcasts to all connected clients

### Pub/Sub (MQTT)
- Backend publishes status changes to topics
- Truck simulators subscribe and react to alerts

## 🛑 Circular Dependency Resolution

**Problem:** Original design had `DeliveryManager` and `MqttPublisherService` referencing each other.  
**Solution:** Introduced `INotificationPublisher` interface:
- `DeliveryManager` depends on abstraction, not concrete class
- `MqttPublisherService` implements the interface with no reverse dependency
- DI container can now resolve both cleanly

## 📊 Example Workflow

1. **Simulator sends GPS** → HTTP POST to `/api/vehicles/update`
2. **Backend receives update** → `DeliveryManager` updates truck position
3. **Backend computes status** → Distance to nearest node determines OK/ALERT/STOP
4. **Status changed?** → `MqttPublisherService` publishes to `trucks/{id}/status`
5. **Simulator receives MQTT** → Updates `currentStatus` and freezes if STOP
6. **Backend broadcasts** → WebSocket pushes updated vehicle to all dashboards
7. **Dashboard updates** → Marker moves, color changes, info panel updates

## 📁 Project Structure

```
delivery-truck-monitor/
├── vehicule_tracker2._0/              # ASP.NET Backend
│   ├── Program.cs                     # DI, HTTP, WebSocket, MQTT setup
│   ├── DeliveryManager.cs             # Core logic: trucks, nodes, status
│   ├── MqttPublisherService.cs        # MQTT client (INotificationPublisher)
│   ├── INotificationPublisher.cs      # Interface (breaks circular dependency)
│   ├── Models/
│   │   ├── Vehicle.cs
│   │   ├── LocationNode.cs
│   │   └── VehicleUpdate.cs
│   └── Controllers/
│       └── VehiclesController.cs      # REST endpoints
├── TruckSimulator/
│   ├── Program.cs                     # Truck simulator console app
│   └── .csproj
├── wwwroot/
│   └── index.html                     # Dashboard UI (Leaflet + WebSocket)
├── README.md
└── .gitignore
```

## 🧪 Testing

Run multiple truck simulators to test concurrent behavior:
```bash
# Terminal 1: Simulator stays in ALERT
dotnet run --project TruckSimulator

# Terminal 2: Simulator drifts to STOP
cd TruckSimulator && sed 's/TRUCK-001/TRUCK-002/g' Program.cs > Program2.cs && dotnet run
```

## 📝 Future Improvements

- [ ] Database persistence (replace in-memory dictionary)
- [ ] User authentication & authorization
- [ ] Advanced filtering on dashboard (by status, distance, time)
- [ ] Historical data visualization
- [ ] Mobile app for truck drivers
- [ ] Alert notifications (email/SMS on STOP)
- [ ] Route optimization
- [ ] Geofencing for multiple zones
- [ ] Driver acknowledgment for alerts

## 🔐 Security Notes

Currently the project has no authentication. For production:
- Add JWT/OAuth authentication to API endpoints
- Use TLS/SSL for WebSocket and HTTP connections
- Implement MQTT broker authentication
- Add input validation and rate limiting
- Use environment variables for sensitive config (broker address, ports)

## 📄 License

MIT License – see [LICENSE](LICENSE) file for details

## 👨‍💻 Author

Built as a portfolio project demonstrating real-time system design with HTTP, WebSockets, MQTT, and clean architecture principles.

## 📞 Support

For questions or issues, please open a [GitHub issue](https://github.com/yourusername/delivery-truck-monitor/issues).

---

**Last Updated:** December 7, 2025  
**Status:** Active & Maintained
