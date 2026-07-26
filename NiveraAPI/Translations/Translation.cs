using NiveraAPI.Extensions;

using NiveraAPI.Translations.Entries;
using NiveraAPI.Translations.Exceptions;

namespace NiveraAPI.Translations;

/// <summary>
/// Represents a collection of translation entries, along with associated metadata such as
/// the language name, language code, and file path. Provides methods for managing and retrieving
/// translation entries.
/// </summary>
public class Translation
{
	private HashSet<ITranslationEntry> m_Entries = new HashSet<ITranslationEntry>();

	internal string m_LanguageName;
	internal string m_LanguageCode;

	private string m_FilePath;

	private TranslationReader m_Reader;
	private TranslationWriter m_Writer;

	/// <summary>
	/// Gets the name of the language associated with the translation.
	/// </summary>
	public string LanguageName => m_LanguageName;

	/// <summary>
	/// Gets the language code associated with the translation.
	/// </summary>
	public string LanguageCode => m_LanguageCode;

	/// <summary>
	/// Gets the file path where the translation file is stored.
	/// </summary>
	public string FilePath => m_FilePath;

	/// <summary>
	/// Gets the complete file name of the translation file, composed of the file path, a fixed naming convention,
	/// and the associated language name.
	/// </summary>
	public string FileName => Path.Combine(FilePath, "translation." + LanguageName);

	/// <summary>
	/// Provides a read-only collection of translation entries within the current instance of the <see cref="Translation"/> class.
	/// </summary>
	public IReadOnlyCollection<ITranslationEntry> Entries => m_Entries;

	/// <summary>
	/// Creates a new instance of the <see cref="Translation"/> class.
	/// </summary>
	public Translation(string path, string languageName, string languageCode)
	{
		m_FilePath = path;
		
		m_LanguageName = languageName;
		m_LanguageCode = languageCode;
		
		m_Reader = new TranslationReader(this);
		m_Writer = new TranslationWriter();
	}

	/// <summary>
	/// Adds a new translation entry to the collection or updates an existing one
	/// with the provided value and description.
	/// </summary>
	/// <param name="id">The unique identifier of the translation entry to add or update.</param>
	/// <param name="value">The string value of the translation entry.</param>
	/// <param name="description">A description providing additional context for the translation entry.</param>
	/// <returns>
	/// The newly created translation entry, or the updated translation entry if one
	/// with the specified identifier already exists.
	/// </returns>
	public ITranslationEntry Add(string id, string value, string description)
	{
		if (TryGetEntry(id, out var entry))
		{
			entry.StringValue = value;
			return entry;
		}
		
		entry = new StringEntry(id, value, description);
		
		m_Entries.Add(entry);
		m_Entries = m_Entries.OrderByDescending(entry => entry.Id).ToHashSet();
		
		return entry;
	}

	/// <summary>
	/// Retrieves a translated string identified by the specified unique identifier
	/// and formats it using the provided parameters.
	/// </summary>
	/// <param name="id">The unique identifier of the translation entry to retrieve.</param>
	/// <param name="parameters">
	/// An array of parameters to format the translation entry. These are used to
	/// replace placeholders within the translation string.
	/// </param>
	/// <returns>
	/// The translated and formatted string associated with the specified identifier.
	/// </returns>
	/// <exception cref="TranslationEntryNotFoundException">
	/// Thrown if no translation entry exists with the specified identifier.
	/// </exception>
	public string Get(string id, params object[] parameters)
	{
		if (TryGetEntry(id, out var entry))
			return entry.Translate(parameters.Select(parameter => parameter.ToString()).ToArray());

		throw new TranslationEntryNotFoundException(id);
	}

	/// <summary>
	/// Loads the translation data from the corresponding file.
	/// If the file does not exist, it creates a new translation file
	/// by saving the current data. If the file exists, its content
	/// is read and processed to populate the translation entries.
	/// </summary>
	/// <exception cref="FileNotFoundException">
	/// Thrown if the specified file path is invalid or inaccessible.
	/// </exception>
	/// <exception cref="TranslationEntryNotFoundException">
	/// Thrown if a translation entry referenced in the file is missing
	/// or cannot be processed.
	/// </exception>
	public void Load()
	{
		if (!File.Exists(FileName))
		{
			Save();
			return;
		}
		
		string[] array = File.ReadAllLines(FileName);
		
		if (array.Length > 0)
			m_Reader.Read(array);
	}

	/// <summary>
	/// Saves the current translation data to a file. This includes writing the language metadata
	/// and all translation entries to the specified file path.
	/// </summary>
	public void Save()
	{
		m_Writer.WriteLanguage(LanguageName, LanguageCode);
		
		m_Entries.ForEach(delegate(ITranslationEntry entry)
		{
			m_Writer.Write(entry);
		});
		
		var contents = m_Writer.ToString();
		
		File.WriteAllText(FileName, contents);
	}

	/// <summary>
	/// Attempts to retrieve a translation entry by its unique identifier.
	/// </summary>
	/// <param name="entryId">The unique identifier of the translation entry to retrieve.</param>
	/// <param name="entry">
	/// When this method returns, contains the translation entry associated with the specified
	/// identifier, if found; otherwise, null. This parameter is passed uninitialized.
	/// </param>
	/// <returns>
	/// True if the translation entry with the specified identifier is found; otherwise, false.
	/// </returns>
	public bool TryGetEntry(string entryId, out ITranslationEntry entry)
	{
		return m_Entries.TryGetFirst(entry => entry.Id == entryId, out entry);
	}
}