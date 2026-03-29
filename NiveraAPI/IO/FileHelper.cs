using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using NiveraAPI.Logs;
using NiveraAPI.Extensions;

using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO
{
    /// <summary>
    /// Utility methods related to file I/O operations.
    /// </summary>
    public static class FileUtils
    {
        private static LogSink log;
        
        private static JsonSerializerSettings indentedSettings;
        private static JsonSerializerSettings nonIndentedSettings;

        static FileUtils()
        {
            log = LogManager.GetSource("IO", "FileHelper");
            
            indentedSettings = new() { Formatting = Formatting.Indented };
            
            if (indentedSettings.Converters.TryGetFirst(x => x is StringEnumConverter, out var enumConverter))
                indentedSettings.Converters.Remove(enumConverter);

            indentedSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy(), true));

            nonIndentedSettings = new() { Formatting = Formatting.None };

            if (nonIndentedSettings.Converters.TryGetFirst(x => x is StringEnumConverter, out enumConverter))
                nonIndentedSettings.Converters.Remove(enumConverter);

            nonIndentedSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy(), true));
        }

        /// <summary>
        /// Creates a directory path by combining the specified path segments, ensuring that all intermediate
        /// directories exist.
        /// </summary>
        /// <remarks>If any intermediate directories in the specified path do not exist, they are created.
        /// Path segments that include file extensions are treated as files and do not result in directory creation for
        /// that segment. This method is useful for preparing a file path and ensuring the directory structure exists
        /// before file operations.</remarks>
        /// <param name="parts">An array of path segments to combine. Each segment represents a part of the final path. Cannot be null or
        /// empty.</param>
        /// <returns>The absolute path created by combining the specified segments. The returned path is fully qualified.</returns>
        public static string CreatePath(params string[] parts)
        {
            for (var x = 0; x < parts.Length; x++)
            {
                var part = parts[x];
                var extension = Path.GetExtension(part);

                if (!string.IsNullOrEmpty(extension) && (x + 1 >= parts.Length || !string.IsNullOrEmpty(Path.GetExtension(parts[x + 1]))))
                    continue;

                var currentPath = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Take(x + 1));

                if (!Directory.Exists(currentPath))
                    Directory.CreateDirectory(currentPath);
            }

            return Path.GetFullPath(Path.Combine(parts));
        }

        /// <summary>
        /// Creates all directories and subdirectories in the specified file path that do not already exist.
        /// </summary>
        /// <remarks>This method does not create the file itself; it only ensures that all directories in
        /// the specified path exist. If the directories already exist, no action is taken. The returned path is the
        /// absolute path corresponding to the input.</remarks>
        /// <param name="filePath">The full or relative path of the file for which to ensure all parent directories exist. Cannot be null or
        /// empty.</param>
        /// <returns>A fully qualified file path with all necessary directories created.</returns>
        public static string CreatePath(string filePath)
        {
            filePath = Path.GetFullPath(filePath);

            var parts = filePath.Split(Path.DirectorySeparatorChar);

            for (var x = 0; x < parts.Length; x++)
            {
                var part = parts[x];
                var extension = Path.GetExtension(part);

                if (!string.IsNullOrEmpty(extension) && (x + 1 >= parts.Length || !string.IsNullOrEmpty(Path.GetExtension(parts[x + 1]))))
                    continue;

                var currentPath = string.Join(Path.DirectorySeparatorChar.ToString(), parts.Take(x + 1));

                if (!Directory.Exists(currentPath))
                    Directory.CreateDirectory(currentPath);
            }

            return filePath;
        }

        /// <summary>
        /// Attempts to save a binary file to the specified directory using the provided writer delegate.
        /// </summary>
        /// <remarks>This method combines the specified directory and file name to determine the file
        /// path. If either the directory or name is null or empty, the method returns false without attempting to save
        /// the file.</remarks>
        /// <param name="directory">The path to the directory where the file will be saved. Cannot be null or empty.</param>
        /// <param name="name">The name of the file to create within the specified directory. Cannot be null or empty.</param>
        /// <param name="writer">A delegate that writes the binary content to the file using a NetworkWriter instance.</param>
        /// <returns>true if the file was successfully saved; otherwise, false.</returns>
        public static bool TrySaveBinaryFile(string directory, string name, Action<ByteWriter> writer)
        {
            if (string.IsNullOrEmpty(directory))
                return false;

            if (string.IsNullOrEmpty(name))
                return false;

            var filePath = Path.Combine(directory, name);
            return TrySaveBinaryFile(filePath, writer);
        }

        /// <summary>
        /// Attempts to save binary data to the specified file using a provided writer delegate.
        /// </summary>
        /// <remarks>If an exception occurs during writing or saving, the method logs the error and
        /// returns false. The file will be overwritten if it already exists.</remarks>
        /// <param name="filePath">The path of the file to which the binary data will be saved. Cannot be null or empty.</param>
        /// <param name="writer">A delegate that writes binary data to a provided NetworkWriter instance. Cannot be null.</param>
        /// <returns>true if the file was saved successfully; otherwise, false.</returns>
        public static bool TrySaveBinaryFile(string filePath, Action<ByteWriter> writer)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            if (writer == null)
                return false;

            try
            {
                using (var pooled = ByteWriter.Get())
                {
                    writer(pooled);

                    var data = pooled.ToArray();

                    File.WriteAllBytes(filePath, data);
                }

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Caught an exception while attempting to save binary file &3{Path.GetFileName(filePath)}&r:\n{ex}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to load a binary file from the specified directory and file name, invoking the provided reader
        /// action if the file is found.
        /// </summary>
        /// <remarks>This method combines the specified directory and file name to locate the binary file.
        /// If either parameter is null or empty, the method returns false without attempting to load the
        /// file.</remarks>
        /// <param name="directory">The path to the directory containing the binary file. Cannot be null or empty.</param>
        /// <param name="name">The name of the binary file to load. Cannot be null or empty.</param>
        /// <param name="reader">An action to execute with a NetworkReader for the loaded file. The action is invoked only if the file is
        /// successfully found and loaded.</param>
        /// <returns>true if the binary file is found and loaded successfully; otherwise, false.</returns>
        public static bool TryLoadBinaryFile(string directory, string name, Action<ByteReader> reader)
        {
            if (string.IsNullOrEmpty(directory))
                return false;

            if (string.IsNullOrEmpty(name))
                return false;

            var filePath = Path.Combine(directory, name);
            return TryLoadBinaryFile(filePath, reader);
        }

        /// <summary>
        /// Attempts to load a binary file from the specified path and invokes the provided reader action with a
        /// NetworkReader for the file's contents.
        /// </summary>
        /// <remarks>This method does not throw exceptions for file access errors or invalid input;
        /// instead, it returns false if loading fails. The reader action is only called if the file exists and contains
        /// data.</remarks>
        /// <param name="filePath">The path to the binary file to load. Must refer to an existing, non-empty file.</param>
        /// <param name="reader">An action to execute with a NetworkReader containing the file's data. Cannot be null.</param>
        /// <returns>true if the file was successfully loaded and the reader action was invoked; otherwise, false.</returns>
        public static bool TryLoadBinaryFile(string filePath, Action<ByteReader> reader)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            if (reader == null)
                return false;

            try
            {
                if (!File.Exists(filePath))
                    return false;

                var data = File.ReadAllBytes(filePath);

                if (data.Length == 0)
                    return false;

                using (var pooled = ByteReader.Get(data, 0, data.Length))
                    reader(pooled);

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Caught an exception while loading binary file &3{Path.GetFileName(filePath)}&r:\n{ex}");
                return false;
            }
        }

        /// <summary>
        /// Loads a JSON file from the specified directory and file name, deserializing its contents into an object of
        /// type <typeparamref name="T"/>. If the file does not exist or cannot be loaded, returns the specified default
        /// value.
        /// </summary>
        /// <remarks>If either <paramref name="directory"/> or <paramref name="name"/> is null or empty,
        /// the method returns <paramref name="defaultValue"/> without attempting to load or save any file. The method
        /// can optionally save the default value to the file if it does not exist, and can move the file aside if an
        /// error occurs during loading, based on the provided parameters.</remarks>
        /// <typeparam name="T">The type of object to deserialize from the JSON file.</typeparam>
        /// <param name="directory">The path to the directory containing the JSON file. Cannot be null or empty.</param>
        /// <param name="name">The name of the JSON file to load. Cannot be null or empty.</param>
        /// <param name="defaultValue">The value to return if the file does not exist or cannot be loaded.</param>
        /// <param name="saveIfNotExists">true to save the default value to the file if it does not exist; otherwise, false.</param>
        /// <param name="saveIndented">true to format the saved JSON with indentation for readability; otherwise, false.</param>
        /// <param name="moveIfError">true to move the file aside if an error occurs during loading; otherwise, false.</param>
        /// <returns>An object of type <typeparamref name="T"/> deserialized from the JSON file, or the specified default value
        /// if the file is missing or cannot be loaded.</returns>
        public static T LoadJsonFileOrDefault<T>(string directory, string name, T defaultValue, bool saveIfNotExists = true, bool saveIndented = true, 
            bool moveIfError = true)
        {
            if (string.IsNullOrEmpty(directory))
                return defaultValue;

            if (string.IsNullOrEmpty(name))
                return defaultValue;

            var filePath = Path.Combine(directory, name);
            return LoadJsonFileOrDefault(filePath, defaultValue, saveIfNotExists, saveIndented, moveIfError);
        }

        /// <summary>
        /// Loads an object of type <typeparamref name="T"/> from a JSON file at the specified path, or returns a
        /// default value if the file does not exist or cannot be loaded.
        /// </summary>
        /// <remarks>If the file cannot be loaded due to an error and <paramref name="moveIfError"/> is
        /// <see langword="true"/>, the original file is renamed with a timestamp and the default value is saved in its
        /// place. This method does not throw exceptions for missing or invalid files; it always returns a valid
        /// result.</remarks>
        /// <typeparam name="T">The type of object to load from the JSON file.</typeparam>
        /// <param name="filePath">The path to the JSON file to load. Cannot be null or empty.</param>
        /// <param name="defaultValue">The value to return if the file does not exist or cannot be loaded.</param>
        /// <param name="saveIfNotExists">If <see langword="true"/>, saves the <paramref name="defaultValue"/> to the specified file if it does not
        /// exist.</param>
        /// <param name="saveIndented">If <see langword="true"/>, formats the saved JSON with indentation for readability.</param>
        /// <param name="moveIfError">If <see langword="true"/>, moves the file to a backup location and saves the <paramref name="defaultValue"/>
        /// if loading fails due to an error.</param>
        /// <returns>The object loaded from the JSON file, or <paramref name="defaultValue"/> if the file does not exist or
        /// cannot be loaded.</returns>
        public static T LoadJsonFileOrDefault<T>(string filePath, T defaultValue, bool saveIfNotExists = true, bool saveIndented = true, bool moveIfError = true)
        {
            if (string.IsNullOrEmpty(filePath))
                return defaultValue;

            if (!File.Exists(filePath))
            {
                if (saveIfNotExists)
                    TrySaveJsonFile(filePath, defaultValue!, saveIndented);

                return defaultValue;
            }

            if (!TryLoadJsonFile<T>(filePath, out var result))
            {
                if (moveIfError)
                {
                    var errorPath = filePath + ".error_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    try
                    {
                        File.Move(filePath, errorPath);
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Failed to move erroneous config file &3{Path.GetFileName(filePath)}&r to &3{Path.GetFileName(errorPath)}&r:\n{ex}");
                    }

                    TrySaveJsonFile(filePath, defaultValue!, saveIndented);
                }

                return defaultValue;
            }

            return result;
        }

        /// <summary>
        /// Attempts to save the specified object as a JSON file in the given directory with the provided file name.
        /// </summary>
        /// <remarks>If either <paramref name="directory"/> or <paramref name="name"/> is null or empty,
        /// the method returns false and no file is created.</remarks>
        /// <param name="directory">The path to the directory where the JSON file will be saved. Cannot be null or empty.</param>
        /// <param name="name">The name of the JSON file to create, including the file extension. Cannot be null or empty.</param>
        /// <param name="data">The object to serialize and save as JSON. The object must be serializable.</param>
        /// <param name="indented">Specifies whether the JSON output should be formatted with indentation for readability. The default value is
        /// <see langword="true"/>.</param>
        /// <returns>true if the file was saved successfully; otherwise, false.</returns>
        public static bool TrySaveJsonFile(string directory, string name, object data, bool indented = true)
        {
            if (string.IsNullOrEmpty(directory))
                return false;

            if (string.IsNullOrEmpty(name))
                return false;

            var filePath = Path.Combine(directory, name);
            return TrySaveJsonFile(filePath, data, indented);
        }

        /// <summary>
        /// Attempts to serialize the specified object to JSON and save it to the given file path.
        /// </summary>
        /// <remarks>This method returns false if the file path or data is invalid, or if an error occurs
        /// during serialization or file writing. The method does not throw exceptions for common errors; instead, it
        /// logs them internally and returns false.</remarks>
        /// <param name="filePath">The path of the file to which the JSON data will be written. Cannot be null or empty.</param>
        /// <param name="data">The object to serialize and save as JSON. Cannot be null.</param>
        /// <param name="indented">Specifies whether the JSON output should be formatted with indentation for readability. The default is <see
        /// langword="true"/>.</param>
        /// <returns>true if the file was successfully saved; otherwise, false.</returns>
        public static bool TrySaveJsonFile(string filePath, object data, bool indented = true)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            if (data == null)
                return false;

            try
            {
                var serialized = JsonConvert.SerializeObject(data, indented 
                                                                        ? indentedSettings
                                                                        : nonIndentedSettings);

                if (string.IsNullOrEmpty(serialized))
                    return false;

                File.WriteAllText(filePath, serialized);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Caught an exception while attempting to save file &3{Path.GetFileName(filePath)}&r (&6{data.GetType()}&r):\n{ex}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to load and deserialize a JSON file from the specified directory and file name into an object of
        /// type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type into which the JSON file will be deserialized.</typeparam>
        /// <param name="directory">The path to the directory containing the JSON file. Cannot be null or empty.</param>
        /// <param name="name">The name of the JSON file to load. Cannot be null or empty.</param>
        /// <param name="result">When this method returns, contains the deserialized object if the operation succeeds; otherwise, the default
        /// value for type <typeparamref name="T"/>.</param>
        /// <returns>true if the JSON file was successfully loaded and deserialized; otherwise, false.</returns>
        public static bool TryLoadJsonFile<T>(string directory, string name, out T result)
        {
            result = default!;

            if (string.IsNullOrEmpty(directory))
                return false;

            if (string.IsNullOrEmpty(name))
                return false;

            var filePath = Path.Combine(directory, name);
            return TryLoadJsonFile(filePath, out result);
        }

        /// <summary>
        /// Attempts to load and deserialize a JSON file into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This method does not throw exceptions for file access or deserialization errors;
        /// instead, it returns false if any error occurs. The method returns false if the file does not exist, is
        /// empty, or cannot be deserialized into the specified type.</remarks>
        /// <typeparam name="T">The type into which the JSON content will be deserialized.</typeparam>
        /// <param name="filePath">The path to the JSON file to load. Cannot be null or empty.</param>
        /// <param name="result">When this method returns, contains the deserialized object if successful; otherwise, the default value for
        /// type <typeparamref name="T"/>.</param>
        /// <returns>true if the file was successfully loaded and deserialized; otherwise, false.</returns>
        public static bool TryLoadJsonFile<T>(string filePath, out T result)
        {
            result = default!;

            if (string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                if (!File.Exists(filePath))
                    return false;

                var content = File.ReadAllText(filePath);

                if (string.IsNullOrEmpty(content))
                    return false;

                result = JsonConvert.DeserializeObject<T>(content, indentedSettings)!;
                return result != null;
            }
            catch (Exception ex)
            {
                log.Error($"Caught an exception while loading file &3{Path.GetFileName(filePath)}&r (&6{typeof(T)}&r):\n{ex}");
                return false;
            }
        }
    }
}