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
        /// Safely attempts to cast the provided object to the specified type. If the cast is not valid, returns the default value for the type.
        /// </summary>
        /// <param name="obj">
        /// The object to be cast to the specified type.
        /// </param>
        /// <typeparam name="T">
        /// The target type to which the object is to be cast.
        /// </typeparam>
        /// <returns>
        /// An instance of type <typeparamref name="T"/> if the cast is successful; otherwise, the default value of type <typeparamref name="T"/>.
        /// </returns>
        public static T? SafeAs<T>(this object obj)
        {
            if (!obj.Is<T>(out var t))
                return default;

            return t;
        }

        /// <summary>
        /// Casts the specified object to the target type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="obj">
        /// The object to be cast to the specified type.
        /// </param>
        /// <typeparam name="T">
        /// The target type to cast the object to.
        /// </typeparam>
        /// <returns>
        /// The object cast to the type <typeparamref name="T"/>.
        /// </returns>
        public static T As<T>(this object obj)
        {
            return (T)obj;
        }

        /// <summary>
        /// Executes the specified action if the object can be cast to the specified type.
        /// </summary>
        /// <typeparam name="T">
        /// The type to which the object is attempted to be cast.
        /// </typeparam>
        /// <param name="obj">
        /// The object on which the check and cast operation is performed.
        /// </param>
        /// <param name="func">
        /// The action to execute if the object can be successfully cast to the specified type.
        /// </param>
        public static void IfIs<T>(this object obj, Action<T> func)
        {
            if (obj.Is<T>(out var t))
            {
                func?.Invoke(t);
            }
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