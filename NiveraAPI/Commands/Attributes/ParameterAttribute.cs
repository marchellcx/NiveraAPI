namespace NiveraAPI.Commands.Attributes;

/// <summary>
/// Represents a parameter of a command overload.
/// </summary>
[AttributeUsage(AttributeTargets.Method, 
    AllowMultiple = true, 
    Inherited = false)]
public class ParameterAttribute : Attribute
{
    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// The description of the parameter.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Creates a new parameter attribute.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="description">The description of the parameter.</param>
    /// <exception cref="ArgumentNullException">Thrown if name or description is null.</exception>
    public ParameterAttribute(string name, string description = "No description provided.")
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}