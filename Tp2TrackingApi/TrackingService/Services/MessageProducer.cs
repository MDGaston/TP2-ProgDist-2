using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TrackingService.API.Services;
using TrackingService.Controllers;

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

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // Configurar argumentos para Dead-Letter Queue
        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "" },  // Aquí debe ser el nombre de tu intercambio de Dead-Letter
            { "x-dead-letter-routing-key", "dead_letter_queue" }  // Nombre de la ruta para la Dead-Letter Queue
        };

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

        // Declarar la cola visit_url_queue con los argumentos de Dead-Letter
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);

        // Serializar el evento y publicarlo en la cola correspondiente
        var jsonString = JsonSerializer.Serialize(trackingEvent);
        var body = Encoding.UTF8.GetBytes(jsonString);

        channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
    }
}
