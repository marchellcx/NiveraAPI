using System.Reflection;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;

using NiveraAPI.Pooling.Interfaces;

using NiveraAPI.Logs;
using NiveraAPI.Utilities;
using NiveraAPI.Extensions;

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

    /// <summary>
    /// Registers all serializers found in the given assembly. This method scans the types
    /// within the specified assembly, identifies serializer-related members based on custom
    /// attributes, and automatically registers them for use in serialization operations.
    /// </summary>
    /// <param name="assembly">
    /// The assembly to scan for serializers. This parameter must not be null, and it should
    /// contain types with members marked by serializer-specific attributes.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided assembly is null.
    /// </exception>
    public static void RegisterSerializers(Assembly assembly)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        foreach (var type in assembly.GetTypes())
        {
            try
            {
                foreach (var prop in type.GetAllProperties())
                {
                    if (!prop.HasAttribute<SerializerAttribute>(out _))
                        continue;

                    var getter = prop.GetGetMethod(true);

                    if (getter == null)
                    {
                        log.Warn($"Property &1{prop.Name}&r does not have a getter, skipping ..");
                        continue;
                    }

                    if (!getter.IsStatic)
                    {
                        log.Warn($"Property &1{prop.Name}&r is not static, skipping ..");
                        continue;                   
                    }

                    var delArgs = prop.PropertyType.GetGenericArguments();

                    if (delArgs.Length != 2)
                    {
                        log.Warn($"Property &1{prop.Name}&r is not a generic type, skipping ..");
                        continue;
                    }

                    if (delArgs[0] == typeof(ByteReader))
                    {
                        var deserializer = getter.Invoke(null, null);

                        if (deserializer != null)
                        {
                            var storedType = typeof(ByteSerializer<>).MakeGenericType(delArgs[1]);
                            var storedField = storedType.FindField("Deserializer");

                            if (storedField != null)
                            {
                                storedField.SetValue(null, deserializer);
                                
                                log.Debug($"Registered deserializer for property &1{prop.Name}&r: &3{delArgs[1]}&r");
                            }
                            else
                            {
                                log.Warn($"Failed to get deserializer field for &1{prop.Name}&r, skipping ..");
                            }
                        }
                        else
                        {
                            log.Warn($"Failed to get deserializer for property &1{prop.Name}&r, skipping ..");
                        }
                    }
                    else if (delArgs[0] == typeof(ByteWriter))
                    {
                        var serializer = getter.Invoke(null, null);

                        if (serializer != null)
                        {
                            var storedType = typeof(ByteSerializer<>).MakeGenericType(delArgs[1]);
                            var storedField = storedType.FindField("Serializer");

                            if (storedField != null)
                            {
                                storedField.SetValue(null, serializer);
                                
                                log.Debug($"Registered serializer for property &1{prop.Name}&r: &3{delArgs[1]}&r");
                            }
                            else
                            {
                                log.Warn($"Failed to get serializer field for &1{prop.Name}&r, skipping ..");
                            }
                        }
                        else
                        {
                            log.Warn($"Failed to get serializer for property &1{prop.Name}&r, skipping ..");
                        }
                    }
                    else
                    {
                        log.Warn($"Property &1{prop.Name}&r contains an invalid signature, skipping ..");
                    }
                }

                foreach (var field in type.GetAllFields())
                {
                    if (!field.HasAttribute<SerializerAttribute>(out _))
                        continue;

                    if (!field.IsStatic)
                    {
                        log.Warn($"Field &1{field.Name}&r is not static, skipping ..");
                        continue;
                    }

                    var delArgs = field.FieldType.GetGenericArguments();

                    if (delArgs.Length != 2)
                    {
                        log.Warn($"Field &1{field.Name}&r is not a generic type, skipping ..");
                        continue;
                    }

                    if (delArgs[0] == typeof(ByteReader))
                    {
                        var deserializer = field.GetValue(null);

                        if (deserializer != null)
                        {
                            var storedType = typeof(ByteSerializer<>).MakeGenericType(delArgs[1]);
                            var storedField = storedType.FindField("Deserializer");

                            if (storedField != null)
                            {
                                storedField.SetValue(null, deserializer);
                                
                                log.Debug($"Registered deserializer for field &1{field.Name}&r: &3{delArgs[1]}&r");
                            }
                            else
                            {
                                log.Warn($"Failed to get deserializer field for &1{field.Name}&r, skipping ..");
                            }
                        }
                        else
                        {
                            log.Warn($"Failed to get deserializer for field &1{field.Name}&r, skipping ..");
                        }
                    }
                    else if (delArgs[0] == typeof(ByteWriter))
                    {
                        var serializer = field.GetValue(null);

                        if (serializer != null)
                        {
                            var storedType = typeof(ByteSerializer<>).MakeGenericType(delArgs[1]);
                            var storedField = storedType.FindField("Serializer");

                            if (storedField != null)
                            {
                                storedField.SetValue(null, serializer);
                                
                                log.Debug($"Registered serializer for field &1{field.Name}&r: &3{delArgs[1]}&r");
                            }
                            else
                            {
                                log.Warn($"Failed to get serializer field for &1{field.Name}&r, skipping ..");
                            }
                        }
                        else
                        {
                            log.Warn($"Failed to get serializer for field &1{field.Name}&r, skipping ..");
                        }
                    }
                    else
                    {
                        log.Warn($"Field &1{field.Name}&r contains an invalid signature, skipping ..");
                    }
                }

                foreach (var method in type.GetAllMethods())
                {
                    if (!method.HasAttribute<SerializerAttribute>(out _))
                        continue;

                    if (method.ReturnType != typeof(void))
                    {
                        var args = method.GetAllParameters();

                        if (args.Length != 1 || args[0].ParameterType != typeof(ByteReader))
                        {
                            log.Warn($"Method &1{method.GetMemberName()}&r does not have a valid signature, skipping ..");
                            continue;
                        }

                        var deserializerType = typeof(Func<,>).MakeGenericType(typeof(ByteReader), method.ReturnType);
                        var deserializer = method.CreateDelegate(deserializerType);

                        if (deserializer != null)
                        {
                            var storedType = typeof(ByteSerializer<>).MakeGenericType(method.ReturnType);
                            var storedField = storedType.FindField("Deserializer");

                            if (storedField != null)
                            {
                                storedField.SetValue(null, deserializer);
                                
                                log.Debug($"Registered deserializer for method &1{method.GetMemberName()}&r");
                            }
                            else
                            {
                                log.Warn($"Failed to get deserializer field for &1{method.GetMemberName()}&r, skipping ..");
                            }
                        }
                        else
                        {
                            log.Warn($"Failed to get deserializer for method &1{method.GetMemberName()}&r, skipping ..");
                        }
                    }
                    else
                    {
                        var args = method.GetAllParameters();

                        if (args.Length != 2 || args[0].ParameterType != typeof(ByteWriter))
                        {
                            log.Warn($"Method &1{method.GetMemberName()}&r does not have a valid signature, skipping ..");
                            continue;
                        }

                        var serializerType = typeof(Action<,>).MakeGenericType(typeof(ByteWriter), args[1].ParameterType);
                        var serializer = method.CreateDelegate(serializerType);

                        if (serializer != null)
                        {
                            var storedType = typeof(ByteSerializer<>).MakeGenericType(args[1].ParameterType);
                            var storedField = storedType.FindField("Serializer");

                            if (storedField != null)
                            {
                                storedField.SetValue(null, serializer);
                                
                                log.Debug($"Registered serializer for method &1{method.GetMemberName()}&r");
                            }
                            else
                            {
                                log.Warn($"Failed to get serializer field for &1{method.GetMemberName()}&r, skipping ..");
                            }
                        }
                        else
                        {
                            log.Warn($"Failed to get serializer for method &1{method.GetMemberName()}&r, skipping ..");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to register serializers for type &1{type.FullName}&r:\n{ex}");
            }
        }   
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