using System.Net.WebSockets;
using vehicule_tracker2._0.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<INotificationPublisher, MqttPublisherService>();
builder.Services.AddSingleton<DeliveryManager>();
builder.Services.AddSingleton<MqttPublisherService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var deliveryManager = context.RequestServices.GetRequiredService<DeliveryManager>();
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await deliveryManager.HandleClientAsync(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapControllers();

var mqttPublisher = app.Services.GetRequiredService<MqttPublisherService>();
await mqttPublisher.ConnectAsync();
await mqttPublisher.SubscribeToTruckUpdatesAsync();

Console.WriteLine("Server ready");
Console.WriteLine("Dashboard: http://localhost:5295");
Console.WriteLine("WebSocket: ws://localhost:5295/ws");
Console.WriteLine("MQTT Broker: localhost:1883");

app.Run();


//GET http://localhost:5295/api/vehicles
//GET http://localhost:5295/api/vehicles/{id}
//GET http://localhost:5295/api/nodes
//GET http://localhost:5295/api/vehicules/status
//POST http://localhost:5295/api/vehicles/update