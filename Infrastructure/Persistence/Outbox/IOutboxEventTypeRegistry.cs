namespace Infrastructure.Persistence.Outbox;

public interface IOutboxEventTypeRegistry
{
    string GetTypeName(Type type);

    Type? Resolve(string typeName);
}