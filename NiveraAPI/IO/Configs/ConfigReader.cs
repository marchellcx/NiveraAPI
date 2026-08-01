using System.Text;

using NiveraAPI.Logs;
using NiveraAPI.Extensions;

namespace NiveraAPI.IO.Configs;

/// <summary>
/// Responsible for reading and parsing configuration data formatted in sections, keys, and values.
/// </summary>
public static class ConfigReader
{
    /// <summary>
    /// Whether or not to log debug messages.
    /// </summary>
    public static bool DebugLogs = false;
    
    // Format:
    
    // # Comment
    // [Key]
    // Value
    
    // Section {
    //   # Comment
    //   [Key]
    //   Value
    // }

    /// <summary>
    /// Reads and parses configuration data from the provided text input.
    /// </summary>
    /// <param name="lines">The configuration text containing sections, keys, and values to be parsed.</param>
    /// <returns>
    /// A dictionary where the keys represent the concatenated section and key names (formatted as "Section.Key"),
    /// and the values represent the corresponding configuration values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="lines"/> is null.</exception>
    public static Dictionary<string, string> ReadConfigs(string[] lines)
    {
        if (lines == null)
            throw new ArgumentNullException(nameof(lines));

        var builder = new StringBuilder();
        var read = new Dictionary<string, string>();

        bool isInValue = false;

        string? key = null;
        string? value = null;
        string? section = null;

        void SaveValue()
        {
            Log.Debug("SaveValue()", DebugLogs);

            if (builder.Length > 0 && !string.IsNullOrEmpty(key))
            {
                Log.Debug($"Builder length &3{builder.Length}&r, key &3{key}&r, section &3{section ?? "null"}&r",
                    DebugLogs);

                value = builder.ToString();


                Log.Debug("Value", DebugLogs);
                Log.Debug(value, DebugLogs);

                if (!string.IsNullOrEmpty(section))
                    read[section + "." + key] = value;
                else
                    read[key] = value;

                key = null;
                value = null;
            }
            else
            {
                Log.Debug("No value to save", DebugLogs);
            }

            builder.Clear();
        }

        Log.Debug($"Reading config file, {lines.Length} lines", DebugLogs);

        for (var x = 0; x < lines.Length; x++)
        {
            var line = lines[x].TrimEnd(' ');
            var trimmedLine = line.Trim();

            Log.Debug(
                $"Line: &3{line}&r ({x} / {lines.Length}), section: &3{section ?? "null"}&r, key: &3{key ?? "null"}&r",
                DebugLogs);

            if (trimmedLine.Length < 1
                || string.IsNullOrWhiteSpace(trimmedLine)) // Ignore empty lines
            {
                if (DebugLogs)
                    Log.Debug("Whitespace line", DebugLogs);

                if (isInValue) // Unless the empty line belongs to the value
                {
                    Log.Debug("Currently in value, appending empty line", DebugLogs);

                    if (section != null)
                    {
                        if (builder.Length > 0)
                            builder.AppendLine();

                        builder.AppendLine(new string(line.Skip(2).ToArray()));
                    }
                    else
                    {
                        if (builder.Length > 0)
                            builder.AppendLine();

                        builder.AppendLine(line);
                    }

                    builder.RemoveTrailingWhiteSpaces(true);
                }
                else
                {
                    Log.Debug("Not in value, ignoring", DebugLogs);
                }

                continue;
            }

            if (trimmedLine[0] == '#') // Ignore comments
            {
                Log.Debug("Skipping comment", DebugLogs);

                continue;
            }

            if (trimmedLine.Length == 1 && trimmedLine[0] == '}') // Handle section end
            {
                Log.Debug("Section end, saving value", DebugLogs);

                SaveValue();

                section = null;
                continue;
            }

            if (char.IsLetter(trimmedLine[0])
                && trimmedLine[trimmedLine.Length - 1] == '{') // Check for section start
            {
                Log.Debug("Section start, saving value", DebugLogs);

                SaveValue();

                section = trimmedLine
                    .Substring(0, trimmedLine.Length - 1)
                    .Trim();

                continue;
            }

            if (trimmedLine[0] == '[' && trimmedLine[trimmedLine.Length - 1] == ']')
            {
                Log.Debug("Key start, saving value", DebugLogs);

                SaveValue();

                key = trimmedLine.Substring(1, trimmedLine.Length - 2)
                    .Trim();

                isInValue = true;
                continue;
            }

            if (isInValue)
            {
                Log.Debug("Appending to value", DebugLogs);

                if (section != null)
                {
                    if (builder.Length > 0)
                        builder.AppendLine();

                    builder.AppendLine(new string(line.Skip(2).ToArray()));
                }
                else
                {
                    if (builder.Length > 0)
                        builder.AppendLine();

                    builder.AppendLine(line);
                }

                builder.RemoveTrailingWhiteSpaces(true);
            }
            else if (DebugLogs)
            {
                Log.Debug("Line not appended", DebugLogs);
            }
        }

        Log.Debug("Finished reading config file, saving last value", DebugLogs);

        SaveValue();
        return read;
    }
}