using System.Reflection;
using System.Collections.Concurrent;

using LabApi.Features.Wrappers;

using LabApi.Loader;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;

using LabExtended.Core;
using LabExtended.Utilities.Update;

using NiveraAPI.Extensions;

using NiveraAPI.IO.Configs;

using NiveraAPI.Logs;
using NiveraAPI.Utilities;

using Serialization;

using YamlDotNet.Serialization;

namespace NiveraAPI.ScpSl;

/// <summary>
/// Represents a plugin used to initialize the NiveraAPI library when running in an SCP:SL server.
/// Inherits from the Plugin base class to integrate with the server and provides essential setup functionality.
/// </summary>
public class Loader : Plugin<Config>
{
    private static volatile object serializer;
    private static volatile object deserializer;

    private static volatile Func<object, string>? serialize;
    private static volatile Func<string, Type, object>? deserialize;
    
    private static volatile bool overrideStrings;
    private static volatile ConcurrentDictionary<Assembly, ConfigHandler> configs = new();
    
    /// <summary>
    /// The name of the plugin.
    /// </summary>
    public override string Name { get; } = "NiveraAPI.ScpSl";
    
    /// <summary>
    /// The author of the plugin.
    /// </summary>
    public override string Author { get; } = "marchellcx";

    /// <summary>
    /// The description of the plugin.
    /// </summary>
    public override string Description { get; } = "A plugin used to initialize the NiveraAPI library when running in a SCP:SL server.";

    /// <summary>
    /// The required LabAPI version.
    /// </summary>
    public override Version RequiredApiVersion { get; } = null!;

    /// <summary>
    /// The priority of the plugin.
    /// </summary>
    public override LoadPriority Priority { get; } = LoadPriority.Highest;

