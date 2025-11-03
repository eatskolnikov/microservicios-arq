namespace ProductService.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishProductCreatedAsync(Events.ProductCreatedEvent evt);
    Task PublishProductUpdatedAsync(Events.ProductUpdatedEvent evt);
    Task PublishProductDeletedAsync(Events.ProductDeletedEvent evt);
}

