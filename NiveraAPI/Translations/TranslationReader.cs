using System.Text;

using NiveraAPI.Translations.Exceptions;

namespace NiveraAPI.Translations;

/// <summary>
/// Responsible for reading and processing translation data from a buffer, enabling interaction
/// with translation entries in the context of a <see cref="Translation"/> object.
/// </summary>
public class TranslationReader
{
    private int m_Pos = 0;
    private string[]? m_Buffer;
    private Translation? m_Translation;

    /// <summary>
    /// Creates a new instance of the <see cref="TranslationReader"/> class.
    /// </summary>
    public TranslationReader(Translation translation)
    {
        m_Translation = translation;
    }

    /// <summary>
    /// Reads and processes translation entries from the provided buffer.
    /// </summary>
    /// <param name="buffer">An array of strings representing the content of a translation file.</param>
    /// <exception cref="TranslationEntryNotFoundException">
    /// Thrown when a translation entry referenced in the buffer is not found in the translation data.
    /// </exception>
    public void Read(string[] buffer)
    {
        m_Buffer = buffer;

        string? text = null;
        ITranslationEntry? entry = null;
        
        var flag = false;
        var stringBuilder = new StringBuilder();
        
        while (m_Pos < m_Buffer.Length)
        {
            var text2 = m_Buffer[m_Pos];
            
            m_Pos++;
            
            if ((string.IsNullOrWhiteSpace(text2) && !flag) || (text2.StartsWith("Language:") && !flag))
                continue;
            
            if (text2.StartsWith("<--") && text2.EndsWith("-->"))
            {
                text = text2.Replace("<--", "").Replace("-->", "").Trim();
                
                entry = null;
                
                if (!m_Translation.TryGetEntry(text, out entry))
                    throw new TranslationEntryNotFoundException(text);
            }
            else if (text2.StartsWith("##") && text2.EndsWith("##"))
            {
                if (text2.Contains("Translation"))
                {
                    if (flag)
                    {
                        flag = false;
                        entry.StringValue = stringBuilder.ToString().Trim();
                    }
                    else
                    {
                        flag = true;
                        stringBuilder.Clear();
                    }
                }
            }
            else if (entry != null && flag)
            {
                stringBuilder.AppendLine(text2);
            }
        }
    }
}