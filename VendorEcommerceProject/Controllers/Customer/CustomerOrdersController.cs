using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VendorEcommerceProject.Dtos.Customer.Orders;
using VendorEcommerceProject.Helpers;
using VendorEcommerceProject.Models.OrdersAndCartTable;
using VendorEcommerceProject.Models.UserDetailsTable;

[ApiController]
[Route("api/customer/orders")]
[Authorize(Roles = "Customer")]
public class CustomerOrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomerOrdersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var orders = await _db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
            .Include(o => o.Status)
            .Where(o => o.UserId == userId)
            .ToListAsync();

        var result = orders.Select(o => new
        {
            OrderId = o.OrderId,
            CreatedAt = o.CreatedAt,
            Status = o.Status.Name,
            Items = o.OrderItems.Select(oi => new
            {
                ProductId = oi.ProductId,
                ProductName = oi.Product.ProductsName,
                Quantity = oi.Quantity,
                Price = oi.Product.Price,
                FirstImageUrl = oi.Product.ProductImages.OrderBy(pi => pi.ProductImageId).Select(pi => pi.ImageUrl).FirstOrDefault()
            }).ToList(),
            TotalAmount = o.OrderItems.Sum(oi => oi.Quantity * oi.Product.Price)
        });

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // 1. Get cart
        var cart = await _db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
            return BadRequest("Cart is empty".SendResponse());

        // 2. Save new shipping address
        if (dto.SetAsDefault)
        {
            var oldDefaults = await _db.UserAddresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();

            oldDefaults.ForEach(a => a.IsDefault = false);
        }

        var address = new UserAddress
        {
            UserId = userId,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            City = dto.City,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            IsDefault = dto.SetAsDefault
        };

        _db.UserAddresses.Add(address);
        await _db.SaveChangesAsync();

        // 3. Get Pending OrderStatusId
        var pendingStatusId = await _db.OrderStatuses
            .Where(s => s.Name == "Pending")
            .Select(s => s.OrderStatusId)
            .FirstOrDefaultAsync();

        if (pendingStatusId == 0)
            return BadRequest("Pending order status not found. Contact admin.".SendResponse());

        // 4. Create order
        var order = new Orders
        {
            UserId = userId,
            ShippingAddressId = address.UserAddressId,
            OrderStatusId = pendingStatusId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // 5. Add order items
        foreach (var item in cart.Items)
        {
            _db.OrderItems.Add(new OrderItem
            {
                OrderId = order.OrderId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Product.Price
            });
        }

        // 6. Clear cart
        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            orderId = order.OrderId,
            message = "Order placed successfully",
            shippingAddress = $"{address.AddressLine1}, {address.City}, {address.Country}"
        });
    }



}
