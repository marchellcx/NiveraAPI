namespace NiveraAPI.IO.Serialization;

/// <summary>
/// Indicates that a field or property contains a serializer / deserializer delegate.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public class SerializerAttribute : Attribute { }