using Microsoft.EntityFrameworkCore;
using Order.API.Models;

namespace Order.API.Db;
public class OrderContext : DbContext
{
    public OrderContext(DbContextOptions<OrderContext> options) 
        : base(options)
    {
    }

    public DbSet<OrderItem> OrderItems { get; set; }
}