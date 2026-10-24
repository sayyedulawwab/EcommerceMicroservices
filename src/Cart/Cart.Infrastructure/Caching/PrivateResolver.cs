using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SharedKernel.Domain;
using System.Collections;
using System.Reflection;

namespace Cart.Infrastructure.Caching;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility", Justification = "Rebuilds the cart from the cache through its private list fields")]
public class PrivateResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty prop = base.CreateProperty(member, memberSerialization);

        if (prop.Writable || member is not PropertyInfo property)
        {
            return prop;
        }

        if (property.GetSetMethod(true) is not null)
        {
            prop.Writable = true;

            return prop;
        }

        // A read-only collection property over a private list field (CartItems => _cartItems) is filled through the field.
        FieldInfo? field = property.DeclaringType?.GetField(
            "_" + char.ToLowerInvariant(property.Name[0]) + property.Name[1..],
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field is not null && typeof(IList).IsAssignableFrom(field.FieldType))
        {
            prop.ValueProvider = new ListFieldValueProvider(field);
            prop.Writable = true;
        }

        return prop;
    }

    protected override JsonConverter? ResolveContractConverter(Type objectType) =>
        objectType == typeof(Currency) ? new CurrencyJsonConverter() : base.ResolveContractConverter(objectType);

    private sealed class ListFieldValueProvider(FieldInfo field) : IValueProvider
    {
        public object? GetValue(object target) => field.GetValue(target);

        public void SetValue(object target, object? value)
        {
            var list = (IList)field.GetValue(target)!;

            list.Clear();

            if (value is IEnumerable items)
            {
                foreach (object? item in items)
                {
                    list.Add(item);
                }
            }
        }
    }
}
