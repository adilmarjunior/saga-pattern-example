using Catalog.API.Db;
using Catalog.API.Models;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Plain.RabbitMQ;

namespace Catalog.API.Listeners;

public class OrderCreatedListener : IHostedService
{
    private readonly ISubscriber _subscribe;
    private readonly IPublisher _publisher;
    private readonly IServiceScopeFactory _scopeFactory;
    public OrderCreatedListener(ISubscriber subscriber, IPublisher publisher, IServiceScopeFactory scopeFactory)
    {
        _subscribe = subscriber;
        _publisher = publisher;
        _scopeFactory = scopeFactory;
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscribe.Subscribe(Subscribe);
        return Task.CompletedTask;
    }

    private bool Subscribe(string message, IDictionary<string, object> header)
    {
        var response = JsonConvert.DeserializeObject<OrderRequest>(message) ?? throw new ArgumentNullException(nameof(message));

        using (var scope = _scopeFactory.CreateScope())
        {
            var _context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
            try
            {
                CatalogItem catalogItem = _context.CatalogItems.Find(response.CatalogId) ?? throw new InvalidDataException(nameof(catalogItem));

                if (catalogItem == null || catalogItem.AvailableStock < response.Units) throw new Exception();

                catalogItem.AvailableStock -= response.Units;

                _context.Entry(catalogItem).State = EntityState.Modified;
                _context.SaveChanges();

                _publisher.Publish(JsonConvert.SerializeObject(
                        new CatalogResponse { OrderId = response.OrderId, CatalogId = response.CatalogId, IsSuccess = true }
                    ), "catalog_response_routingkey", null);
            }
            catch (Exception)
            {
                _publisher.Publish(JsonConvert.SerializeObject(
                new CatalogResponse { OrderId = response.OrderId, CatalogId = response.CatalogId, IsSuccess = false }
                ), "catalog_response_routingkey", null);
            }
        }

        return true;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}