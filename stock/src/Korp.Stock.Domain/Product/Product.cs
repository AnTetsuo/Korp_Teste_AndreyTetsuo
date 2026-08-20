using Domain.Common;

namespace Domain.Product;

public class Product
{
    public const int ProductCodeMaxLength = 16;
    public const int DescriptionMaxLength = 255;

    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static Result<Product> Create(string description, string productCode)
    {
        var errors = new ValidationErrors()
            .RequireText(description, nameof(description), "Description", DescriptionMaxLength)
            .RequireText(productCode, nameof(productCode), "Product code", ProductCodeMaxLength);

        if (errors.Any)
            return Result<Product>.Invalid(errors.ToArray());

        var now = DateTime.UtcNow;

        return new Product
        {
            Id = Guid.CreateVersion7(),
            Description = description.Trim(),
            ProductCode = productCode.Trim(),
            Active = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
