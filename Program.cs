using AeroCloud.PPS.Data;
using AeroCloud.PPS.Filters;
using AeroCloud.PPS.Messaging;
using AeroCloud.PPS.Middleware;
using AeroCloud.PPS.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<IPassengerService, PassengerService>();
builder.Services.AddScoped<IBagDropService, BagDropService>();
builder.Services.AddScoped<IFlightService, FlightService>();

builder.Services.AddScoped<ValidateBookingReferenceFilter>();

var serviceBusConnectionString = builder.Configuration["Azure:ServiceBus:ConnectionString"];
var isServiceBusConfigured = !string.IsNullOrWhiteSpace(serviceBusConnectionString)
    && serviceBusConnectionString != "YOUR_SERVICE_BUS_CONNECTION_STRING";

if (isServiceBusConfigured)
    builder.Services.AddSingleton<IBoardingEventPublisher, ServiceBusBoardingEventPublisher>();
else
    builder.Services.AddSingleton<IBoardingEventPublisher, NullBoardingEventPublisher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "AeroCloud Passenger Processing System (PPS)",
        Version = "v1",
        Description = "Mock PPS API — C# / .NET 10, SQL Server, Azure Service Bus."
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
        logger.LogInformation("Database migration applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed. Check your connection string.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AeroCloud PPS v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseRequestLogging();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();    
}
app.MapControllers();
app.Run();

public partial class Program { }
