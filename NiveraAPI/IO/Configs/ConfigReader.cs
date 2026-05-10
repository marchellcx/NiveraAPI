using System.Text;

namespace NiveraAPI.IO.Configs;

/// <summary>
/// Responsible for reading and parsing configuration data formatted in sections, keys, and values.
/// </summary>
public static class ConfigReader
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
            if (builder.Length > 0 && !string.IsNullOrEmpty(key))
            {
                value = builder
                    .ToString()
                    .Trim();
                
                if (!string.IsNullOrEmpty(section))
                    read[section + "." + key] = value;
                else
                    read[key] = value;

                key = null;
                value = null;
            }

            builder.Clear();
        }

        for (var x = 0; x < lines.Length; x++)
        {
            var line = lines[x].Trim();

            if (line.Length < 1
                || string.IsNullOrWhiteSpace(line)) // Ignore empty lines
            {
                if (isInValue) // Unless the empty line belongs to the value
                {
                    if (section != null)
                        builder.AppendLine(new string(line.Skip(2).ToArray()));
                    else
                        builder.AppendLine(line);
                }

                continue;
            }

            if (line[0] == '#') // Ignore comments
                continue;

            if (line.Length == 1 && line[0] == '}') // Handle section end
            {
                SaveValue();

                section = null;
                continue;
            }

            if (char.IsLetter(line[0])
                && line[line.Length - 1] == '{') // Check for section start
            {
                SaveValue();

                section = line
                    .Substring(0, line.Length - 1)
                    .Trim();

                continue;
            }

            if (line[0] == '[' && line[line.Length - 1] == ']')
            {
                SaveValue();

                key = line.Substring(1, line.Length - 2)
                    .Trim();

                isInValue = true;
                continue;
            }

            if (isInValue)
            {
                if (section != null)
                    builder.AppendLine(new string(line.Skip(2).ToArray()));
                else
                    builder.AppendLine(line);
            }
        }

        SaveValue();
        return read;
    }
}