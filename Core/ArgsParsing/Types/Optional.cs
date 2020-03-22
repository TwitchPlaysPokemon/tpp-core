using System;

namespace Core.ArgsParsing.Types
{
    public class Optional
    {
    }

    public class Optional<T> : Optional
    {
        public bool IsPresent { get; }

        private readonly T _value;
        public T Value
        {
            get
            {
                if (!IsPresent) throw new InvalidOperationException("Cannot access the value of an empty Optional.");
                return _value;
            }
        }

        public Optional(bool present, T value)
        {
            IsPresent = present;
            _value = value;
        }

        public T OrElse(T fallback)
        {
            return IsPresent ? Value : fallback;
        }
    }
}
