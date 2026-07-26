namespace NiveraAPI.Translations.Entries;

/// <summary>
/// Represents a translation entry that contains an identifier, a string value, and a description,
/// with support for parameterized translations.
/// </summary>
public class StringEntry : ITranslationEntry
{
    private string m_Id;
    private string m_Value;
    private string m_Description;

    private List<Tuple<string, string, string>> m_Params = new();

    /// <summary>
    /// The ID of the translation entry.
    /// </summary>
    public string Id => m_Id;
    
    /// <summary>
    /// The value of the translation entry.
    /// </summary>
    public string StringValue
    {
        get => m_Value;
        set => m_Value = value;
    }

    /// <summary>
    /// The description of the translation entry.
    /// </summary>
    public string Description => m_Description;

    /// <summary>
    /// The parameters of the translation entry.
    /// </summary>
    public List<Tuple<string, string, string>> Parameters => m_Params;

    /// <summary>
    /// Creates a new instance of the StringEntry class.
    /// </summary>
    public StringEntry(string id, string value, string description)
    {
        m_Id = id;
        m_Value = value;
        m_Description = description;
    }

    /// <summary>
    /// Replaces parameter placeholders in the specified input string with the corresponding values from the provided parameters array.
    /// </summary>
    /// <param name="input">The reference to the input string containing parameter placeholders to be replaced.</param>
    /// <param name="parameters">An array of parameter values to replace the placeholders in the input string.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the number of parameters provided does not match the number of placeholders in the parameters list.
    /// </exception>
    public void ReplaceParameters(ref string input, string[] parameters)
    {
        if (parameters.Length != m_Params.Count)
            throw new ArgumentException($"There are either too many or too few parameters! (Required: {m_Params.Count} / Received: {parameters.Length})");
        
        for (var i = 0; i < m_Params.Count; i++)
        {
            var item = m_Params[i].Item1;
            
            input = input.Replace(item, parameters[i]);
        }
    }

    /// <summary>
    /// Translates the current translation entry by replacing parameters within the text
    /// with the provided values.
    /// </summary>
    /// <param name="parameters">An array of parameter values to replace within the translation entry text.</param>
    /// <returns>The translated string with the parameters replaced.</returns>
    public string Translate(string[] parameters)
    {
        var input = new string(m_Value.ToCharArray());
        
        ReplaceParameters(ref input, parameters);
        return input;
    }
}