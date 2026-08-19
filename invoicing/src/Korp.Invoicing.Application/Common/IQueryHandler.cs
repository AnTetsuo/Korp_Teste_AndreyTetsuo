namespace Application.Common;

public interface IQueryHandler<in TQuery, TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
