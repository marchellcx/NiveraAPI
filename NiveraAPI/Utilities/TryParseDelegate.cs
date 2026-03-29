namespace NiveraAPI.Utilities;

/// <summary>
/// Represents a delegate that attempts to parse a string input into a specified type.
/// </summary>
/// <typeparam name="T">
/// The type of the result to parse the input into.
/// </typeparam>
/// <param name="input">
/// The string to be parsed.
/// </param>
/// <param name="result">
/// When this method returns, contains the parsed value of type T, if the parsing succeeded;
/// otherwise, the default value of type T. This parameter is passed uninitialized.
/// </param>
/// <returns>
/// A boolean value indicating whether the parsing was successful.
/// </returns>
public delegate bool TryParseDelegate<T>(string input, out T result);