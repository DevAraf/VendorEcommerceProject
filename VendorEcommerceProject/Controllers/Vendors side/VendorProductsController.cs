using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendorEcommerceProject.Dtos.Vendor.Products;
using VendorEcommerceProject.Models.ProductsTables;
using VendorEcommerceProject.Models.VendorsTable;
using System.Security.Claims;

[ApiController]
[Route("api/vendor/products")]
[Authorize(Roles = "Vendor")]
public class VendorProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public VendorProductsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // --------------------------------------------------
    // GET: Vendor own products
    // --------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> GetMyProducts()
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var vendorId = await _db.Vendors
            .Where(v => v.UserId == userId)
            .Select(v => v.VendorId)
            .FirstOrDefaultAsync();

        var products = await _db.Products
            .Where(p => p.VendorId == vendorId && p.DeletedAt == null)
            .Select(p => new VendorProductListDto
            {
                ProductId = p.ProductId,
                ProductsName = p.ProductsName,
                Price = p.Price,
                Quantity = p.Quantity,
                StatusName = p.Status.Name,
                CreatedAt = p.CreatedAt,
                FirstImageUrl = p.ProductImages
                    .OrderBy(i => i.ProductImageId)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(products);
    }

    // --------------------------------------------------
    // POST: Create product
    // --------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] VendorProductCreateDto dto)
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.UserId == userId);
        if (vendor == null) return BadRequest("Vendor account not found");

        var pendingStatusId = await _db.ProductStatuses
            .Where(s => s.Name == "Pending")
            .Select(s => s.ProductStatusId)
            .FirstAsync();

        var product = new Products
        {
            VendorId = vendor.VendorId,
            CategoryId = dto.CategoryId,
            ProductsName = dto.ProductsName,
            Description = dto.Description,
            Price = dto.Price,
            Quantity = dto.Quantity,
            Sku = dto.Sku,
            ProductStatusId = pendingStatusId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        await SaveImages(product.ProductId, dto.Images);

        return Ok("Product created and sent for admin approval");
    }

    // --------------------------------------------------
    // PUT: Update product (own only)
    // --------------------------------------------------
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromForm] VendorProductUpdateDto dto)
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var product = await _db.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.ProductId == id && p.Vendor.UserId == userId);

        if (product == null) return NotFound("Product not found");

        product.ProductsName = dto.ProductsName;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Quantity = dto.Quantity;
        product.Sku = dto.Sku;
        product.CategoryId = dto.CategoryId;
        product.ModifiedAt = DateTime.UtcNow;

        // Back to pending after edit
        product.ProductStatusId = await _db.ProductStatuses
            .Where(s => s.Name == "Pending")
            .Select(s => s.ProductStatusId)
            .FirstAsync();

        // delete images
        if (dto.ImageIdsToDelete != null)
        {
            var images = product.ProductImages
                .Where(i => dto.ImageIdsToDelete.Contains(i.ProductImageId))
                .ToList();

            _db.ProductImages.RemoveRange(images);
        }

        await SaveImages(product.ProductId, dto.ImagesToAdd);
        await _db.SaveChangesAsync();

        return Ok("Product updated and re-sent for approval");
    }

    // --------------------------------------------------
    // DELETE: Soft delete product
    // --------------------------------------------------
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> SoftDelete(long id)
    {
        long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var product = await _db.Products
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.ProductId == id && p.Vendor.UserId == userId);

        if (product == null) return NotFound();

        product.DeletedAt = DateTime.UtcNow;
        product.DeletedBy = userId.ToString();

        await _db.SaveChangesAsync();
        return Ok("Product removed from vendor listing");
    }

    // --------------------------------------------------
    // IMAGE SAVE HELPER
    // --------------------------------------------------
    private async Task SaveImages(long productId, IList<IFormFile>? images)
    {
        if (images == null || images.Count == 0) return;

        var uploadPath = Path.Combine(_env.WebRootPath, "images", "products");
        Directory.CreateDirectory(uploadPath);

        foreach (var file in images)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadPath, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            _db.ProductImages.Add(new ProductsImages
            {
                ProductId = productId,
                ImageUrl = $"/images/products/{fileName}",
                FileName = file.FileName,
                ImageFileType = file.ContentType,
                FileSize = file.Length
            });
        }

        await _db.SaveChangesAsync();
    }
}
