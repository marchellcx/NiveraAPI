using System.Reflection;
using NiveraAPI.Logs;
using NiveraAPI.Pooling;

namespace NiveraAPI.Extensions
{
    /// <summary>
    /// Provides extension methods for working with assemblies, including retrieving assembly names and loading
    /// instances of types that match specified criteria.
    /// </summary>
    public static class AssemblyExtensions
    {
        private static volatile LogSink log = LogManager.GetSource("EXT", "Assembly");
        
        /// <summary>
        /// Invokes all static methods in the specified assembly that match the given predicate, passing the provided
        /// arguments to each method.
        /// </summary>
        /// <remarks>Only static methods declared directly on each type in the assembly are considered. If
        /// a method throws an exception during invocation, the exception is logged and the process continues with the
        /// next method. This method does not throw exceptions for individual method invocation failures.</remarks>
        /// <param name="assembly">The assembly whose static methods are to be invoked. Cannot be null.</param>
        /// <param name="predicate">A function that determines whether a static method should be invoked. The method is invoked if this function
        /// returns <see langword="true"/> for the given <see cref="MethodInfo"/>.</param>
        /// <param name="args">An array of arguments to pass to each invoked static method. The arguments must match the parameters of the
        /// target methods.</param>
        public static void InvokeStaticMethods(this Assembly assembly, Func<MethodInfo, bool> predicate, params object[] args)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetAllMethods()
                             .Where(x => x.DeclaringType == type))
                {
                    try
                    {
                        if (!method.IsStatic)
                            continue;

                        if (!predicate(method))
                            continue;

                        method.Invoke(null, args);
                    }
                    catch (Exception ex)
                    {
                        log.Error(nameof(InvokeStaticMethods),
                            $"Failed to invoke static method &3{method.GetMemberName()}&r in assembly " +
                            $"&3{assembly.GetName().Name}&r due to an exception:\n{ex}");
                    }
                }
            }
        }

        /// <summary>
        /// Invokes all static methods in the specified assembly that match the given predicate, in an order determined
        /// by the provided priority selector.
        /// </summary>
        /// <remarks>If a method invocation throws an exception, the exception is logged and the
        /// invocation continues with the next method. Only static methods declared directly on each type are
        /// considered; inherited static methods are not included.</remarks>
        /// <param name="assembly">The assembly whose static methods are to be discovered and invoked.</param>
        /// <param name="predicate">A function that determines whether a static method should be invoked. The method is invoked only if this
        /// predicate returns <see langword="true"/>.</param>
        /// <param name="prioritySelector">A function that assigns a priority value to each method. Methods are invoked in order based on this value.</param>
        /// <param name="isDescending">A value indicating whether methods should be invoked in descending order of priority. If <see
        /// langword="true"/>, methods with higher priority values are invoked first.</param>
        /// <param name="args">An array of arguments to pass to each static method when invoking it. The arguments must match the
        /// parameters of the methods being invoked.</param>
        public static void InvokeStaticMethods(this Assembly assembly, Func<MethodInfo, bool> predicate, Func<MethodInfo, int> prioritySelector, bool isDescending, params object[] args)
        {
            var types = assembly.GetTypes();
            var methods = ListPool<MethodInfo>.Shared.Rent();

            foreach (var type in types)
            {
                foreach (var method in type.GetAllMethods())
                {
                    if (method.DeclaringType is null || method.DeclaringType != type)
                        continue;

                    if (!method.IsStatic || !predicate(method))
                        continue;

                    methods.Add(method);
                }
            }

            var orderedMethods = isDescending 
                ? methods.OrderByDescending(prioritySelector)
                : methods.OrderBy(prioritySelector);

            foreach (var method in orderedMethods)
            {
                try
                {
                    method.Invoke(null, args);
                }
                catch (Exception ex)
                {
                    log.Error(nameof(InvokeStaticMethods),$"Failed to invoke static method &1{method.GetMemberName()}&r:\n{ex}");
                }
            }

            ListPool<MethodInfo>.Shared.Return(methods);
        }
        
        /// <summary>
        /// Loads all assemblies from the specified directory that match the ".dll" file extension.
        /// </summary>
        /// <remarks>Assemblies that fail to load are skipped, and errors are logged. Only files with a
        /// ".dll" extension are considered. This method does not search subdirectories.</remarks>
        /// <param name="directory">The path to the directory from which to load assemblies. Cannot be null or empty.</param>
        /// <param name="loadRaw">true to load assemblies by reading their raw bytes; false to load assemblies using their file paths. Loading
        /// by raw bytes can be useful when files may be locked or in use by other processes.</param>
        /// <returns>A list of assemblies loaded from the specified directory. The list is empty if the directory does not exist
        /// or contains no valid assemblies.</returns>
        /// <exception cref="ArgumentNullException">Thrown if directory is null or empty.</exception>
        public static List<Assembly> LoadAssembliesFrom(string directory, bool loadRaw = true)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));

            if (!Directory.Exists(directory))
                return new();

            var list = new List<Assembly>();
            var files = Directory.GetFiles(directory, "*.dll");

            for (var x = 0; x < files.Length; x++)
            {
                try
                {
                    var file = files[x];

                    var assembly = loadRaw
                        ? Assembly.Load(File.ReadAllBytes(file))
                        : Assembly.LoadFrom(file);

                    if (assembly == null)
                        continue;

                    list.Add(assembly);
                }
                catch (Exception ex)
                {
                    log.Error(nameof(LoadAssembliesFrom), $"Could not load assembly '{files[x]}':\n{ex}");
                }
            }

            return list;
        }

        /// <summary>
        /// Gets the simple (short) name of the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly from which to retrieve the simple name. Can be null.</param>
        /// <returns>The simple name of the assembly, or "(null)" if <paramref name="assembly"/> is null. Returns "(unknown)" if
        /// the assembly's name cannot be determined, or an empty string if the simple name is not available.</returns>
        public static string GetSimpleName(this Assembly assembly)
        {
            if (assembly == null)
                return "(null)";

            var name = assembly.GetName();

            if (name == null)
                return assembly.FullName ?? "(unknown)";

            return name.Name ?? "(unknown)";
        }

        /// <summary>
        /// Creates and returns a list of instances of all non-abstract, non-generic types in the specified assembly
        /// that implement or inherit from the specified type parameter.
        /// </summary>
        /// <remarks>Only types with a public or non-public parameterless constructor are instantiated. If
        /// a type defines a parameterless method named OnAssemblyLoaded, it is invoked on the instance (or statically)
        /// if invokeOnAssemblyLoaded is true. Loader exceptions encountered while retrieving types from the assembly
        /// are logged but do not prevent processing of successfully loaded types.</remarks>
        /// <typeparam name="TType">The base type or interface to search for. Only types assignable to this type are instantiated and included
        /// in the result.</typeparam>
        /// <param name="assembly">The assembly to search for types that can be instantiated as instances of the specified type parameter.</param>
        /// <param name="invokeOnAssemblyLoaded">true to invoke a parameterless OnAssemblyLoaded method on each created instance (if present); otherwise,
        /// false.</param>
        /// <returns>A list of instances of types from the assembly that are assignable to the specified type parameter. The list
        /// is empty if no matching types are found.</returns>
        /// <exception cref="ArgumentNullException">Thrown if assembly is null.</exception>
        public static List<TType> LoadTypes<TType>(this Assembly assembly, bool invokeOnAssemblyLoaded = true)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var list = new List<TType>();
            var types = Array.Empty<Type>();

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;

                if (ex.LoaderExceptions?.Length > 0)
                {
                    for (var x = 0; x < ex.LoaderExceptions.Length; x++)
                    {
                        var loaderException = ex.LoaderExceptions[x];

                        log.Error(nameof(LoadTypes), $"Loader exception {x}: {loaderException}");
                    }
                }
            }

            if (types?.Length < 1)
            {
                return list;
            }

            for (var x = 0; x < types.Length; x++)
            {
                try
                {
                    var type = types[x];

                    if (type.IsAbstract || type.IsGenericTypeDefinition || type.IsInterface) 
                        continue;

                    if (!typeof(TType).IsAssignableFrom(type))
                        continue;

                    if (type.GetConstructors().FirstOrDefault(x => x.GetParameters()?.Length < 1) == null) 
                        continue;

                    if (Activator.CreateInstance(type) is not TType instance) 
                        continue;

                    list.Add(instance);

                    if (invokeOnAssemblyLoaded)
                    {
                        var method = type.GetMethod("OnAssemblyLoaded", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                        if (method == null || method.GetParameters()?.Length > 0 || method.ReturnType != typeof(void))
                            continue;

                        try
                        {
                            method.Invoke(method.IsStatic ? null : instance, Array.Empty<object>());
                        }
                        catch (Exception ex)
                        {
                            log.Error(nameof(LoadTypes), $"Error invoking OnAssemblyLoaded for type {type.FullName}: {ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(nameof(LoadTypes), ex);
                }
            }

            return list;
        }
    }
}