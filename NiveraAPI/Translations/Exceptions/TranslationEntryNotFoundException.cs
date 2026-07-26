namespace NiveraAPI.Translations.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a translation entry with a specified ID cannot be found.
/// </summary>
public class TranslationEntryNotFoundException : Exception
{
    /// <summary>
    /// Creates a new instance of the <see cref="TranslationEntryNotFoundException"/> class.
    /// </summary>
    public TranslationEntryNotFoundException(string entryId) : base("Failed to find a translation entry with ID: " + entryId) { }

    /// <summary>
    /// Throws a <see cref="TranslationEntryNotFoundException"/> if the translation entry with the specified ID is not found.
    /// </summary>
    /// <param name="entryId">The ID of the translation entry that could not be located.</param>
    /// <exception cref="TranslationEntryNotFoundException">
    /// Always thrown to indicate that the specified translation entry ID is not available.
    /// </exception>
    public static void Throw(string entryId)
        => throw new TranslationEntryNotFoundException(entryId);
}