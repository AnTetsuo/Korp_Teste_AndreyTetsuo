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
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(description))
            errors.Add(new ValidationError(nameof(description), "Description is required."));
        else if (description.Trim().Length > DescriptionMaxLength)
            errors.Add(new ValidationError(nameof(description),
                $"Description must be at most {DescriptionMaxLength} characters."));

        if (string.IsNullOrWhiteSpace(productCode))
            errors.Add(new ValidationError(nameof(productCode), "Product code is required."));
        else if (productCode.Trim().Length > ProductCodeMaxLength)
            errors.Add(new ValidationError(nameof(productCode),
                $"Product code must be at most {ProductCodeMaxLength} characters."));

        if (errors.Count > 0)
            return Result<Product>.Invalid([.. errors]);

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
