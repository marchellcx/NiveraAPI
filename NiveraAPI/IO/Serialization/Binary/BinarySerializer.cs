using HarmonyLib;

using NiveraAPI.Logs;
using NiveraAPI.Extensions;

namespace NiveraAPI.IO.Serialization.Binary;

/// <summary>
/// Provides functionality for serializing and deserializing objects in binary format.
/// </summary>
/// <remarks>
/// This static class manages serialization metadata and handles conversion between objects
/// and their binary representations. It supports both simple and complex types.
/// </remarks>
public static class BinarySerializer
{
    private static LogSink log = LogManager.GetSource("IO", "BinarySerializer");
    
    private static Dictionary<Type, BinaryClassInfo> classes = new();
    
    private static Dictionary<Type, Delegate> serializers = new();
    private static Dictionary<Type, Delegate> deserializers = new();
    
    /// <summary>
    /// Cached classes.
    /// </summary>
    public static IReadOnlyDictionary<Type, BinaryClassInfo> Classes => classes;

    /// <summary>
    /// Refreshes the internal metadata for the specified type.
    /// </summary>
    /// <param name="type">The type to refresh metadata for. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is null.</exception>
    public static void Refresh(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        
        serializers.Clear();
        deserializers.Clear();

        classes[type] = ScanType(type);
    }

    /// <summary>
    /// Serializes the given object into binary format using the specified writer.
    /// </summary>
    /// <param name="obj">The object to serialize. Must not be null.</param>
    /// <param name="writer">The writer to which the binary data will be written. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> or <paramref name="writer"/> is null.</exception>
    public static void Serialize(object obj, ByteWriter writer)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        
        var type = obj.GetType();
        
        if (!classes.TryGetValue(type, out var classInfo))
            classes[type] = classInfo = ScanType(type);

        if (classInfo.IsSimple)
        {
            classInfo.Serializer.Key(classInfo.Serializer.Value, writer, obj);
            return;
        }
        
