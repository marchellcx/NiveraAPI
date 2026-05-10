namespace NiveraAPI.Extensions
{
    /// <summary>
    /// Extensions targeting anonymous objects.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Whether or not the object is of a specific type.
        /// </summary>
        public static bool Is<T>(this object instance)
            => instance != null && instance is T;

        /// <summary>
        /// Whether or not the object is of a specific type.
        /// </summary>
        public static bool Is<T>(this object instance, out T result)
        {
            result = default!;

            if (instance is null || instance is not T cast)
                return false;

            result = cast;
            return true;
        }

        /// <summary>
        /// Determines whether the current object is equal to another object.
        /// </summary>
        /// <param name="instance">The current object instance being compared.</param>
        /// <param name="otherInstance">The object being compared to the current instance.</param>
        /// <param name="countNull">Indicates whether to treat two null objects as equal.</param>
        /// <returns>True if the objects are considered equal; otherwise, false.</returns>
        public static bool IsEqualTo(this object instance, object otherInstance, bool countNull = false)
        {
            if (instance is null && otherInstance is null)
                return countNull;

            if ((instance is null && otherInstance != null) || (instance != null && otherInstance is null))
                return false;

            return instance == otherInstance;
        }

        /// <summary>
        /// Copies all properties that have a setter and a getter from one object instance to another.
        /// </summary>
        public static void CopyPropertiesFrom(this object target, object instance)
            => CopyPropertiesTo(instance, target);

        /// <summary>
        /// Copies all properties that have a setter and a getter from one object instance to another.
        /// </summary>
        public static void CopyPropertiesTo(this object instance, object target)
        {
            if (instance is null || target is null)
                return;

            var props = instance.GetType().GetAllProperties();

            foreach (var prop in props)
            {
                if (prop.GetSetMethod(true) is null)
                    continue;

                if (prop.GetGetMethod(true) is null)
                    continue;

                prop.SetValue(target, prop.GetValue(instance));
            }
        }
    }
}