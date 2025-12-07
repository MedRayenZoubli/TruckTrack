using System.Text.Json;
using MQTTnet;

const string TRUCK_ID = "TRUCK-002";


double lat = 34.00;   
double lon = -118.25;
string currentStatus = "ALERT";

Console.WriteLine($"🚚 Starting {TRUCK_ID} simulator...");
Console.WriteLine($"⚠️  This truck will be in ALERT status (5-8km from nodes)\n");

var mqttFactory = new MqttClientFactory();
var mqttClient = mqttFactory.CreateMqttClient();

var mqttOptions = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .WithCleanSession()
    .Build();

mqttClient.ApplicationMessageReceivedAsync += async e =>
{
    if (e.ApplicationMessage.Topic == $"trucks/{TRUCK_ID}/status")
    {
        var payload = e.ApplicationMessage.ConvertPayloadToString();
        var statusUpdate = JsonSerializer.Deserialize<JsonElement>(payload);

        string newStatus = statusUpdate.GetProperty("status").GetString();
        double distance = statusUpdate.GetProperty("distance").GetDouble();

        currentStatus = newStatus;

        Console.WriteLine($"\n📡 STATUS UPDATE RECEIVED:");
        Console.WriteLine($"   Status: {newStatus}");
        Console.WriteLine($"   Distance: {distance:F2} km");

        if (newStatus == "ALERT")
        {
            Console.WriteLine($"   ⚠️  WARNING: Truck drifting! Return to delivery zone!");
        }
        else if (newStatus == "STOP")
        {
            Console.WriteLine($"   🛑 EMERGENCY: Truck must stop and return immediately!");
        }
        else if (newStatus == "OK")
        {
            Console.WriteLine($"   ✅ GOOD: Truck returned to safe zone!");
        }
        Console.WriteLine();
    }
};

try
{
    await mqttClient.ConnectAsync(mqttOptions);
    Console.WriteLine($"✅ Connected to MQTT broker");

    await mqttClient.SubscribeAsync($"trucks/{TRUCK_ID}/status");
    Console.WriteLine($"📡 Subscribed to: trucks/{TRUCK_ID}/status\n");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ MQTT Connection failed: {ex.Message}");
    return;
}

using (var httpClient = new HttpClient())
{
    while (true)
    {
       
        lat += (Random.Shared.NextDouble() - 0.5) * 0.003;  
        lon += (Random.Shared.NextDouble() - 0.5) * 0.003;


        lat = Math.Max(33.96, Math.Min(34.01, lat));

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
            await httpClient.PostAsync(
                "http://localhost:5295/api/vehicles/update",
                content);

            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ HTTP Error: {ex.Message}");
        }

        await Task.Delay(2000);
    }
}