        SerializeFields(obj, classInfo, writer);
        SerializeProperties(obj, classInfo, writer);
    }
    
    /// <summary>
    /// Deserializes data into an object of the specified type using binary serialization.
    /// </summary>
    /// <param name="type">The type of the object to deserialize.</param>
    /// <param name="data">The binary data to deserialize from.</param>
    /// <returns>The deserialized object of the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> or <paramref name="data"/> is null.</exception>
    /// <exception cref="Exception">Thrown if the instance of the specified type cannot be created.</exception>
    public static object Deserialize(Type type, ByteReader data)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (data == null)
            throw new ArgumentNullException(nameof(data));
        
        if (!classes.TryGetValue(type, out var classInfo))
            classes[type] = classInfo = ScanType(type);

        if (classInfo.IsSimple)
            return classInfo.Deserializer.Key(classInfo.Deserializer.Value, data);

        var obj = classInfo.Constructor();
        
        if (obj == null)
            throw new Exception($"Could not create instance of {type}");
        
        DeserializeFields(obj, classInfo, data);
        DeserializeProperties(obj, classInfo, data);
        
        return obj;
    }

    private static void SerializeFields(object obj, BinaryClassInfo info, ByteWriter writer)
    {
        for (var x = 0; x < info.Fields.Count; x++)
        {
            var field = info.Fields[x];
            var value = field.Reference(obj);
            
            field.Serializer.Key(field.Serializer.Value, writer, value);
        }
    }

    private static void SerializeProperties(object obj, BinaryClassInfo info, ByteWriter writer)
    {
        for (var x = 0; x < info.Properties.Count; x++)
        {
            var property = info.Properties[x];
            var value = property.Getter(obj);
            
            property.Serializer.Key(property.Serializer.Value, writer, value);
        }
    }

    private static void DeserializeFields(object obj, BinaryClassInfo info, ByteReader data)
    {
        for (var x = 0; x < info.Fields.Count; x++)
        {
            var field = info.Fields[x];
            var value = field.Deserializer.Key(field.Deserializer.Value, data);

            field.Reference(obj) = value;
        }
    }

    private static void DeserializeProperties(object obj, BinaryClassInfo info, ByteReader data)
    {
        for (var x = 0; x < info.Properties.Count; x++)
        {
            var property = info.Properties[x];
            var value = property.Deserializer.Key(property.Deserializer.Value, data);

            property.Setter(obj, value);
        }
    }

    private static BinaryClassInfo ScanType(Type type)
    {
        var gSerializerDelegate = GetSerializer(type);
        var gDeserializeDelegate = GetDeserializer(type);

        if (gSerializerDelegate != null && gDeserializeDelegate != null)
        {
            var info = new BinaryClassInfo();

            info.Type = type;
            info.IsSimple = true;
            
            var gSerializer = MethodInvoker.GetHandler(gSerializerDelegate.Method);
            var gDeserializer = MethodInvoker.GetHandler(gDeserializeDelegate.Method);
            
            info.Serializer = new(gSerializer, gSerializerDelegate.Target);
            info.Deserializer = new(gDeserializer, gDeserializeDelegate.Target);
            
            return info;
        }
        else
        {
            var info = new BinaryClassInfo();

            info.Type = type;

            var fields = type.GetAllFields();
            var properties = type.GetAllProperties();

            for (var x = 0; x < fields.Length; x++)
            {
                var field = fields[x];

                if (!field.HasAttribute<BinaryFieldAttribute>(out _))
                    continue;

                if (field.IsInitOnly)
                {
                    log.Warn($"Field &1{field.GetMemberName()}&r is marked as serializable, but it's read-only!");
                    continue;
                }

                if (field.IsStatic)
                {
                    log.Warn($"Field &1{field.GetMemberName()}&r is marked as serializable, but it's static!");
                    continue;
                }

                var serializerDelegate = GetSerializer(field.FieldType);
                var deserializerDelegate = GetDeserializer(field.FieldType);

                if (serializerDelegate == null)
                {
                    log.Warn($"Field &1{field.GetMemberName()}&r has no serializer!");
                    continue;
                }

                if (deserializerDelegate == null)
                {
                    log.Warn($"Field &1{field.GetMemberName()}&r has no deserializer!");
                    continue;
                }

                var serializer = MethodInvoker.GetHandler(serializerDelegate.Method);
                var deserializer = MethodInvoker.GetHandler(deserializerDelegate.Method);

                var binaryField = new BinaryFieldInfo();

                binaryField.Field = field;

                binaryField.Serializer = new(serializer, serializerDelegate.Target);
                binaryField.Deserializer = new(deserializer, deserializerDelegate.Target);

                binaryField.Reference = AccessTools.FieldRefAccess<object, object>(field);

                info.Fields.Add(binaryField);
            }

            for (var x = 0; x < properties.Length; x++)
            {
                var property = properties[x];

                if (!property.HasAttribute<BinaryFieldAttribute>(out _))
                    continue;

                var getMethod = property.GetGetMethod(true);
                var setMethod = property.GetSetMethod(true);

                if (getMethod == null)
                {
                    log.Warn($"Property &1{property.GetMemberName()}&r has no getter!");
                    continue;
                }

                if (setMethod == null)
                {
                    log.Warn($"Property &1{property.GetMemberName()}&r has no setter!");
                    continue;
                }

                if (getMethod.IsStatic || setMethod.IsStatic)
                {
                    log.Warn($"Property &1{property.GetMemberName()}&r is marked as serializable, but it's static!");
                    continue;
                }

                var serializerDelegate = GetSerializer(property.PropertyType);
                var deserializerDelegate = GetDeserializer(property.PropertyType);

                if (serializerDelegate == null)
                {
                    log.Warn($"Property &1{property.GetMemberName()}&r has no serializer!");
                    continue;
                }

                if (deserializerDelegate == null)
                {
                    log.Warn($"Property &1{property.GetMemberName()}&r has no deserializer!");
                    continue;
                }

                var getter = MethodInvoker.GetHandler(getMethod);
                var setter = MethodInvoker.GetHandler(setMethod);

                var serializer = MethodInvoker.GetHandler(serializerDelegate.Method);
                var deserializer = MethodInvoker.GetHandler(deserializerDelegate.Method);

                var binaryProperty = new BinaryPropertyInfo();

                binaryProperty.Property = property;

                binaryProperty.Setter = setter;
                binaryProperty.Getter = getter;

                binaryProperty.Serializer = new(serializer, serializerDelegate.Target);
                binaryProperty.Deserializer = new(deserializer, deserializerDelegate.Target);

                info.Properties.Add(binaryProperty);
            }

            var orderedFields = info.Fields
                .OrderBy(x => x.Field.GetMemberName())
                .ToList();

            var orderedProperties = info.Properties
                .OrderBy(x => x.Property.GetMemberName())
                .ToList();

            info.Fields.Clear();
            info.Properties.Clear();

            info.Fields.AddRange(orderedFields);
            info.Properties.AddRange(orderedProperties);

            return info;
        }
    }

    private static Delegate? GetDeserializer(Type type)
    {
        if (deserializers.TryGetValue(type, out var deserializer))
            return deserializer;
        
        var generic = typeof(ByteSerializer<>).MakeGenericType(type);
        var field = generic.FindField("Deserialize");
        
        return deserializers[type] = field?.GetValue(null) as Delegate;
    }

    private static Delegate? GetSerializer(Type type)
    {
        if (serializers.TryGetValue(type, out var serializer))
            return serializer;
        
        var generic = typeof(ByteSerializer<>).MakeGenericType(type);
        var field = generic.FindField("Serialize");
        
        return serializers[type] = field?.GetValue(null) as Delegate;
    }
}