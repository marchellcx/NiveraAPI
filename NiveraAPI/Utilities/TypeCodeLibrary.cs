using System.Collections.Concurrent;

namespace NiveraAPI.Utilities
{
    /// <summary>
    /// Manages codes of all loaded types.
    /// </summary>
    public static class TypeCodeLibrary
    {
        /// <summary>
        /// Stores type codes keyed by types.
        /// </summary>
        public static volatile ConcurrentDictionary<Type, int> TypeToCodeCache = new();

        /// <summary>
        /// Stores type codes keyed by codes.
        /// </summary>
        public static volatile ConcurrentDictionary<int, Type> CodeToTypeCache = new();

        /// <summary>
        /// Gets a stable hash code for a type.
        /// </summary>
        /// <param name="type">The target type.</param>
        /// <returns>The generated stable hash code.</returns>
        public static int GenerateLibraryCode(this Type type)
        {
            unchecked
            {
                var str = type.AssemblyQualifiedName;

                var hash = 5381;
                var hash2 = hash;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash = ((hash << 5) + hash) ^ str[i];

                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;

                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash + (hash2 * 1566083941);
            }
        }

        /// <summary>
        /// Gets a type from a library code.
        /// </summary>
        /// <param name="code">The code of the type.</param>
        /// <returns>The found library type.</returns>
        public static Type GetLibraryType(int code)
        {
            if (!CodeToTypeCache.TryGetValue(code, out var type))
                throw new TypeLoadException($"Could not find type '{code}' in the library cache!");

            return type;
        }

        /// <summary>
        /// Gets the library code of a type.
        /// </summary>
        /// <param name="type">The target type.</param>
        /// <returns>The type's library code.</returns>
        /// <exception cref="ArgumentNullException">type is null</exception>
        public static int GetLibraryCode(this Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (TypeToCodeCache.TryGetValue(type, out var code))
                return code;

            code = type.GenerateLibraryCode();

            TypeToCodeCache.TryAdd(type, code);
            CodeToTypeCache.TryAdd(code, type);

            return code;
        }

        /// <summary>
        /// Saves a type code to the cache.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void SaveLibraryCode(this Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            var code = type.GenerateLibraryCode();

            TypeToCodeCache.TryAdd(type, code);
            CodeToTypeCache.TryAdd(code, type);
        }
    }
}