using FastGenericNew;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;
using NiveraAPI.Logs;
using NiveraAPI.Pooling;
using NiveraAPI.Pooling.Interfaces;
using NiveraAPI.Extensions;
using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Serialization;

/// <summary>
/// Provides utility methods to manage and interact with serializers. This class supports
/// registering, unregistering, and retrieving serializers, maintaining a collection of
/// registered serializers.
/// </summary>
public static class ObjectSerializer
{
    private static IObjectSerializer[] serializers;
    private static LogSink log = LogManager.GetSource("IO", "ObjectSerializer");

    /// <summary>
    /// Retrieves an object serializer from the collection of registered serializers
    /// based on the specified index.
    /// </summary>
    /// <param name="index">
    /// The index of the objectSerializer to retrieve. The index must be a valid
    /// position within the collection of registered serializers.
    /// </param>
    /// <returns>
    /// The objectSerializer located at the specified index if the index is valid
    /// and serializers have been initialized; otherwise, null.
    /// </returns>
    public static IObjectSerializer? GetSerializer(ushort index)
    {
        if (serializers == null)
            return null;

        index--;
        
        if (index < 0 || index >= serializers.Length)
            return null;
        
        return serializers[index];
    }

    /// <summary>
    /// Removes an object serializer from the collection of registered serializers.
    /// If the specified objectSerializer is found and successfully removed,
    /// the remaining serializers are re-ordered by their type's full name.
    /// </summary>
    /// <param name="objectSerializer">
    /// The objectSerializer to be unregistered. Must implement the IObjectSerializer interface.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the objectSerializer was successfully unregistered.
    /// Returns true if unregistered successfully, otherwise false.
    /// </returns>
    public static bool UnregisterSerializer(IObjectSerializer objectSerializer)
    {
        if (serializers == null)
            return false;
        
        var index = serializers.IndexOf(objectSerializer);
        
        if (index == -1)
            return false;
        
        serializers = serializers
            .Where(x => x != objectSerializer)
            .OrderBy(x => x.GetType().FullName)
            .ToArray();
        
        objectSerializer.UpdateIndex(0);
        
        UpdateIndexes();
        return true;
    }

    /// <summary>
    /// Adds a objectSerializer to the collection of registered serializers.
    /// If the objectSerializer is already registered, its existing index is returned.
    /// Otherwise, the objectSerializer is added, and the collection is re-ordered by the full name of each objectSerializer's type.
    /// </summary>
    /// <param name="objectSerializer">
    /// The objectSerializer to be registered. Must implement the IObjectSerializer interface. Cannot be null.
    /// </param>
    /// <returns>
    /// The index at which the objectSerializer is registered within the collection of serializers.
    /// Returns the existing index if the objectSerializer was already registered.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided objectSerializer is null.
    /// </exception>
    public static void RegisterSerializer(IObjectSerializer objectSerializer)
    {
        if (objectSerializer == null)
            throw new ArgumentNullException(nameof(objectSerializer));

        if (serializers == null)
        {
            serializers = new IObjectSerializer[1];
            
            serializers[0] = objectSerializer;
            
            objectSerializer.UpdateIndex(1);
            return;
        }
        
        var index = serializers.IndexOf(objectSerializer);

        if (index != -1)
            return;

        serializers = serializers
            .Append(objectSerializer)
            .OrderBy(x => x.GetType().FullName)
            .ToArray();
        
        UpdateIndexes();
    }

    /// <summary>
    /// Registers a default serializer for a specified type that implements the
    /// ISerializableObject interface. An optional constructor can be provided
    /// for creating instances of the type during deserialization.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the object for which the default serializer is being registered.
    /// The type must implement the ISerializableObject interface.
    /// </typeparam>
    /// <param name="constructor">
    /// An optional function that specifies a constructor to create instances of
    /// the specified type. If provided, it will be used during deserialization.
    /// </param>
    public static void RegisterDefaultSerializer<T>(Func<T>? constructor = null) where T : ISerializableObject
    {
        RegisterSerializer(DefaultSerializer<T>.Singleton);
        
        if (constructor != null)
            StaticConstructor<T>.Set(constructor);       
    }

    /// <summary>
    /// Registers a pooling serializer for a specific type. The pooling serializer
    /// enables efficient management of serializable objects that implement both
    /// <see cref="ISerializableObject"/> and <see cref="IPoolResettable"/>, allowing
    /// for object reuse and minimized allocations.
    /// </summary>
    /// <param name="constructor">
    /// An optional factory function used to create new instances of the specified type.
    /// If provided, it will be used as the default constructor for instances managed
    /// by the serializer.
    /// </param>
    /// <typeparam name="T">
    /// The type of object to be managed by the pooling serializer. The type must implement
    /// both <see cref="ISerializableObject"/> and <see cref="IPoolResettable"/>.
    /// </typeparam>
    public static void RegisterPoolingSerializer<T>(Func<T>? constructor = null)
        where T : class, ISerializableObject, IPoolResettable
    {
        RegisterSerializer(PoolingSerializer<T>.Singleton);
        
        if (constructor != null)
            StaticConstructor<T>.Set(constructor);       
    }

    private static void UpdateIndexes()
    {
        for (var x = 0; x < serializers.Length; x++)
        {
            try
            {
                serializers[x].UpdateIndex((ushort)(x + 1));
            }
            catch (Exception ex)
            {
                log.Error($"Failed to update index for serializer &1{serializers[x].GetType().FullName}&r: {ex}");
            }
        }
    }
}