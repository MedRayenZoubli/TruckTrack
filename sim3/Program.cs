using System.Text.Json;
using MQTTnet;

const string TRUCK_ID = "TRUCK-003";

double lat = 33.985;
double lon = -118.25;
string currentStatus = "ALERT";
bool hasStoppedMoving = false;

Console.WriteLine($"Starting {TRUCK_ID} simulator...");
Console.WriteLine($"Truck will drift until STOP status is received.\n");

var mqttFactory = new MqttClientFactory();
var mqttClient = mqttFactory.CreateMqttClient();

var mqttOptions = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .WithCleanSession()
    .Build();


mqttClient.ApplicationMessageReceivedAsync += async e =>
{
    try
    {
        if (e.ApplicationMessage.Topic == $"trucks/{TRUCK_ID}/status")
        {
            var payload = e.ApplicationMessage.ConvertPayloadToString();
            var statusUpdate = JsonSerializer.Deserialize<JsonElement>(payload);

            string newStatus = statusUpdate.GetProperty("status").GetString();
            double distance = statusUpdate.GetProperty("distance").GetDouble();

            string oldStatus = currentStatus;
            currentStatus = newStatus;

            Console.WriteLine($"\n--- STATUS UPDATE ---");
            Console.WriteLine($"   {oldStatus} → {newStatus}");
            Console.WriteLine($"   Distance: {distance:F2} km\n");

            if (newStatus == "STOP")
            {
                hasStoppedMoving = true;
                Console.WriteLine("🛑 TRUCK HAS STOPPED — MOVEMENT DISABLED\n");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"MQTT message error: {ex.Message}");
    }
};


try
{
    await mqttClient.ConnectAsync(mqttOptions);
    Console.WriteLine("Connected to MQTT broker");

    await mqttClient.SubscribeAsync($"trucks/{TRUCK_ID}/status");
    Console.WriteLine($"Subscribed to trucks/{TRUCK_ID}/status\n");
}
catch (Exception ex)
{
    Console.WriteLine($"MQTT Connection failed: {ex.Message}");
    return;
}

using (var httpClient = new HttpClient())
{
    int iteration = 0;

    while (true)
    {
        iteration++;

        if (!hasStoppedMoving)
        {
            if (iteration <= 10)
            {
                lat += (Random.Shared.NextDouble() - 0.5) * 0.002;
                lon += (Random.Shared.NextDouble() - 0.5) * 0.002;
            }
            else
            {
                lat -= 0.004;
                lon += (Random.Shared.NextDouble() - 0.5) * 0.001;
            }
        }

        var truck = new
        {
            id = TRUCK_ID,
            latitude = Math.Round(lat, 6),
            longitude = Math.Round(lon, 6)
        };

        var json = JsonSerializer.Serialize(truck);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            await httpClient.PostAsync("http://localhost:5295/api/vehicles/update", content);

            string statusEmoji = currentStatus switch
            {
                "OK" => "✅",
                "ALERT" => "⚠️",
                "STOP" => "🛑",
                _ => "❓"
            };

            if (hasStoppedMoving)
            {
                Console.WriteLine($"{statusEmoji} {TRUCK_ID}: ({truck.latitude}, {truck.longitude}) | Status: STOP | MOVEMENT FROZEN");
            }
            else if (currentStatus == "ALERT" && iteration > 10)
            {
                Console.WriteLine($"{statusEmoji} {TRUCK_ID}: ({truck.latitude}, {truck.longitude}) | Status: ALERT | DRIFTING...");
            }
            else
            {
                Console.WriteLine($"{statusEmoji} {TRUCK_ID}: ({truck.latitude}, {truck.longitude}) | Status: {currentStatus}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HTTP Error: {ex.Message}");
        }

        await Task.Delay(2000);
    }
}
