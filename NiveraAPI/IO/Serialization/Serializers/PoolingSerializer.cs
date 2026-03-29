using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.Pooling;
using NiveraAPI.Pooling.Interfaces;

namespace NiveraAPI.IO.Serialization.Serializers;

/// <summary>
/// Responsible for serializing and deserializing objects that implement
/// <see cref="ISerializableObject"/> and <see cref="IPoolResettable"/>
/// using a pooling mechanism. This ensures efficient reuse of objects
/// through memory pooling, reducing the overhead of frequent allocations
/// and deallocations.
/// </summary>
/// <typeparam name="T">
/// The type of object to be serialized and deserialized. Must implement
/// both <see cref="ISerializableObject"/> and <see cref="IPoolResettable"/>.
/// </typeparam>
public class PoolingSerializer<T> : IObjectSerializer, IDisposingSerializer
    where T : class, ISerializableObject, IPoolResettable
{
    private static volatile ushort index;
    
    /// <summary>
    /// Gets the singleton instance of the <see cref="PoolingSerializer{T}"/> class.
    /// </summary>
    public static volatile PoolingSerializer<T> Singleton = new();
    
    private PoolingSerializer() { }

    /// <summary>
    /// Retrieves the current internal index value associated with the serializer instance.
    /// This value is used to uniquely identify the specific instance of the <c>DefaultSerializer</c>.
    /// </summary>
    /// <returns>
    /// A 16-bit unsigned integer representing the internal index of the serializer instance.
    /// </returns>
    public ushort GetIndex()
        => index;

    /// <summary>
    /// Sets the internal index value for the serializer instance.
    /// This index is used to associate an identifier with the current instance of the DefaultSerializer.
    /// </summary>
    /// <param name="index">
    /// The 16-bit unsigned integer value to set as the internal index of the serializer instance.
    /// </param>
    public void UpdateIndex(ushort index)
        => PoolingSerializer<T>.index = index;

    /// <summary>
    /// Creates and returns a new instance of a serializable object.
    /// The created object must conform to the ISerializableObject interface, allowing
    /// it to be serialized and deserialized using the associated objectSerializer.
    /// </summary>
    /// <returns>
    /// An instance of a class implementing the ISerializableObject interface, ready for
    /// serialization or deserialization.
    /// </returns>
    public ISerializableObject Construct()
    {
        return PoolBase<T>.Shared.Rent();
    }

    /// <summary>
    /// Converts the state of a given serializableObject object into a sequence of bytes
    /// and writes the resulting data using the specified byte writer.
    /// </summary>
    /// <param name="serializableObject">
    /// The object implementing the ISerializableObject interface whose state is to be serialized.
    /// </param>
    /// <param name="writer">
    /// The ByteWriter instance used to write the serialized data to the target output buffer.
    /// </param>
    public void Serialize(ISerializableObject serializableObject, ByteWriter writer)
    {
        serializableObject.Serialize(writer);
    }

    /// <summary>
    /// Reconstructs the state of a serializableObject object from its serialized data.
    /// The deserialization process reads data from the provided ByteReader
    /// and populates the specified object with the reconstructed state.
    /// </summary>
    /// <param name="serializableObject">
    /// The instance of the object implementing ISerializableObject whose state
    /// is to be deserialized.
    /// </param>
    /// <param name="reader">
    /// The ByteReader instance from which the serialized data will be read.
    /// This reader should contain the serialized form of the object's state.
    /// </param>
    public void Deserialize(ISerializableObject serializableObject, ByteReader reader)
    {
        serializableObject.Deserialize(reader);
    }
    
    /// <summary>
    /// Disposes the given object.
    /// </summary>
    /// <param name="obj">The object to dispose.</param>
    public void DisposeObject(ISerializableObject obj)
    {
        PoolBase<T>.Shared.Return((T)obj);
    }
}