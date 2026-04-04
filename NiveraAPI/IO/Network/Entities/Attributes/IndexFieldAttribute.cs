namespace NiveraAPI.IO.Network.Entities.Attributes;

/// <summary>
/// Marks a field that holds an index of a network member.
/// </summary>
[AttributeUsage(AttributeTargets.Field,
    AllowMultiple = false, 
    Inherited = true)]
public class IndexFieldAttribute : Attribute
{
    /// <summary>
    /// The name of the targeted network member.
    /// </summary>
    public string? Name { get; }
    
    /// <summary>
    /// Creates a new instance of the IndexFieldAttribute class.
    /// </summary>
    /// <param name="name">The name of the targeted network member.</param>
    public IndexFieldAttribute(string? name = null)
    {
        Name = name;
    }
}