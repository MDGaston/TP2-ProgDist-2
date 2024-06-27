using Serilog;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("/app/Logs/blacklist/blacklistLog.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Agregar lista de direcciones IP permitidas desde la configuración
var allowedIPAddresses = builder.Configuration.GetSection("AllowedIPAddresses").Get<List<string>>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

// Config Access Point

app.Use(async (context, next) =>
{
    var remoteIPAddress = context.Connection.RemoteIpAddress?.ToString();

    // Normalizar la dirección IP remota para IPV6
    if (remoteIPAddress.StartsWith("::ffff:"))
    {
        remoteIPAddress = remoteIPAddress.Replace("::ffff:", "");
    }

    // Verificar si la dirección IP remota está en la lista de direcciones IP permitidas
    if (!allowedIPAddresses.Contains(remoteIPAddress))
    {
        var errorMessage = $"Forbidden: Remote IP Address '{remoteIPAddress}' is not allowed.";
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync(errorMessage);
        return;
    }

    await next();
});

app.MapControllers();

try
{
    Log.Information("Starting web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
