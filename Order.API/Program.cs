using Microsoft.EntityFrameworkCore;
using Order.API.Db;
using Ordering.API;
using Plain.RabbitMQ;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.AddDbContext<OrderContext>(options => options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

services.AddSingleton<IConnectionProvider>(new ConnectionProvider("amqp://guest:guest@localhost:5672"));

services.AddSingleton<IPublisher>(_ => new Publisher(_.GetService<IConnectionProvider>(),
               "order_exchange", // exchange name
               ExchangeType.Topic));

services.AddSingleton<ISubscriber>(s => new Subscriber(s.GetService<IConnectionProvider>(),
                "catalog_exchange", // Exchange name
                "catalog_response_queue", //queue name
                "catalog_response_routingkey", // routing key
                ExchangeType.Topic));

services.AddHostedService<CatalogResponseListener>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
