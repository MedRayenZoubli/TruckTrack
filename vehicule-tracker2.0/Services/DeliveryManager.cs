using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using vehicule_tracker2._0.Models;

namespace vehicule_tracker2._0.Services
{
    public class DeliveryManager
    {
        private readonly Dictionary<string, Vehicle> _trucks = new();
        private readonly List<LocationNode> _nodes = new();
        private readonly List<WebSocket> _clients = new();
        private readonly INotificationPublisher _notificationPublisher;

        public DeliveryManager(INotificationPublisher notificationPublisher)
        {
            _notificationPublisher = notificationPublisher;

            _nodes.Add(new LocationNode
            {
                Id = "NODE-001",
                Name = "Downtown Warehouse",
                Latitude = 34.05,
                Longitude = -118.25
            });

            _nodes.Add(new LocationNode
            {
                Id = "NODE-002",
                Name = "Airport Hub",
                Latitude = 34.07,
                Longitude = -118.25
            });

            _nodes.Add(new LocationNode
            {
                Id = "NODE-003",
                Name = "Harbor Depot",
                Latitude = 34.06,
                Longitude = -118.20
            });
        }

        public void UpdateVehicle(VehicleUpdate update)
        {
            if (update == null)
            {
                Console.WriteLine(" Null vehicle update received");
                return;
            }

            Vehicle truck;

            if (!_trucks.ContainsKey(update.Id))
            {
                truck = new Vehicle
                {
                    Id = update.Id,
                    Latitude = update.Latitude,
                    Longitude = update.Longitude,
                    Status = "OK",
                    LastUpdate = DateTime.UtcNow,
                    StatusChangedAt = DateTime.UtcNow
                };

                _trucks[update.Id] = truck;
                Console.WriteLine($" New truck created: {update.Id}");
            }
            else
            {
                truck = _trucks[update.Id];
                truck.Latitude = update.Latitude;
                truck.Longitude = update.Longitude;
                truck.LastUpdate = DateTime.UtcNow;
            }

            UpdateTruckAsync(truck).GetAwaiter().GetResult();
        }

        public async Task UpdateTruckAsync(Vehicle truck)
        {
            string previousStatus = _trucks.ContainsKey(truck.Id)
                ? _trucks[truck.Id].Status
                : "OK";

            string newStatus = DetermineStatus(truck);
            truck.Status = newStatus;
            truck.LastUpdate = DateTime.UtcNow;

            _trucks[truck.Id] = truck;

            Console.WriteLine(
                $" Truck {truck.Id}: ({truck.Latitude:F4}, {truck.Longitude:F4}) | " +
                $"Status: {newStatus} | Distance: {truck.DistanceToNearestNode:F2}km | " +
                $"Clients: {_clients.Count}");

            if (previousStatus != newStatus)
            {
                Console.WriteLine($"\n STATUS CHANGE: {truck.Id}");
                Console.WriteLine($" {previousStatus} → {newStatus}");
                Console.WriteLine($" Distance: {truck.DistanceToNearestNode:F2} km\n");

                if (_notificationPublisher != null)
                {
                    await _notificationPublisher.PublishStatusChangeAsync(
                        truck.Id,
                        newStatus,
                        truck.DistanceToNearestNode
                    );
                }

                truck.StatusChangedAt = DateTime.UtcNow;
            }

            await BroadcastAsync(truck);
        }

        private string DetermineStatus(Vehicle truck)
        {
            double closestDistance = double.MaxValue;
            string? closestNodeId = null;
            string? closestNodeName = null;

            foreach (var node in _nodes)
            {
                double distance = CalculateDistance(
                    truck.Latitude, truck.Longitude,
                    node.Latitude, node.Longitude
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNodeId = node.Id;
                    closestNodeName = node.Name;
                }
            }

            truck.DistanceToNearestNode = closestDistance;
            truck.NearestNodeId = closestNodeId;
            truck.NearestNodeName = closestNodeName;

            if (closestDistance <= 5) return "OK";
            if (closestDistance <= 8) return "ALERT";
            return "STOP";
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public async Task HandleClientAsync(WebSocket webSocket)
        {
            _clients.Add(webSocket);
            Console.WriteLine($" Dashboard connected. Total clients: {_clients.Count}");

            foreach (var truck in _trucks.Values)
            {
                var json = JsonSerializer.Serialize(truck);
                var bytes = Encoding.UTF8.GetBytes(json);

                try
                {
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" Error sending initial state: {ex.Message}");
                }
            }

            var buffer = new byte[1024];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" WebSocket error: {ex.Message}");
            }
            finally
            {
                _clients.Remove(webSocket);
                Console.WriteLine($" Dashboard disconnected. Remaining clients: {_clients.Count}");

                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed",
                        CancellationToken.None);
                }
            }
        }

        private async Task BroadcastAsync(Vehicle truck)
        {
            if (_clients.Count == 0)
                return;

            var json = JsonSerializer.Serialize(truck);
            var bytes = Encoding.UTF8.GetBytes(json);
            var clientsToRemove = new List<WebSocket>();

            foreach (var client in _clients.ToList())
            {
                if (client.State == WebSocketState.Open)
                {
                    try
                    {
                        await client.SendAsync(
                            new ArraySegment<byte>(bytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($" Failed to send to client: {ex.Message}");
                        clientsToRemove.Add(client);
                    }
                }
                else
                {
                    clientsToRemove.Add(client);
                }
            }

            foreach (var client in clientsToRemove)
            {
                _clients.Remove(client);
            }
        }

        public List<Vehicle> GetAllTrucks() => _trucks.Values.ToList();
        public List<LocationNode> GetAllNodes() => _nodes;
    }
}
