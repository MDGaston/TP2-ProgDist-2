using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace TrackingConsumer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Configuración de Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/trackingLog.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Configuración de la aplicación
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(dispose: true); // Agregar Serilog con dispose: true para liberar recursos correctamente
            });

            var logger = loggerFactory.CreateLogger<RabbitMqConsumer>();

            var rabbitMqConsumer = new RabbitMqConsumer(configuration, logger);

            Console.WriteLine("Tracking Consumer running. Press Ctrl+C to exit.");

            // Mantener la aplicación en ejecución
            while (true)
            {
                // Agregamos una pausa para no consumir recursos excesivamente
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}
