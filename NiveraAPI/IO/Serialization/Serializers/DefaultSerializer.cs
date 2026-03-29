using FastGenericNew;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Serialization.Serializers;

/// <summary>
/// The <c>DefaultSerializer</c> class provides a general-purpose implementation of the
/// <see cref="IObjectSerializer"/> interface for objects implementing the <see cref="ISerializableObject"/> interface.
/// It uses a customizable <see cref="Constructor"/> delegate to create and serialize instances of the specified type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">
/// The type of the objects being serialized and deserialized, constrained to implement the <see cref="ISerializableObject"/> interface.
/// </typeparam>
/// <remarks>
/// This class is designed to provide a default serialization mechanism that can be overridden or extended
/// via the <see cref="Constructor"/> delegate property and other custom serialization logic if needed.
/// </remarks>
public class DefaultSerializer<T> : IObjectSerializer
    where T : ISerializableObject
{
    private static volatile ushort index;
    
    /// <summary>
    /// Gets the singleton instance of the <see cref="DefaultSerializer{T}"/> class.
    /// </summary>
    public static volatile DefaultSerializer<T> Singleton = new();
    
    private DefaultSerializer() { }

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
        => DefaultSerializer<T>.index = index;

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
        => StaticConstructor<T>.Construct();

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
}