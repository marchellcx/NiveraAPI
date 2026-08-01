using NiveraAPI.Logs;
using NiveraAPI.Extensions;

namespace NiveraAPI.IO.Configs;

/// <summary>
/// Manages the handling, serialization, and deserialization of configuration data
/// for an application. It provides mechanisms for registering, unregistering,
/// saving, and loading configuration values.
/// </summary>
public class ConfigHandler
{
    /// <summary>
    /// A delegate property used to deserialize a string representation into an object of the specified type.
    /// </summary>
    /// <param name="type">The target type into which the string representation will be deserialized. Must be a valid, non-null type.</param>
    /// <param name="txt">The string representation of the object to be deserialized. Must not be null or malformed according to the deserialization logic.</param>
    /// <returns>Returns an object of the specified type reconstructed from its string representation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown during loading if the property has not been set with a valid deserialization function.
    /// </exception>
    public delegate object Deserializer(Type type, string txt);

    /// <summary>
    /// A delegate property used to serialize an object of the specified type into its string representation.
    /// </summary>
    /// <param name="type">The type of the object being serialized. Must be a valid, non-null type.</param>
    /// <param name="obj">The object instance to serialize. Must not be null and must match the specified type.</param>
    /// <returns>Returns a string representation of the provided object, formatted as per the serialization logic.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown during serialization if the property has not been set with a valid serialization function.
    /// </exception>
    public delegate string Serializer(Type type, object obj);
    
    private readonly LogSink log = LogManager.GetSource("IO", "ConfigHandler");
    private readonly List<ConfigValue> values = new();

    /// <summary>
    /// A delegate property used to serialize an object of the specified type into a string representation.
    /// </summary>
    /// <remarks>
    /// This property defines the serialization logic to convert an object into its string representation.
    /// The default implementation utilizes the Newtonsoft.Json library to perform the serialization with indented formatting.
    /// If the delegate is not set, an exception will be thrown during the saving process.
    /// Ensure that the provided serializer is capable of handling the expected object types.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown during saving if the property has not been set with a valid serialization function.
    /// </exception>
    public Serializer Serialize { get; set; }
        = (type, obj) => FileHelper.YamlSerializer.Serialize(obj, type);

    /// <summary>
    /// A delegate property used to deserialize a string into an object of the specified type.
    /// </summary>
    /// <remarks>
    /// This property defines the deserialization logic to convert a string representation of data
    /// into an object of a given type. The default implementation uses the Newtonsoft.Json library
    /// to perform the deserialization. Users can override this behavior by assigning a custom
    /// deserialization method.
    /// Proper deserialization is crucial for the functionality of loading configuration values.
    /// If the delegate is not set, an exception will be thrown during configuration loading.
    /// Ensure that the provided deserializer is capable of handling the types expected in the configuration.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown during loading when the property has not been set with a valid deserialization function.
    /// </exception>
    public Deserializer Deserialize { get; set; } 
        = (type, txt) => FileHelper.YamlDeserializer.Deserialize(txt, type)!;

