namespace NiveraAPI.Translations;

/// <summary>
/// Extension methods for <see cref="ITranslationEntry"/> objects.
/// </summary>
public static class TranslationExtensions
{
    /// <summary>
    /// Adds a parameter to the translation entry with the specified name, type, and description.
    /// </summary>
    /// <param name="entry">
    /// The translation entry to which the parameter will be added.
    /// </param>
    /// <param name="parameterName">
    /// The name of the parameter to add. This name will be prefixed with a dollar sign ('$')
    /// when stored in the parameter list.
    /// </param>
    /// <param name="parameterType">
    /// The type of the parameter. This is used to describe the expected value type for the parameter.
    /// </param>
    /// <param name="parameterDescription">
    /// A description of the parameter, providing context about its purpose or usage.
    /// </param>
    /// <returns>
    /// The updated translation entry, including the newly added parameter.
    /// </returns>
    public static ITranslationEntry WithParameter(this ITranslationEntry entry, string parameterName,
        string parameterType, string parameterDescription)
    {
        entry.Parameters.Add(new Tuple<string, string, string>("$" + parameterName, parameterType, parameterDescription));
        return entry;
    }
}