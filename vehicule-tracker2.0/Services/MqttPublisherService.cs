using System.Buffers;
using System.Text;
using System.Text.Json;
using MQTTnet;
using vehicule_tracker2._0.Models;

namespace vehicule_tracker2._0.Services
{
    public class MqttPublisherService : INotificationPublisher
    {
        private IMqttClient? _mqttClient;
        private const string BrokerAddress = "localhost";
        private const int BrokerPort = 1883;

        public MqttPublisherService()
        {
        }

        public async Task ConnectAsync()
        {
            var mqttFactory = new MqttClientFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(BrokerAddress, BrokerPort)
                .WithCleanSession()
                .Build();

            try
            {
                _mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;
                await _mqttClient.ConnectAsync(options, CancellationToken.None);
                Console.WriteLine(" Connected to MQTT Broker");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" MQTT Connection failed: {ex.Message}");
            }
        }

        public async Task SubscribeToTruckUpdatesAsync()
        {
            if (_mqttClient?.IsConnected != true)
            {
                Console.WriteLine(" MQTT client not connected");
                return;
            }

            try
            {
                await _mqttClient.SubscribeAsync("trucks/+/status");
                Console.WriteLine(" Subscribed to truck updates on topic: trucks/+/status");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error subscribing: {ex.Message}");
            }
        }

        
        private Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                string payload = string.Empty;

                if (e.ApplicationMessage.Payload.Length > 0)
                {
                    payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());
                }

                Console.WriteLine($" Received from {topic}: {payload}");

                if (string.IsNullOrEmpty(payload))
                {
                    Console.WriteLine(" Empty payload received");
                    return Task.CompletedTask;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error handling message: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public async Task PublishStatusChangeAsync(string truckId, string status, double distance)
        {
            if (_mqttClient?.IsConnected != true)
            {
                Console.WriteLine(" MQTT not connected, skipping publish");
                return;
            }

            var statusUpdate = new
            {
                truckId,
                status,
                distance,
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(statusUpdate);
            var topic = $"trucks/{truckId}/status";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(json)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            try
            {
                await _mqttClient.PublishAsync(message);
                Console.WriteLine($" Published: {topic} → {status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Publish failed: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_mqttClient?.IsConnected == true)
            {
                await _mqttClient.DisconnectAsync();
                Console.WriteLine("Disconnected from MQTT");
            }
        }
    }
}
