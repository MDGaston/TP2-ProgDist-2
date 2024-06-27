using TrackingService.API.Services;
using Microsoft.EntityFrameworkCore;
using TrackingService.Context;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Crear una variable para la cadena de conexion
var connectionString = builder.Configuration.GetConnectionString("Connection");
//Registrar el servicio para la conexion
builder.Services.AddDbContext<AppDbContext>(
    options => options.UseSqlServer(connectionString)
);

var gatewayIPAddress = builder.Configuration["GatewayIPAddress"];

builder.Services.AddControllers();
builder.Services.AddHttpClient();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IMessageProducer, MessageProducer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

// Config AccesPoint
app.Use(async (context, next) =>
{
    var remoteIPAddress = context.Connection.RemoteIpAddress?.ToString();

    // Normalizar la dirección IP remota para IPV6
    if (remoteIPAddress.StartsWith("::ffff:"))
    {
        remoteIPAddress = remoteIPAddress.Replace("::ffff:", "");
    }

    if (remoteIPAddress != gatewayIPAddress)
    {
        var errorMessage = $"Forbidden: Remote IP Address '{remoteIPAddress}' does not match Gateway IP Address '{gatewayIPAddress}'.";
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync(errorMessage);
        return;
    }

    await next();
});

app.MapControllers();

app.Run();
