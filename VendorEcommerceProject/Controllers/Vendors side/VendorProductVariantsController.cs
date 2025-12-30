using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendorEcommerceProject.Dtos.Vendor.ProductVariants;
using VendorEcommerceProject.Models.ProductsTables;
using System.Security.Claims;

[ApiController]
[Route("api/vendor/product-variants")]
[Authorize(Roles = "Vendor")]
public class VendorProductVariantsController : ControllerBase
{
    private readonly AppDbContext _db;

    public VendorProductVariantsController(AppDbContext db)
    {
        _db = db;
    }

    // ----------------------------------------
    // GET: Variants of a product (vendor only)
    // ----------------------------------------
    [HttpGet("{productId:long}")]
    public async Task<IActionResult> GetByProduct(long productId)
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var product = await _db.Products
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.Vendor.UserId == userId);

        if (product == null)
            return NotFound("Product not found");

        var variants = await _db.ProductVariants
            .Where(v => v.ProductId == productId)
            .Select(v => new VendorProductVariantListDto
            {
                ProductVariantId = v.ProductVariantId,
                AttributeName = v.Attribute.Name,
                Value = v.Value,
                AdditionalPrice = v.AdditionalPrice,
                Quantity = v.Quantity
            })
            .ToListAsync();

        return Ok(variants);
    }

    // ----------------------------------------
    // POST: Create variant
    // ----------------------------------------
    [HttpPost]
    public async Task<IActionResult> Create(VendorProductVariantCreateDto dto)
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var product = await _db.Products
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p =>
                p.ProductId == dto.ProductId &&
                p.Vendor.UserId == userId);

        if (product == null)
            return BadRequest("Invalid product");

        var variant = new ProductVariant
        {
            ProductId = dto.ProductId,
            ProductAttributeId = dto.ProductAttributeId,
            Value = dto.Value,
            AdditionalPrice = dto.AdditionalPrice,
            Quantity = dto.Quantity,
            CreatedAt = DateTime.UtcNow
        };

        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync();

        return Ok("Variant added successfully");
    }

    // ----------------------------------------
    // PUT: Update variant
    // ----------------------------------------
    [HttpPut]
    public async Task<IActionResult> Update(VendorProductVariantUpdateDto dto)
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .ThenInclude(p => p.Vendor)
            .FirstOrDefaultAsync(v =>
                v.ProductVariantId == dto.ProductVariantId &&
                v.Product.Vendor.UserId == userId);

        if (variant == null)
            return NotFound("Variant not found");

        variant.Value = dto.Value;
        variant.AdditionalPrice = dto.AdditionalPrice;
        variant.Quantity = dto.Quantity;
        variant.ModifiedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok("Variant updated");
    }

    // ----------------------------------------
    // DELETE: Remove variant
    // ----------------------------------------
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .ThenInclude(p => p.Vendor)
            .FirstOrDefaultAsync(v =>
                v.ProductVariantId == id &&
                v.Product.Vendor.UserId == userId);

        if (variant == null)
            return NotFound();

        _db.ProductVariants.Remove(variant);
        await _db.SaveChangesAsync();

        return Ok("Variant deleted");
    }
}
