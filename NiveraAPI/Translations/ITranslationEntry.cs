namespace NiveraAPI.Translations;

/// <summary>
/// Represents a translation entry that defines translatable text, its unique identifier,
/// and any parameters necessary for dynamic content generation in translations.
/// </summary>
public interface ITranslationEntry
{
    /// <summary>
    /// Represents the core translation string associated with a translation entry.
    /// This string defines the raw value to be translated or used directly as output.
    /// </summary>
    string StringValue { get; set; }

    /// <summary>
    /// Provides a brief description associated with the translation entry.
    /// This description gives context or details about the entry's purpose
    /// or usage within the translation system.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the unique identifier associated with the translation entry.
    /// The identifier is used to differentiate and retrieve specific translation entries
    /// within a collection or translation system.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the list of parameters associated with the translation entry.
    /// Each parameter is represented as a tuple containing three elements:
    /// - The parameter name with a prefixed `$`.
    /// - The type of the parameter.
    /// - A description of the parameter.
    /// </summary>
    List<Tuple<string, string, string>> Parameters { get; }

    /// <summary>
    /// Replaces placeholders in the given input string with the corresponding values
    /// from the provided parameters array. The method ensures that the number of
    /// parameters matches the required count for placeholders in the string.
    /// </summary>
    /// <param name="input">
    /// A reference to the string in which placeholders will be replaced
    /// with parameter values.
    /// </param>
    /// <param name="parameters">
    /// An array of strings containing replacement values for the placeholders
    /// in the input string. The number of items in the array must match the
    /// number of placeholders.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the number of provided parameters does not match the number
    /// of placeholders in the input string.
    /// </exception>
    void ReplaceParameters(ref string input, string[] parameters);

    /// <summary>
    /// Translates the current translation entry using the provided parameters.
    /// The method replaces placeholders within the translation's string value
    /// with corresponding values from the parameters provided.
    /// </summary>
    /// <param name="parameters">
    /// An array of strings containing values to replace placeholders
    /// within the translation's string value.
    /// </param>
    /// <returns>
    /// The translated string with placeholders replaced by the provided parameters.
    /// </returns>
    string Translate(string[] parameters);
}