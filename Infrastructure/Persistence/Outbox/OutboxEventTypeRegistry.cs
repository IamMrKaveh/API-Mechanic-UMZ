using Domain.Common.Abstractions;
using System.Reflection;

namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxEventTypeRegistry : IOutboxEventTypeRegistry
{
    private readonly Dictionary<string, Type> _byName;
    private readonly Dictionary<Type, string> _byType;

    public OutboxEventTypeRegistry()
    {
        _byName = new Dictionary<string, Type>(StringComparer.Ordinal);
        _byType = new Dictionary<Type, string>();

        var domainEventType = typeof(IDomainEvent);

        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic);

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                if (type.IsAbstract || type.IsInterface) continue;
                if (!domainEventType.IsAssignableFrom(type)) continue;

                var name = $"{type.FullName}, {type.Assembly.GetName().Name}";

                _byName[name] = type;
                _byType[type] = name;

                if (type.FullName is { } fullName)
                    _byName[fullName] = type;
            }
        }
    }

    public string GetTypeName(Type type)
    {
        if (_byType.TryGetValue(type, out var name))
            return name;

        return $"{type.FullName}, {type.Assembly.GetName().Name}";
    }

    public Type? Resolve(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        if (_byName.TryGetValue(typeName, out var type))
            return type;

        var commaIndex = typeName.IndexOf(',');
        if (commaIndex > 0)
        {
            var fullNameOnly = typeName[..commaIndex].Trim();
            if (_byName.TryGetValue(fullNameOnly, out type))
                return type;
        }

        return Type.GetType(typeName, throwOnError: false);
    }
}