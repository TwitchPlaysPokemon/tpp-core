using System;

namespace TPP.Common;

/// <summary>
/// Convenience base class that implements <see cref="IEquatable{T}"/>
/// and overrides Equals, GetHashCode, operator == and operator !=,
/// and forwards all the equality checks to <see cref="EqualityId"/>.
/// </summary>
/// <typeparam name="T">type of self</typeparam>
public abstract class PropertyEquatable<T> : IEquatable<T> where T : PropertyEquatable<T>
{
    protected abstract object EqualityId { get; }

    public bool Equals(T? other)
        => !ReferenceEquals(other, null) && EqualityId.Equals(other.EqualityId);

    public static bool operator ==(PropertyEquatable<T>? a, PropertyEquatable<T>? b)
        => ReferenceEquals(a, b) || !ReferenceEquals(a, null) && a.EqualityId.Equals(b?.EqualityId);

    public static bool operator !=(PropertyEquatable<T>? a, PropertyEquatable<T>? b)
        => !(a == b);

    public override bool Equals(object? obj)
        => obj as T == this;

    public override int GetHashCode()
        => EqualityId.GetHashCode();
}
