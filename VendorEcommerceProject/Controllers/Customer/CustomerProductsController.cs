using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendorEcommerceProject.Dtos.Customer.Products;
using VendorEcommerceProject.Models.ProductsTables;

[ApiController]
[Route("api/customer/products")]
public class CustomerProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomerProductsController(AppDbContext db)
    {
        _db = db;
    }

    // ==========================
    // GET: Product List with filters
    // ==========================
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] long? categoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20
    )
    {
        var query = _db.Products
            .Include(p => p.Vendor)
            .Include(p => p.ProductImages)
            .Include(p => p.Status)
            .Where(p => p.Status.Name == "Approved")
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.ProductsName.Contains(search));

        // Category filter
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        // Price filter
        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        // Total count for frontend pagination
        var totalItems = await query.CountAsync();

        // Pagination
        var products = await query
            .OrderBy(p => p.ProductId) // or any sorting logic
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new CustomerProductListDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductsName,
                Price = p.Price,
                VendorId = p.VendorId,
                VendorName = p.Vendor.Name,
                InStock = p.Quantity > 0,
                ThumbnailImageUrl = p.ProductImages
                    .OrderBy(i => i.ProductImageId)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new
        {
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            Products = products
        });
    }


    // ==========================
    // GET: Product Details
    // ==========================
    //[HttpGet("{id:long}")]
    //public async Task<IActionResult> GetProduct(long id)
    //{
    //    var product = await _db.Products
    //        .Include(p => p.Vendor)
    //        .Include(p => p.ProductImages)
    //        .Include(p => p.Status)
    //        .Where(p => p.ProductId == id && p.Status.Name == "Approved")
    //        .Select(p => new CustomerProductDetailsDto
    //        {
    //            ProductId = p.ProductId,
    //            ProductName = p.ProductsName,
    //            Description = p.Description,
    //            Price = p.Price,
    //            VendorId = p.VendorId,
    //            VendorName = p.Vendor.Name,
    //            InStock = p.Quantity > 0,
    //            Images = p.ProductImages
    //                .OrderBy(i => i.ProductImageId)
    //                .Select(i => i.ImageUrl)
    //                .ToList()
    //        })
    //        .FirstOrDefaultAsync();

    //    if (product == null)
    //        return NotFound("Product not found or not approved");

    //    return Ok(product);
    //}


    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetProduct(long id)
    {
        // ==========================
        // Get main product
        // ==========================
        var product = await _db.Products
            .Include(p => p.Vendor)
            .Include(p => p.ProductImages)
            .Include(p=>p.Variants)
            .Include(p => p.Status)
            .Where(p => p.ProductId == id && p.Status.Name == "Approved")
            .Select(p => new
            {
                ProductId = p.ProductId,
                ProductName = p.ProductsName,
                Description = p.Description,
                Price = p.Price,
                VendorId = p.VendorId,
                VendorName = p.Vendor.Name,
                InStock = p.Quantity > 0,
                CategoryId = p.CategoryId,
                Images = p.ProductImages
                    .OrderBy(i => i.ProductImageId)
                    .Select(i => i.ImageUrl)
                    .ToList(),
                Variants = p.Variants.Select(v=>new ProductVariantDto
                {
                    VariantId=v.ProductVariantId,
                    Size =v.Value,
                    Color=v.Value,
                    Stock=v.Quantity
                }).ToList()

              

            })
            .FirstOrDefaultAsync();

        if (product == null)
            return NotFound("Product not found or not approved");

        // ==========================
        // Get related products (same category)
        // ==========================
        var relatedProducts = await _db.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Status)
            .Where(p =>
                p.CategoryId == product.CategoryId &&
                p.ProductId != product.ProductId &&
                p.Status.Name == "Approved"
            )
            .OrderByDescending(p => p.ProductId)
            .Take(8)
            .Select(p => new CustomerProductListDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductsName,
                Price = p.Price,
                VendorId = p.VendorId,
                InStock = p.Quantity > 0,
                ThumbnailImageUrl = p.ProductImages
                    .OrderBy(i => i.ProductImageId)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // ==========================
        // Final response
        // ==========================
        return Ok(new
        {
            Product = new CustomerProductDetailsDto
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                VendorId = product.VendorId,
                VendorName = product.VendorName,
                InStock = product.InStock,
                Images = product.Images,
                Variants=product.Variants
            },
            RelatedProducts = relatedProducts
        });
    }

}
