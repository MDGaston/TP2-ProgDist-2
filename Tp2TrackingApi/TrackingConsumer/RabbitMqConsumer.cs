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
using System.Net.Sockets;

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
            HostName = _configuration["RabbitMq:HostName"],
            UserName = _configuration["RabbitMq:UserName"],
            Password = _configuration["RabbitMq:Password"],
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            ConfigureDeadLetterQueue(_channel);

            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "" },
                { "x-dead-letter-routing-key", "dead_letter_queue" }
            };

            _channel.QueueDeclare(queue: "visit_url_queue",
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: args);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received message: {Message}", message);

                try
                {
                    var logMessage = await CheckAndAnnotateMessage(message);
                    LogToFile("/app/trackingLog/trackingLog.txt", logMessage);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message, sending to DLQ");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(queue: "visit_url_queue",
                                  autoAck: false,
                                  consumer: consumer);

            _logger.LogInformation("Consumer started.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting RabbitMqConsumer.");
            throw;
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
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            _logger.LogWarning("Error connecting to blacklist service");
            throw; // Re-lanza la excepción para enviar el mensaje a la DLQ
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Error checking URL against blacklist");
            throw; // Re-lanza la excepción para enviar el mensaje a la DLQ
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while checking URL against blacklist");
            throw; // Re-lanza la excepción para enviar el mensaje a la DLQ
        }
        return false;
    }

    private void ConfigureDeadLetterQueue(IModel channel)
    {
        channel.QueueDeclare(queue: "dead_letter_queue",
                             durable: true,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
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
