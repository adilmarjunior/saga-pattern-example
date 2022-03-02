using Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Order.API.Db;
using Plain.RabbitMQ;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ordering.API;
public class CatalogResponseListener : IHostedService
{
    private readonly ISubscriber _subscriber;
    private readonly IServiceScopeFactory _scopeFactory;
    public CatalogResponseListener(ISubscriber subscriber, IServiceScopeFactory scopeFactory)
    {
        this._subscriber = subscriber;
        this._scopeFactory = scopeFactory;
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscriber.Subscribe(Subscribe);
        return Task.CompletedTask;
    }

    private bool Subscribe(string message, IDictionary<string, object> header)
    {
        var response = JsonConvert.DeserializeObject<CatalogResponse>(message) ?? throw new InvalidDataException("Invalid message");

        if (!response.IsSuccess)
        {
            using var scope = _scopeFactory.CreateScope();
            var _orderContext = scope.ServiceProvider.GetRequiredService<OrderContext>();

            // If transaction is not successful, Remove ordering item
            var orderItem = _orderContext.OrderItems.Where(o => o.ProductId == response.CatalogId && o.OrderId == response.OrderId).FirstOrDefault();
            
            if (orderItem is not null)
            {
                _orderContext.OrderItems.Remove(orderItem);
                _orderContext.SaveChanges();
            }
        }

        return true;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
