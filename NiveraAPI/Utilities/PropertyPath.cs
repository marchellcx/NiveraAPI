using System.Reflection;

namespace NiveraAPI.Utilities;

/// <summary>
/// Represents a structure for handling and resolving property paths within an object hierarchy.
/// </summary>
public class PropertyPath
{
    private string[] path;
    private Dictionary<Type, List<MethodInfo>> cachedPaths;

    /// <summary>
    /// The property path to resolve.
    /// </summary>
    public string[] Path => path;

    /// <summary>
    /// Creates a new <see cref="PropertyPath"/> instance.
    /// </summary>
    /// <param name="path">The property path to resolve.</param>
    /// <exception cref="ArgumentNullException">Thrown if the property path is null.</exception>
    public PropertyPath(string[] path)
    {
        this.path = path ?? throw new ArgumentNullException(nameof(path));
        this.cachedPaths = new();
    }

    /// <summary>
    /// Resolves an object by traversing the properties specified in the path for the given object.
    /// </summary>
    /// <param name="result">The object to resolve based on the path. Can be null.</param>
    /// <returns>The resolved object after traversing the properties specified in the path. Returns null if the input result is null.</returns>
    /// <exception cref="Exception">
    /// Thrown if an invalid property path is specified, or if a property in the path does not have a getter.
    /// </exception>
    public object? Resolve(object? result)
    {
        if (result == null)
            return result;

        var type = result.GetType();

        if (!cachedPaths.TryGetValue(type, out var path))
        {
            path = new();

            for (var x = 0; x < Path.Length; x++)
            {
                var property = type.GetProperty(Path[x], BindingFlags.Public | BindingFlags.NonPublic);

                if (property == null)
                    throw new Exception($"Invalid path '{Path[x]}' ({x}) for type '{type.FullName}': unknown property");

                var getter = property.GetGetMethod(true);
                
                if (getter == null)
                    throw new Exception($"Invalid path '{Path[x]}' ({x}) for type '{type.FullName}': property has no getter");
                
                path.Add(getter);
            }
            
            cachedPaths.Add(type, path);
        }

        for (var x = 0; x < path.Count; x++)
            result = path[x].Invoke(result, null);

        return result;
    }
}