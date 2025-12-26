using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VendorEcommerceProject.Models.OrdersAndCartTable;

[ApiController]
[Route("api/customer/cart")]
[Authorize(Roles = "Customer")]
public class CustomerCartController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomerCartController(AppDbContext db)
    {
        _db = db;
    }

    // GET cart
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var cart = await _db.Carts
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.Vendor)
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null) return Ok(new { CartId = 0, Vendors = new List<object>() });

        var vendors = cart.Items
            .GroupBy(ci => ci.Product.Vendor)
            .Select(g => new
            {
                VendorId = g.Key.VendorId,
                VendorName = g.Key.Name,
                Items = g.Select(ci => new
                {
                    CartItemId = ci.CartItemId,
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.ProductsName,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.Price,
                    TotalPrice = ci.Quantity * ci.Product.Price,
                    FirstImageUrl = ci.Product.ProductImages.OrderBy(pi => pi.ProductImageId).Select(pi => pi.ImageUrl).FirstOrDefault()
                }).ToList(),
                SubTotal = g.Sum(ci => ci.Quantity * ci.Product.Price)
            }).ToList();

        return Ok(new
        {
            CartId = cart.CartId,
            Vendors = vendors
        });
    }

    // POST add product to cart
    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddCartItemRequest request)
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null)
        {
            cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        var cartItem = await _db.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == request.ProductId);

        if (cartItem != null)
        {
            cartItem.Quantity += request.Quantity;
        }
        else
        {
            _db.CartItems.Add(new CartItem
            {
                CartId = cart.CartId,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            });
        }

        await _db.SaveChangesAsync();
        return Ok("Product added to cart");
    }

    // PUT update cart item quantity
    [HttpPut("{cartItemId}")]
    public async Task<IActionResult> UpdateCartItem(long cartItemId, [FromBody] UpdateCartItemRequest request)
    {
        var cartItem = await _db.CartItems.FindAsync(cartItemId);
        if (cartItem == null) return NotFound();

        cartItem.Quantity = request.Quantity;
        await _db.SaveChangesAsync();
        return Ok("Cart item updated");
    }

    // DELETE remove product from cart
    [HttpDelete("{cartItemId}")]
    public async Task<IActionResult> RemoveCartItem(long cartItemId)
    {
        var cartItem = await _db.CartItems.FindAsync(cartItemId);
        if (cartItem == null) return NotFound();

        _db.CartItems.Remove(cartItem);
        await _db.SaveChangesAsync();
        return Ok("Product removed from cart");
    }
}

// Request DTOs
public class AddCartItemRequest
{
    public long ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}
