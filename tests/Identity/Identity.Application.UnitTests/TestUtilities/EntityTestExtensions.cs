using Identity.Domain.Users;
using SharedKernel.Domain;
using System.Runtime.CompilerServices;

namespace Identity.Application.UnitTests.TestUtilities;

internal static class EntityTestExtensions
{
    /// <summary>Sets the init-only <see cref="Entity{TEntityId}.Id"/>, which EF normally assigns on save.</summary>
    public static T WithId<T>(this T entity, long id)
        where T : Entity<long>
    {
        typeof(Entity<long>).GetProperty(nameof(Entity<long>.Id))!.SetValue(entity, id);

        return entity;
    }

    /// <summary>Populates the get-only <see cref="RefreshToken.User"/> navigation, which EF normally fills via Include.</summary>
    public static RefreshToken WithUser(this RefreshToken token, User user)
    {
        UserBackingField(token) = user;

        return token;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<User>k__BackingField")]
    private static extern ref User UserBackingField(RefreshToken token);
}