    /// <summary>
    /// Initializes and enables the NiveraAPI library when running in an SCP:SL server.
    /// Registers necessary event handlers, sets up logging, and begins library updates.
    /// </summary>
    public override void Enable()
    {
        try
        {
            overrideStrings = StartupArgs.Args.Any(x =>
                string.Equals(x, "OverrideYamlStrings", StringComparison.OrdinalIgnoreCase));

            LogManager.Log += Log;
            LogManager.UseQueue = false;

            try
            {
                LibraryLoader.Initialize();
            }
            catch (Exception ex)
            {
                ApiLog.Error($"Failed to initialize library:\n{ex}");
            }

            LogManager.UseQueue = false;

            PlayerUpdateHelper.OnLateUpdate += LibraryUpdate.Invoke;

            try
            {
                // genuinely what the fuck is happening

                var domain = AppDomain.CurrentDomain;

                if (domain == null)
                {
                    ApiLog.Error("Failed to get current domain");
                    return;
                }

                var assemblies = domain.GetAssemblies();

                if (assemblies == null)
                {
                    ApiLog.Error("Failed to get assemblies");
                    return;
                }

                if (assemblies.Length < 1)
                {
                    ApiLog.Error("No assemblies found");
                    return;
                }

                var assemblyCSharp = default(Assembly);
                var labApiAssembly = default(Assembly);

                foreach (var assembly in assemblies)
                {
                    if (assembly.FullName.StartsWith("Assembly-CSharp") &&
                        !assembly.FullName.StartsWith("Assembly-CSharp-firstpass"))
                    {
                        assemblyCSharp = assembly;

                        ApiLog.Debug($"Loaded Assembly-CSharp assembly: &1{assemblyCSharp.FullName}&r");
                    }

                    if (assembly.FullName.StartsWith("LabApi"))
                    {
                        labApiAssembly = assembly;

                        ApiLog.Debug($"Loaded LabApi assembly: &1{labApiAssembly.FullName}&r");
                    }
                }

                if (Config.UseLabApiYaml)
                {
                    if (labApiAssembly == null)
                    {
                        ApiLog.Error("LabApi assembly not found");
                        return;
                    }

                    var type = labApiAssembly.GetType("LabApi.Loader.Features.Yaml.YamlConfigParser");

                    if (type == null)
                    {
                        ApiLog.Error("YamlConfigParser type not found");
                        return;
                    }

                    var serializerProp = type.GetProperty("Serializer");
                    var deserializerProp = type.GetProperty("Deserializer");

                    if (serializerProp == null)
                    {
                        ApiLog.Error("Serializer property not found");
                        return;
                    }

                    if (deserializerProp == null)
                    {
                        ApiLog.Error("Deserializer property not found");
                        return;
                    }

                    serializer = serializerProp.GetValue(null);
                    deserializer = deserializerProp.GetValue(null);
                }
                else
                {
                    if (assemblyCSharp == null)
                    {
                        ApiLog.Error("Assembly-CSharp assembly not found");
                        return;
                    }

                    var type = assemblyCSharp.GetType("Serialization.YamlParser");

                    if (type == null)
                    {
                        ApiLog.Error("YamlParser type not found");
                        return;
                    }

                    var serializerProp = type.GetProperty("Serializer");
                    var deserializerProp = type.GetProperty("Deserializer");

                    if (serializerProp == null)
                    {
                        ApiLog.Error("Serializer property not found");
                        return;
                    }

                    if (deserializerProp == null)
                    {
                        ApiLog.Error("Deserializer property not found");
                        return;
                    }

                    serializer = serializerProp.GetValue(null);
                    deserializer = deserializerProp.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                ApiLog.Error($"Failed to load YAML parsers:\n{ex}");
            }

            if (serializer == null)
            {
                ApiLog.Error("Failed to initialize YAML serializer");
                return;
            }

            if (deserializer == null)
            {
                ApiLog.Error("Failed to initialize YAML deserializer");
                return;
            }

            var serializeMethod = serializer.GetType().FindMethod(m =>
            {
                if (m.Name != "Serialize")
                    return false;

                var param = m.GetAllParameters();

                if (param.Length != 1)
                    return false;

                return param[0].ParameterType == typeof(object);
            });

            var deserializeMethod = deserializer.GetType().FindMethod(m =>
            {
                if (m.Name != "Deserialize")
                    return false;

                var param = m.GetAllParameters();

                if (param.Length != 2)
                    return false;

                return param[0].ParameterType == typeof(string) && param[1].ParameterType == typeof(Type);
            });

            if (serializeMethod == null)
            {
                ApiLog.Error("Failed to find YAML serializer method");
                return;
            }

            if (deserializeMethod == null)
            {
                ApiLog.Error("Failed to find YAML deserializer method");
                return;
            }

            serialize = serializeMethod.CreateDelegate(typeof(Func<object, string>), serializer) as Func<object, string>;
            deserialize = deserializeMethod.CreateDelegate(typeof(Func<string, Type, object>), deserializer) as Func<string, Type, object>;

            if (serialize == null)
            {
                ApiLog.Error("Failed to create YAML serializer delegate");
                return;
            }

            if (deserialize == null)
            {
                ApiLog.Error("Failed to create YAML deserializer delegate");
                return;
            }

            RegisterConfigs();
            ReloadConfigs();
        }
        catch (Exception ex)
        {
            ApiLog.Error(ex);
        }
    }

    /// <summary>
    /// Stops the library and unregisters event handlers.
    /// </summary>
    public override void Disable()
    {
        try
        {
            LogManager.Log -= Log;
            
            PlayerUpdateHelper.OnLateUpdate -= LibraryUpdate.Invoke;
        }
        catch (Exception ex)
        {
            ApiLog.Error($"Failed to stop library:\n{ex}");
        }
    }

    /// <summary>
    /// Loads the configuration for the calling assembly.
    /// Retrieves the appropriate configuration handler based on the calling assembly and executes the load operation.
    /// Logs an error if the configuration loading process encounters an issue.
    /// </summary>
    /// <exception cref="Exception">
    /// Thrown when the calling assembly cannot be determined or its corresponding configuration handler cannot be retrieved.
    /// </exception>
    public static void LoadConfig()
    {
        var assembly = Assembly.GetCallingAssembly();
        
        if (assembly == null)
            throw new Exception("Failed to get calling assembly");
        
        if (!configs.TryGetValue(assembly, out var config))
            throw new Exception("Failed to get config handler");

        try
        {
            config.Load();
        }
        catch (Exception ex)
        {
            ApiLog.Error($"Failed to load configs for &1{assembly.GetSimpleName()}&r:\n{ex}");
        }
    }

    /// <summary>
    /// Saves the configuration data for the calling assembly.
    /// Retrieves the configuration handler associated with the calling assembly and invokes the save operation.
    /// Logs an error if the configuration save process fails or if the calling assembly or handler cannot be located.
    /// </summary>
    /// <exception cref="Exception">
    /// Thrown when the calling assembly cannot be retrieved or when a configuration handler associated with the calling assembly is not found.
    /// </exception>
    public new static void SaveConfig()
    {
        var assembly = Assembly.GetCallingAssembly();
        
        if (assembly == null)
            throw new Exception("Failed to get calling assembly");
        
        if (!configs.TryGetValue(assembly, out var config))
            throw new Exception("Failed to get config handler");

        try
        {
            config.Save();
        }
        catch (Exception ex)
        {
            ApiLog.Error($"Failed to save configs for &1{assembly.GetSimpleName()}&r:\n{ex}");
        }
    }

    /// <summary>
    /// Reloads the configuration for the calling assembly by invoking the respective configuration handler.
    /// Attempts to load and save the associated configuration. Exceptions are logged on failure.
    /// </summary>
    /// <exception cref="Exception">
    /// Thrown when the calling assembly cannot be determined, or if a configuration handler is not found
    /// for the calling assembly.
    /// </exception>
    public static void ReloadConfig()
    {
        var assembly = Assembly.GetCallingAssembly();
        
        if (assembly == null)
            throw new Exception("Failed to get calling assembly");
        
        if (!configs.TryGetValue(assembly, out var config))
            throw new Exception("Failed to get config handler");

        try
        {
            config.Load();
            config.Save();
        }
        catch (Exception ex)
        {
            ApiLog.Error($"Failed to reload configs for &1{assembly.GetSimpleName()}&r:\n{ex}");
        }
    }

    private void RegisterConfigs()
    {
        foreach (var kvp in PluginLoader.Plugins)
        {
            var types = GetConfigTypes(kvp.Value);

            if (types.Count < 1)
            {
                types.ReturnList();
                continue;
            }

            var path = GetConfigPath(kvp.Value.GetSimpleName());
            var handler = new ConfigHandler();

            handler.FilePath = path;
            
            handler.Serialize = (type, obj) =>
            {
                try
                {
                    return serialize(obj);
                }
                catch (Exception ex)
                {
                    ApiLog.Error($"Failed while serializing &3{type.Name}&r for plugin &1{kvp.Key.Name}&r:\n{ex}");
                    return string.Empty;
                }
            };
        
            handler.Deserialize = (type, str) =>
            {
                try
                {
                    if (overrideStrings && type == typeof(string))
                    {
                        if (string.IsNullOrEmpty(str))
                            return str;

                        if (str[0] == '\'' && str[str.Length - 1] == '\'')
                            return str.Substring(1, str.Length - 2);

                        return str;
                    }

                    return deserialize(str, type);
                }
                catch (Exception ex)
                {
                    ApiLog.Error($"Failed while deserializing &1{type.Name}&r in plugin &1{kvp.Key.Name}&r:\n{ex}");

                    if (type == typeof(string))
                        return string.Empty;
                    
                    return null!;
                }
            };

            for (var x = 0; x < types.Count; x++)
            {
                try
                {
                    handler.Register(types[x]);
                }
                catch (Exception ex)
                {
                    ApiLog.Error($"Error while registering type &1{types[x].FullName}&r:\n{ex}");
                }
            }

            configs.TryAdd(kvp.Value, handler);
        }
    }

    private void ReloadConfigs()
    {
        foreach (var kvp in configs)
        {
            try
            {
                kvp.Value.Load();
                kvp.Value.Save();
            }
            catch (Exception ex)
            {
                ApiLog.Error($"Error while reloading config file &1{kvp.Key.GetSimpleName()}&r:\n{ex}");
            }
        }
    }

    private string GetConfigPath(string assembly)
    {
        if (Config.ConfigPaths.TryGetValue(assembly, out var path))
            return path;
        
        if (Config.GlobalConfigs.Contains(assembly))
            return Path.Combine(PathManager.Configs.FullName, $"{assembly}_global.ini");
        
        return Path.Combine(PathManager.Configs.FullName, $"{assembly}_{Server.Port}.ini");
    }

    private List<Type> GetConfigTypes(Assembly assembly)
    {
        var list = Pools.PoolList<Type>();

        try
        {
            foreach (var type in assembly.GetTypes())
            {
                try
                {
                    var fields = type.GetAllFields();
                    var properties = type.GetAllProperties();

                    if (fields.Any(f => f.GetCustomAttribute<ConfigAttribute>() != null)
                        || properties.Any(p => p.GetCustomAttribute<ConfigAttribute>() != null))
                    {
                        list.Add(type);
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
        catch
        {
            // ignored
        }

        return list;
    }
    
    private void Log(LogMessage msg)
    {
        if (!Config.EnableLogs)
            return;

        var source = new string(msg.SourceText.Substring(5).Take(msg.SourceText.Length - 8).ToArray());
        
        switch (msg.Level)
        {
            case LogLevel.Debug or LogLevel.Verbose:
            {
                if (!Config.EnableDebugLogs)
                    return;
                
                ApiLog.Info(source, msg.MessageText);
                break;
            }

            case LogLevel.Error or LogLevel.Fatal:
                ApiLog.Error(source, msg.MessageText);
                break;
            
            case LogLevel.Warning:
                ApiLog.Warn(source, msg.MessageText);
                break;
            
            case LogLevel.Information:
                ApiLog.Info(source, msg.MessageText);
                break;
        }
    }
}