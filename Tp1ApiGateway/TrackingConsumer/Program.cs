using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                .WriteTo.File("/app/trackingLog/trackingLog.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Configuración de la aplicación
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, configuration);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<RabbitMqConsumer>>();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var rabbitMqConsumer = new RabbitMqConsumer(configuration, logger, httpClientFactory);

            Console.WriteLine("Tracking Consumer running. Press Ctrl+C to exit.");

            // Mantener la aplicación en ejecución
            while (true)
            {
                // Agregamos una pausa para no consumir recursos excesivamente
                System.Threading.Thread.Sleep(1000);
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient();
            services.AddLogging(configure => configure.AddSerilog())
                    .AddTransient<RabbitMqConsumer>();
        }
    }
}
