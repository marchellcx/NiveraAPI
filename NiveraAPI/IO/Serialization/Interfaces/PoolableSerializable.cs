using NiveraAPI.IO.Serialization.Serializers;

using NiveraAPI.Pooling;
using NiveraAPI.Pooling.Interfaces;

namespace NiveraAPI.IO.Serialization.Interfaces;

/// <summary>
/// A generic class that combines functionality for pooling, serialization, and object disposal.
/// It extends <see cref="PoolResettable"/> and implements <see cref="ISerializableObject"/>
/// and <see cref="IDisposableObject"/> to support object lifecycle management within a pooled context.
/// </summary>
/// <typeparam name="T">
/// The type of objects managed by this class. Must implement <see cref="IPoolResettable"/> and <see cref="ISerializableObject"/>.
/// </typeparam>
public class PoolableSerializable<T> : PoolResettable, ISerializableObject, IDisposableObject
    where T : class, IPoolResettable, ISerializableObject
{
    /// <summary>
    /// Gets the objectSerializer responsible for serializing and deserializing the object.
    /// </summary>
    public IObjectSerializer Serializer => PoolingSerializer<T>.Singleton;

    /// <summary>
    /// Serializes the current state of the object to the provided ByteWriter instance.
    /// </summary>
    /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
    public virtual void Serialize(ByteWriter writer)
    {
        
    }

    /// <summary>
    /// Deserializes data from the provided ByteReader instance into the current object's state.
    /// </summary>
    /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
    public virtual void Deserialize(ByteReader reader)
    {
        
    }

    /// <summary>
    /// Whether or not the object should be disposed.
    /// </summary>
    /// <returns>true if the object should be disposed, false otherwise.</returns>
    public virtual bool ShouldDispose()
    {
        return true;
    }
    
    /// <summary>
    /// Places the object back into the pool for reuse by resetting its state and performing any necessary cleanup.
    /// This method must be implemented by derived classes to define specific reset behavior.
    /// </summary>
    /// <remarks>
    /// This method is intended to be called when the object is no longer in use and should be returned to a reusable state.
    /// Implementations should ensure that the object is properly prepared for its next usage and does not retain any stale references or data.
    /// </remarks>
    public override void ReturnToPool()
    {
        
    }
}