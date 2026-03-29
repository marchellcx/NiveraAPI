namespace NiveraAPI.IO.Serialization.Binary;

/// <summary>
/// Marks a field or property as serializable.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, 
    AllowMultiple = false, 
    Inherited = false)]
public class BinaryFieldAttribute : Attribute { }