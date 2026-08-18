using Application.Products.ListProducts;
using Application.Products.ListProducts.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Queries;

internal sealed class ProductReadRepository(StockDbContext context) : IProductReadRepository
{
    private const string SelectClause =
        """
        SELECT p.description,
               p.product_code,
               p.created_at,
               p.updated_at,
               COALESCE(s.quantity, 0) AS quantity,
               COUNT(*) OVER () AS total_count
        FROM products p
        LEFT JOIN stocks s ON s.product_id = p.id
        """;

    public async Task<ListProductsResponse> ListAsync(
        ListProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<object>();
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var index = parameters.Count;
            conditions.Add($"(p.description ILIKE {{{index}}} OR p.product_code ILIKE {{{index}}})");
            parameters.Add($"%{query.SearchTerm.Trim()}%");
        }

        if (query.Active is { } active)
        {
            conditions.Add($"p.active = {{{parameters.Count}}}");
            parameters.Add(active);
        }

        var page = query.Page ?? 1;

        var sql = new List<string> { SelectClause };

        if (conditions.Count > 0)
            sql.Add($"WHERE {string.Join(" AND ", conditions)}");

        sql.Add($"ORDER BY {OrderByClause(query.OrderBy, query.Asc ?? true)}");
        sql.Add($"LIMIT {{{parameters.Count}}}");
        parameters.Add(query.Rows);
        sql.Add($"OFFSET {{{parameters.Count}}}");
        parameters.Add((page - 1) * query.Rows);

        var rows = await context.Database
            .SqlQueryRaw<ProductListRow>(string.Join("\n", sql), [.. parameters])
            .ToListAsync(cancellationToken);

        var products = rows
            .Select(row => new UnitOfProduct(
                row.Description,
                row.ProductCode,
                row.CreatedAt,
                row.UpdatedAt,
                row.Quantity))
            .ToList();

        var totalCount = rows.Count > 0 ? (int)rows[0].TotalCount : 0;

        return new ListProductsResponse(products, page, query.Rows, totalCount);
    }

    private static string OrderByClause(OrderByOptions? orderBy, bool ascending)
    {
        var column = orderBy switch
        {
            OrderByOptions.Description => "p.description",
            OrderByOptions.ProductCode => "p.product_code",
            OrderByOptions.UpdatedAt => "p.updated_at",
            _ => "p.created_at"
        };

        return $"{column} {(ascending ? "ASC" : "DESC")}, p.id ASC";
    }
}

internal sealed class ProductListRow
{
    public string Description { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Quantity { get; set; }
    public long TotalCount { get; set; }
}
