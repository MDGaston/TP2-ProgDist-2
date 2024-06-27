using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

public class RabbitMqConsumer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private readonly HttpClient _httpClient;
    private IConnection _connection;
    private IModel _channel;

    public RabbitMqConsumer(IConfiguration configuration, ILogger<RabbitMqConsumer> logger, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

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
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received message: {Message}", message);

                var logMessage = await CheckAndAnnotateMessage(message);
                LogToFile("/app/trackingLog/trackingLog.txt", logMessage);
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

    private async Task<string> CheckAndAnnotateMessage(string message)
    {
        string url = ExtractUrlFromMessage(message);
        if (!string.IsNullOrEmpty(url))
        {
            bool isBlacklisted = await CheckIfUrlIsBlacklisted(url);
            if (isBlacklisted)
            {
                return $"[BLACKLISTED URL CRITICAL] {message}";
            }
        }
        return message;
    }

    private string ExtractUrlFromMessage(string message)
    {
        try
        {
            var jsonDocument = JsonDocument.Parse(message);
            if (jsonDocument.RootElement.TryGetProperty("Url", out JsonElement urlElement))
            {
                return urlElement.GetString();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing message JSON");
        }
        return null;
    }

    private async Task<bool> CheckIfUrlIsBlacklisted(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync($"http://blacklistapi:8003/api/blacklist/check?url={url}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return bool.Parse(result);
            }
            _logger.LogError("Error checking URL against blacklist: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while checking URL against blacklist");
        }
        return false;
    }

    private void LogToFile(string filePath, string message)
    {
        try
        {
            Log.Logger.Information("{Timestamp} - {Message}", DateTime.Now, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to log file");
        }
    }
}