    /// <summary>
    /// The file path used to load and save configuration data.
    /// </summary>
    /// <remarks>
    /// This property specifies the path to the file where the configuration values are persisted.
    /// Setting the property ensures the internal logging system is updated with an appropriate
    /// name based on the file.
    /// An exception is thrown if the value assigned is null or empty.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an attempt is made to set the property to null or an empty string.
    /// </exception>
    public string FilePath
    {
        get => field;
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));

            log.Name = $"ConfigHandler/{Path.GetFileNameWithoutExtension(value)}";

            field = value;
        }
    }
    
    /// <summary>
    /// Whether debug logs are enabled.
    /// </summary>
    public bool DebugLogs { get; set; }

    /// <summary>
    /// The list of configuration values managed by the handler.
    /// </summary>
    public IReadOnlyList<ConfigValue> Values => values;

    /// <summary>
    /// Unregisters all configuration values associated with the static fields and properties
    /// of the specified type that were previously registered.
    /// </summary>
    /// <remarks>
    /// The method removes all configuration values from the internal collection which are
    /// associated with the specified type. Static fields and properties of the given type
    /// are identified by their declaring type. This operation does not affect other types
    /// or values not associated with the specified type.
    /// </remarks>
    /// <param name="type">The type whose associated configuration values should be unregistered. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="type"/> parameter is null.</exception>
    public void Unregister(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        values.RemoveAll(x => x.DeclaringType != null && x.DeclaringType == type);
    }
    
    /// <summary>
    /// Registers the static fields and properties of the specified type that are marked with the <see cref="ConfigAttribute"/>.
    /// </summary>
    /// <remarks>
    /// The method scans all static fields and properties of the given type to find members decorated with the <see cref="ConfigAttribute"/>.
    /// Valid configuration values found are added to the collection of managed configuration values, using the key and section specified
    /// in the attribute or the member's name if no key is provided. Members must be static and cannot be marked as init-only or missing
    /// required accessors (getter or setter). Duplicate configuration values for the same key and section are skipped, and warnings
    /// are logged for any members that fail validation criteria.
    /// </remarks>
    /// <param name="type">The type whose static fields and properties should be registered as configuration values. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="type"/> parameter is null.</exception>
    public void Register(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        var fields = type.GetAllFields();
        var properties = type.GetAllProperties();

        foreach (var field in fields)
        {
            if (!field.HasAttribute<ConfigAttribute>(out var cfg))
                continue;

            if (field.IsInitOnly)
            {
                log.Warn($"Field &1{field.Name}&r is marked as &6init-only&r, skipping ..");
                continue;
            }

            if (!field.IsStatic)
            {
                log.Warn($"Field &1{field.Name}&r is not &6static&r, skipping ..");
                continue;
            }
            
            var key = cfg.Key ?? field.Name;
            var value = values.Find(x =>
            {
                if (!string.IsNullOrWhiteSpace(cfg.Section)
                    && (string.IsNullOrEmpty(x.Section) || x.Section != cfg.Section))
                    return false;

                return x.Key == key;
            });

            if (value.IsValid)
            {
                log.Warn($"Duplicate config value found for key &1{key}&r (section: &6{cfg.Section ?? "null"}&r), skipping ..");
                continue;
            }
            
            values.Add(new(cfg.Section, key, cfg.Comment, field));
        }

        foreach (var property in properties)
        {
            if (!property.HasAttribute<ConfigAttribute>(out var cfg))
                continue;

            var getter = property.GetGetMethod(true);
            var setter = property.GetSetMethod(true);

            if (getter == null)
            {
                log.Warn($"Property &1{property.Name}&r does not have a getter, skipping ..");
                continue;
            }

            if (setter == null)
            {
                log.Warn($"Property &1{property.Name}&r does not have a setter, skipping ..");
                continue;
            }

            if (!setter.IsStatic || !getter.IsStatic)
            {
                log.Warn($"Property &1{property.Name}&r is not &6static&r, skipping ..");
                continue;
            }
            
            var key = cfg.Key ?? property.Name;
            var value = values.Find(x =>
            {
                if (!string.IsNullOrWhiteSpace(cfg.Section)
                    && (string.IsNullOrEmpty(x.Section) || x.Section != cfg.Section))
                    return false;

                return x.Key == key;
            });

            if (value.IsValid)
            {
                log.Warn($"Duplicate config value found for key &1{key}&r (section: &6{cfg.Section ?? "null"}&r), skipping ..");
                continue;
            }
            
            values.Add(new(cfg.Section, key, cfg.Comment, property));
        }
    }

    /// <summary>
    /// Saves the current configuration values to the file specified in the <see cref="FilePath"/> property.
    /// </summary>
    /// <remarks>
    /// The method serializes all valid configuration values using the configured <see cref="Serialize"/> method.
    /// If a configuration value is associated with a section, its key will be prefixed with the section name.
    /// The resulting serialized data is written to the file at the specified path.
    /// This method also logs warnings for invalid configuration values and skips their serialization, while
    /// errors during serialization of individual values or file operations are logged and do not interrupt the process.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the file path is not set or the serializer is not specified.
    /// </exception>
    public void Save()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
            throw new InvalidOperationException("No file path specified.");
        
        if (Serialize == null)
            throw new InvalidOperationException("No serializer specified.");
        
        try
        {
            var dict = new Dictionary<string, KeyValuePair<string?, string>>();

            foreach (var value in values)
            {
                try
                {
                    if (value.IsValid)
                    {
                        var obj = value.GetValue(null);
                        var txt = Serialize(value.ValueType, obj);

                        if (!string.IsNullOrEmpty(value.Section))
                        {
                            dict[$"{value.Section}.{value.Key}"] = new(value.Comment, txt);
                        }
                        else
                        {
                            dict[value.Key] = new(value.Comment, txt);
                        }
                    }
                    else
                    {
                        log.Warn($"Came across an invalid config value &1{value.Key}&r, skipping ..");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Error while serializing value &1{value.Key}&r:\n{ex}");
                }
            }

            var cfg = ConfigWriter.WriteConfigs(dict);

            File.WriteAllText(FilePath, cfg);
            
            dict.Clear();
        }
        catch (Exception ex)
        {
            log.Error($"Error while saving config file:\n{ex}");
        }
    }

    /// <summary>
    /// Loads the configuration from the file specified in the <see cref="FilePath"/> property.
    /// </summary>
    /// <remarks>
    /// If the file does not exist or is empty, it will create a new file by calling the <see cref="Save"/> method.
    /// If the file exists and contains valid configuration data, the method parses the contents and updates the
    /// configuration values using the provided deserializer.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the file path is not set or deserializer is not specified.
    /// </exception>
    public void Load()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
            throw new InvalidOperationException("No file path specified.");
        
        if (Deserialize == null)
            throw new InvalidOperationException("No deserializer specified.");       

        log.DebugIf("Loading config file ..", DebugLogs);
        
        if (!File.Exists(FilePath))
        {
            log.DebugIf("File does not exist, saving ..", DebugLogs);
            
            Save();
        }
        else
        {
            var lines = File.ReadAllLines(FilePath);

            if (lines.Count(x => !string.IsNullOrWhiteSpace(x)) < 1)
            {
                log.DebugIf("File is empty, saving ..", DebugLogs);
                
                Save();
            }
            else
            {
                var configs = ConfigReader.ReadConfigs(lines);
                
                log.DebugIf($"Loaded &1{configs.Count}&r config keys, setting values ..", DebugLogs);

                foreach (var kvp in configs)
                {
                    try
                    {
                        log.DebugIf($"Processing config value &1{kvp.Key}&r", DebugLogs);
                        
                        string key = string.Empty;
                        string? section = null;

                        if (kvp.Key.TrySplit('.', true, 2, out var splits))
                        {
                            section = splits[0];
                            key = splits[1];
                        }
                        else
                        {
                            key = kvp.Key;
                        }

                        var value = values.Find(x =>
                        {
                            if (!string.IsNullOrWhiteSpace(section)
                                && (string.IsNullOrEmpty(x.Section) || x.Section != section))
                                return false;

                            return x.Key == key;
                        });

                        if (!value.IsValid)
                        {
                            log.Warn($"Could not find config value for key &1{key}&r (section: &6{section ?? "null"}&r), skipping ..");
                            continue;
                        }
                        
                        var obj = Deserialize(value.ValueType!, kvp.Value);

                        value.SetValue(null, obj);
                        
                        log.DebugIf($"Set &1{key}&r (&6{value.Member}&r)", DebugLogs);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Error while processing config value &1{kvp.Key}&r:\n{ex}");
                    }
                }
                
                configs.Clear();
            }
        }
    }
}