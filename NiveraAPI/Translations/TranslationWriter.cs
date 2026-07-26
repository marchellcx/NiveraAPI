using System.Text;

using NiveraAPI.Extensions;

namespace NiveraAPI.Translations;

/// <summary>
/// Provides functionality for writing translation data, including language metadata
/// and translation entries, to a buffer. The contents can be exported as a string
/// representation.
/// </summary>
public class TranslationWriter
{
    private StringBuilder m_Buffer;

    /// <summary>
    /// Writes the language metadata to the internal buffer.
    /// </summary>
    /// <param name="languageName">The name of the language being written.</param>
    /// <param name="languageCode">The code of the language, typically in a short format (e.g., 'en', 'fr').</param>
    public void WriteLanguage(string languageName, string languageCode)
    {
        if (m_Buffer == null)
            m_Buffer = new StringBuilder();
        
        m_Buffer.AppendLine($"Language: {languageName} ({languageCode})");
    }

    /// <summary>
    /// Writes the details of the specified translation entry to the internal buffer.
    /// </summary>
    /// <param name="entry">The translation entry to be written, including its ID, description, parameters, and string value.</param>
    public void Write(ITranslationEntry entry)
    {
        if (m_Buffer == null)
            m_Buffer = new StringBuilder();
        
        m_Buffer.AppendLine();
        m_Buffer.AppendLine("<-- " + entry.Id + " -->");
        
        if (!string.IsNullOrWhiteSpace(entry.Description))
        {
            m_Buffer.AppendLine();
            m_Buffer.AppendLine("## Description ##");
            m_Buffer.AppendLine("# " + entry.Description);
            m_Buffer.AppendLine("## Description ##");
        }
        
        if (entry.Parameters != null && entry.Parameters.Any())
        {
            m_Buffer.AppendLine();
            m_Buffer.AppendLine("## Parameters ##");
            
            for (var i = 0; i < entry.Parameters.Count; i++)
            {
                var tuple = entry.Parameters.ElementAt(i);
                
                if (!string.IsNullOrWhiteSpace(tuple.Item1) && !string.IsNullOrWhiteSpace(tuple.Item2))
                {
                    if (!string.IsNullOrWhiteSpace(tuple.Item2))
                    {
                        m_Buffer.AppendLine($"[{i + 1}] {tuple.Item1} ({tuple.Item2}) # {tuple.Item3}");
                    }
                    else
                    {
                        m_Buffer.AppendLine($"[{i + 1}] {tuple.Item1} ({tuple.Item2})");
                    }
                }
            }
            
            m_Buffer.AppendLine("## Parameters ##");
        }
        
        m_Buffer.AppendLine();
        m_Buffer.AppendLine("## Translation ##");
        
        var values = entry.StringValue.SplitLines();
        
        values.ForEach(str => m_Buffer.AppendLine(str));
        
        m_Buffer.AppendLine("## Translation ##");
        m_Buffer.AppendLine();
        m_Buffer.AppendLine(">-- " + entry.Id + " --<");
    }

    /// <summary>
    /// Converts the contents of the internal buffer to a string representation
    /// and clears the buffer after the conversion.
    /// </summary>
    /// <returns>The string representation of the current contents of the internal buffer.</returns>
    public override string ToString()
    {
        var result = m_Buffer.ToString();
        
        m_Buffer.Clear();
        return result;
    }
}
