namespace NiveraAPI.IO.Serialization.Interfaces;

/// <summary>
/// Defines a contract for a objectSerializer responsible for transforming objects
/// to and from a serialized format. An implementation should provide methods
/// for creating new serializable objects, converting serializable objects
/// into a sequence of bytes, and reconstructing objects from serialized data.
/// </summary>
public interface IObjectSerializer
{
    /// <summary>
    /// Gets the index of the objectSerializer.
    /// </summary>
    /// <returns>The index.</returns>
    ushort GetIndex();
    
    /// <summary>
    /// Creates and returns a new instance of a serializable object.
    /// The created object must conform to the ISerializableObject interface, allowing
    /// it to be serialized and deserialized using the associated objectSerializer.
    /// </summary>
    /// <returns>
    /// An instance of a class implementing the ISerializableObject interface, ready for
    /// serialization or deserialization.
    /// </returns>
    ISerializableObject Construct();

    /// <summary>
    /// Updates the index of the objectSerializer.
    /// </summary>
    /// <param name="index">The new index to assign to the objectSerializer.</param>
    void UpdateIndex(ushort index);

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
    void Serialize(ISerializableObject serializableObject, ByteWriter writer);

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
    void Deserialize(ISerializableObject serializableObject, ByteReader reader);
}