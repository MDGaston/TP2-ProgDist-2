using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TrackingService.Controllers;

namespace TrackingService.API.Services
{
    public class MessageProducer : IMessageProducer
    {
        public void SendingMessage(TrackingEvent trackingEvent)
        {
            var factory = new ConnectionFactory()
            {
                HostName = "rabbitmq",
                UserName = "user",
                Password = "password",
            };
            
            var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Determinar la cola en base al tipo de evento
            string queueName;
            if (trackingEvent.EventType == "click")
            {
                queueName = "click_queue";
            }
            else if (trackingEvent.EventType == "visit_url")
            {
                queueName = "visit_url_queue";
            }
            else
            {
                // Manejo de errores o eventos desconocidos, si es necesario
                throw new ArgumentException($"Unsupported event type: {trackingEvent.EventType}");
            }

            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var jsonString = JsonSerializer.Serialize(trackingEvent);
            var body = Encoding.UTF8.GetBytes(jsonString);

            channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
        }
    }
}
