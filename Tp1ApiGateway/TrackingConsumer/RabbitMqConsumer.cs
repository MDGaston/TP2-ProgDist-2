using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

public class RabbitMqConsumer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection _connection;
    private IModel _channel;

    public RabbitMqConsumer(IConfiguration configuration, ILogger<RabbitMqConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var factory = new ConnectionFactory()
        {
            HostName = _configuration["RabbitMq:HostName"], // Obtén el nombre de host de la configuración
            UserName = _configuration["RabbitMq:UserName"], // Obtén el nombre de usuario de la configuración
            Password = _configuration["RabbitMq:Password"], // Obtén la contraseña de la configuración
        };

        // Intentamos conectar y consumir mensajes
        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "visit_url_queue",
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received message: {Message}", message);

                // Log to file
                LogToFile("logs/trackingLog.txt", message);
            };

            _channel.BasicConsume(queue: "visit_url_queue",
                                  autoAck: true,
                                  consumer: consumer);

            _logger.LogInformation("Consumer started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting RabbitMqConsumer.");
            throw; // Re-lanza la excepción para que la aplicación no continúe sin conexión
        }
    }

    private void LogToFile(string filePath, string message)
    {
        try
        {
            // Utilizamos Serilog para escribir en el archivo de log
            Log.Logger.Information("{Timestamp} - {Message}", DateTime.Now, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to log file");
        }
    }
}
