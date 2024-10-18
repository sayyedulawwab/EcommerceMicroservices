using Catalog.Domain.Abstractions;

namespace Catalog.Domain.Products.Events;
public sealed record ProductCreatedDomainEvent(long productId) : IDomainEvent;
