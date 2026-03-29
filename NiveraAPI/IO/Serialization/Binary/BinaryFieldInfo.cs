using System.Reflection;

using HarmonyLib;

namespace NiveraAPI.IO.Serialization.Binary;

/// <summary>
/// Represents a serializable field.
/// </summary>
public class BinaryFieldInfo
{
    /// <summary>
    /// The targeted field.
    /// </summary>
    public FieldInfo Field { get; set; }
    
    /// <summary>
    /// The serializer for the field.
    /// </summary>
    public KeyValuePair<FastInvokeHandler, object> Serializer { get; set; }
    
    /// <summary>
    /// The deserializer for the field.
    /// </summary>
    public KeyValuePair<FastInvokeHandler, object> Deserializer { get; set; }
    
    /// <summary>
    /// The reference to the field.
    /// </summary>
    public AccessTools.FieldRef<object, object> Reference { get; set; }
}