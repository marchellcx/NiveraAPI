namespace NiveraAPI.Translations;

/// <summary>
/// Provides functionality for managing and handling translations in an application.
/// This class includes methods for setting, loading, saving, and retrieving translations,
/// as well as adding new translation entries.
/// </summary>
public static class Translator
{
    private static Translation m_Current;

    /// <summary>
    /// Gets the current translation context used by the application.
    /// This property holds the active instance of the <see cref="Translation"/> class,
    /// providing access to language-specific translations and their associated metadata.
    /// </summary>
    /// <remarks>
    /// The current translation context can be set using the <c>Set</c> method,
    /// and it is used throughout the application to retrieve and manage translations.
    /// </remarks>
    public static Translation Current => m_Current;

    /// <summary>
    /// Sets the current translation context by initializing a new Translation instance
    /// using the specified path, language name, and language code.
    /// </summary>
    /// <param name="path">The file path where the translation files are located.</param>
    /// <param name="languageName">The name of the language for the translation context (e.g., "English").</param>
    /// <param name="languageCode">The code representing the language (e.g., "en").</param>
    public static void Set(string path, string languageName, string languageCode)
    {
        m_Current = new Translation(path, languageName, languageCode);
    }

    /// <summary>
    /// Loads the current translation context from its associated persistent storage.
    /// If a translation context is set, the method invokes the context's loading functionality.
    /// If no translation context is set, the method does nothing.
    /// </summary>
    public static void Load()
    {
        if (m_Current != null)
        {
            m_Current.Load();
        }
    }

    /// <summary>
    /// Saves the current translation context to persistent storage.
    /// If a translation context is set, its data is serialized and written to a file.
    /// If no translation context is set, the method does nothing.
    /// </summary>
    public static void Save()
    {
        if (m_Current != null)
        {
            m_Current.Save();
        }
    }

    /// <summary>
    /// Retrieves the translated string associated with the specified identifier and applies the provided parameters.
    /// If the translation context is not set, null is returned.
    /// </summary>
    /// <param name="id">The unique identifier for the translation entry.</param>
    /// <param name="parameters">A collection of parameters to format the translated string.</param>
    /// <returns>
    /// The formatted translated string if the identifier exists and the translation context is set,
    /// or null if the context is not initialized.
    /// </returns>
    public static string? Get(string id, params object[] parameters)
    {
        if (m_Current == null)
            return null;

        return m_Current.Get(id, parameters);
    }

    /// <summary>
    /// Adds a new translation entry with the specified identifier, value, and description.
    /// If an entry with the same identifier already exists, the value is updated and the existing entry is returned.
    /// </summary>
    /// <param name="id">The unique identifier for the translation entry.</param>
    /// <param name="value">The translated text or string value.</param>
    /// <param name="description">A description of the translation entry, providing additional context.</param>
    /// <returns>
    /// The added or updated translation entry as an <see cref="ITranslationEntry"/> instance,
    /// or null if the current translation context is not set.
    /// </returns>
    public static ITranslationEntry? Add(string id, string value, string description)
    {
        if (m_Current == null)
            return null;

        return m_Current.Add(id, value, description);
    }
}