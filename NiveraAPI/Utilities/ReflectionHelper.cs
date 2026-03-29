using System.Diagnostics;
using System.Reflection;

namespace NiveraAPI.Utilities;

/// <summary>
/// Utilities targeting the reflection API.
/// </summary>
public static class ReflectionHelper
{
    /// <summary>
    /// Gets called once a new type is discovered.
    /// </summary>
    public static event Action<Type>? Discovered;

    /// <summary>
    /// Gets a list of all loaded types.
    /// </summary>
    public static volatile Type[] Types = Array.Empty<Type>();
    
    /// <summary>
    /// Gets a list of all loaded assemblies.
    /// </summary>
    public static volatile Assembly[] Assemblies = Array.Empty<Assembly>();

    /// <summary>
    /// Attempts to find a type by its name among the currently loaded types, with an option
    /// to perform a case-insensitive comparison.
    /// </summary>
    /// <param name="name">The name of the type to search for. This can be the full name, short name, or assembly-qualified name of the type.</param>
    /// <param name="ignoreCase">true to perform a case-insensitive comparison of the name; false to require an exact match.</param>
    /// <param name="type">When this method returns, contains the type that matches the specified name, if found; otherwise, null.</param>
    /// <returns>true if a type matching the specified name is found; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided name is null, empty, or consists only of whitespace.</exception>
    public static bool TryFindType(string name, bool ignoreCase, out Type? type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        type = null;

        for (var x = 0; x < Types.Length; x++)
        {
            var cur = Types[x];

            if (cur.FullName == name
                || cur.Name == name
                || cur.AssemblyQualifiedName == name
                || (ignoreCase && (cur.FullName ?? string.Empty).Equals(name, StringComparison.OrdinalIgnoreCase)
                    || cur.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    || (cur.AssemblyQualifiedName ?? string.Empty).Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                type = cur;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the assembly of the calling method in the current call stack, with options to skip frames and filter
    /// assemblies.
    /// </summary>
    /// <remarks>This method can be used to identify the assembly that invoked the current code, which is
    /// useful for scenarios such as plugin discovery or diagnostics. The skipFrameCount parameter allows you to control
    /// how many stack frames to skip, which can be helpful when wrapping this method in utility functions. If
    /// ignoreAssembly is provided, any assemblies for which the predicate returns true will be skipped when searching
    /// for the caller.</remarks>
    /// <param name="skipFrameCount">The number of stack frames to skip before determining the caller assembly. Must be zero or greater.</param>
    /// <param name="throwIfNotFound">true to throw an exception if no suitable assembly is found; otherwise, false.</param>
    /// <param name="ignoreAssembly">A predicate used to exclude specific assemblies from consideration. If null, no assemblies are ignored.</param>
    /// <returns>The assembly of the first calling method in the stack that is not ignored by the specified predicate.</returns>
    /// <exception cref="Exception">Thrown if no suitable calling assembly is found and throwIfNotFound is true.</exception>
    public static Assembly GetCallerAssembly(int skipFrameCount, bool throwIfNotFound, Predicate<Assembly>? ignoreAssembly = null)
    {
        var frames = new StackTrace().GetFrames();

        for (var i = 0 + skipFrameCount; i < frames.Length; i++)
        {
            var method = frames[i].GetMethod();

            if (method is null)
                continue;

            var assembly = method.DeclaringType?.Assembly ?? method.ReflectedType.Assembly;

            if (ignoreAssembly is null || !ignoreAssembly(assembly))
                return assembly;
        }

        if (throwIfNotFound)
            throw new Exception("Could not find caller assembly.");

        return null!;
    }

    /// <summary>
    /// Invokes the <see cref="Discovered"/> event for all types in the <see cref="Types"/> collection.
    /// </summary>
    /// <remarks>
    /// This method iterates through the <see cref="Types"/> collection and triggers the <see cref="Discovered"/> event
    /// for each <see cref="Type"/>. Exceptions thrown by individual event handlers are caught and ignored.
    /// </remarks>
    /// <exception cref="NullReferenceException">Thrown if the <see cref="Discovered"/> event is null when invoked. This should generally not occur unless the method is improperly modified.</exception>
    public static void CallDiscovered()
    {
        if (Discovered != null)
        {
            foreach (var type in Types)
            {
                try
                {
                    Discovered.Invoke(type);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    private static void OnLoaded(object _, AssemblyLoadEventArgs ev)
    {
        try
        {
            Assemblies = Assemblies
                .Append(ev.LoadedAssembly)
                .ToArray();
            
            var types = ev.LoadedAssembly.GetTypes();
            
            Types = types.Where(x => !Types.Contains(x))
                .Concat(Types)
                .ToArray();
            
            foreach (var type in types)
            {
                try
                {
                    Discovered?.Invoke(type);
                }
                catch
                {
                    // ignored
                }
            }
        }
        catch
        {
            // ignored
        }
    }
    
    internal static void Initialize()
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Assemblies = assemblies.ToArray();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    Types = Types
                        .Concat(assembly.GetTypes())
                        .ToArray();
                }
                catch
                {
                    // ignored
                }
            }
        }
        catch
        {
            // ignored
        }

        AppDomain.CurrentDomain.AssemblyLoad += OnLoaded;
    }
}