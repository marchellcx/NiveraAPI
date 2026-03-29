using HarmonyLib;

using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Serialization.Binary;

/// <summary>
/// Represents a serializable class.
/// </summary>
public class BinaryClassInfo
{
    /// <summary>
    /// The type targeted.
    /// </summary>
    public Type Type { get; set; }
    
    /// <summary>
    /// Whether or not the target type is a simple type with an already existing serializer (like Int32).
    /// </summary>
    public bool IsSimple { get; set; }
    
    /// <summary>
    /// The constructor for the class.
    /// </summary>
    public Func<object> Constructor => StaticNonGenericConstructor.GetConstructor(Type);
    
    /// <summary>
    /// The serializer for the class.
    /// </summary>
    public KeyValuePair<FastInvokeHandler, object> Serializer { get; set; }
    
    /// <summary>
    /// The deserializer for the class.
    /// </summary>
    public KeyValuePair<FastInvokeHandler, object> Deserializer { get; set; }
    
    /// <summary>
    /// A list of all compatible fields in the class.
    /// </summary>
    public List<BinaryFieldInfo> Fields { get; } = new();

    /// <summary>
    /// A list of all compatible properties in the class.
    /// </summary>
    public List<BinaryPropertyInfo> Properties { get; } = new();
}