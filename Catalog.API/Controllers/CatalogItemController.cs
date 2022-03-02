using Catalog.API.Db;
using Catalog.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CatalogItemController : ControllerBase
{
    private readonly CatalogContext _context;
    public CatalogItemController(CatalogContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCatalogItems([FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 0)
    {
        var totalItems = await _context.CatalogItems.LongCountAsync();
        var itemsOnPage = await _context.CatalogItems
            .OrderBy(c => c.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        var model = new { totalItems, itemsOnPage };

        return Ok(model);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCatalogItem(int id)
    {
        var item = await _context.CatalogItems.SingleOrDefaultAsync(ci => ci.Id == id);
        if (item != null)
        {
            return Ok(item);
        }

        return NotFound();
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateCatalogItem([FromBody] CatalogItem item)
    {
        var newItem = new CatalogItem
        {
            Name = item.Name,
            Price = item.Price,
            AvailableStock = item.AvailableStock,
            MaxStockThreshold = item.MaxStockThreshold,
            Description = item.Description
        };

        _context.CatalogItems.Add(newItem);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetCatalogItem), new { id = newItem.Id }, newItem);
    }
}