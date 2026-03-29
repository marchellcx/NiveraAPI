using System.Reflection;
    
using HarmonyLib;

namespace NiveraAPI.IO.Serialization.Binary;

/// <summary>
/// Represents a serializable field.
/// </summary>
public class BinaryPropertyInfo
{
    /// <summary>
    /// The targeted property.
    /// </summary>
    public PropertyInfo Property { get; set; }
    
    /// <summary>
    /// The serializer for the field.
    /// </summary>
    public KeyValuePair<FastInvokeHandler, object> Serializer { get; set; }
    
    /// <summary>
    /// The deserializer for the field.
    /// </summary>
    public KeyValuePair<FastInvokeHandler, object> Deserializer { get; set; }
    
    /// <summary>
    /// The getter for the property.
    /// </summary>
    public FastInvokeHandler Getter { get; set; }
    
    /// <summary>
    /// The setter for the property.
    /// </summary>
    public FastInvokeHandler Setter { get; set; }
}