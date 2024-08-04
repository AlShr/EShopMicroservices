﻿namespace Catalog.API.Products.GetProductById
{
  public record GetProductByIdQuery(Guid Id) : IQuery<GetProductByIdResult>;

  public record GetProductByIdResult(Product Product);

  internal class GrtProductByIdQueryHandler(IDocumentSession session, ILogger<GrtProductByIdQueryHandler> logger): IQueryHandler<GetProductByIdQuery, GetProductByIdResult>
  {
    public async Task<GetProductByIdResult> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
      logger.LogInformation($"GrtProductByIdQueryHandler.Handler called with {query}", query);

      var product = await session.LoadAsync<Product>(query.Id, cancellationToken);
      if (product is null)
      {
        throw new ProductNotFoundException();
      }

      return new GetProductByIdResult(product);
    }
  }
}
