using MediatR;

namespace Ordering.Domain.Abstractions
{
  public interface IDomainEvent : INotification
  {
    Guid EventID => Guid.NewGuid();
    public DateTime OccurredOn => DateTime.Now;
    public string EventType => GetType().AssemblyQualifiedName;
  }
}