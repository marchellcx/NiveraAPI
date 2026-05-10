using System.Text;

using NiveraAPI.Extensions;

namespace NiveraAPI.IO.Configs;

/// <summary>
/// Provides methods for writing configuration data into a grouped, formatted string representation.
/// </summary>
public static class ConfigWriter
{
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
    /// Writes configuration data in a grouped and formatted string.
    /// Each key-value pair is grouped into sections based on the key's prefix, using '.' as a separator.
    /// </summary>
    /// <param name="values">
    /// A dictionary containing configuration key-value pairs.
    /// The key is a string that may include section prefixes separated by a '.' character.
    /// The value is a KeyValuePair where the key represents additional metadata (or null) and the value holds the actual configuration value.
    /// </param>
    /// <returns>
    /// A string representation of the configuration data, formatted with sections and keys.
    /// If the dictionary is empty, an empty string is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="values"/> parameter is null.
    /// </exception>
    public static string WriteConfigs(IDictionary<string, KeyValuePair<string?, string>> values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        if (values.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();
        var grouped = new Dictionary<string, Dictionary<string, KeyValuePair<string?, string>>>();

        foreach (var kvp in values)
        {
            if (kvp.Key.TrySplit('.', true, 2, out var splits))
            {
                var section = splits[0];
                var key = splits[1];

                if (!grouped.TryGetValue(section, out var dict))
                    grouped[section] = dict = new();
                
                dict[key] = kvp.Value;
            }
            else
            {
                if (!grouped.TryGetValue(string.Empty, out var dict))
                    grouped[string.Empty] = dict = new();
                
                dict[kvp.Key] = kvp.Value;
            }
        }
        
        var ordered = grouped.OrderBy(x => x.Key);

        foreach (var kvp in ordered)
        {
            var hasSection = !string.IsNullOrWhiteSpace(kvp.Key);

            if (hasSection)
            {
                builder.Append(kvp.Key);
                builder.Append(" {");
            }

            foreach (var cfg in kvp.Value)
            {
                var index = kvp.Value.FindKeyIndex(cfg.Key);
                
                if (hasSection)
                {
                    builder.AppendLine();
                    
                    if (!string.IsNullOrEmpty(cfg.Value.Key))
                    {
                        var commentLines = cfg.Value.Key.SplitLines();

                        if (index != 0)
                            builder.AppendLine();
                        
                        foreach (var comment in commentLines)
                            builder.AppendLine($"  # {comment}");
                    }
                    
                    builder.Append("  [");
                    builder.Append(cfg.Key);
                    builder.AppendLine("]");

                    var lines = cfg.Value.Value.SplitLines();

                    foreach (var line in lines)
                    {
                        builder.Append("  ");
                        builder.AppendLine(line);
                    }
                    
                    builder.RemoveTrailingWhiteSpaces(true);
                }
                else
                {
                    builder.AppendLine();
                    
                    if (!string.IsNullOrEmpty(cfg.Value.Key))
                    {
                        var commentLines = cfg.Value.Key.SplitLines();

                        foreach (var comment in commentLines)
                            builder.AppendLine($"  # {comment}");
                    }

                    builder.Append("[");
                    builder.Append(cfg.Key);
                    builder.AppendLine("]");
                    builder.AppendLine(cfg.Value.Value);
                }
            }

            if (hasSection)
            {
                builder.AppendLine();
                builder.Append("}");
                builder.RemoveTrailingWhiteSpaces(true);
            }
        }

        var result = builder.ToString();

        builder.Clear();
        return result.Trim(' ', '\n');
    }
}