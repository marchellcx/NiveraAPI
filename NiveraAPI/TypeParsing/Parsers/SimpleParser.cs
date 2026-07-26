using System.Globalization;

using NiveraAPI.TypeParsing.API;

namespace NiveraAPI.TypeParsing.Parsers;

/// <summary>
/// A parser implementation for converting string inputs into various supported data types.
/// </summary>
/// <remarks>
/// This class provides functionality for parsing string inputs into objects of specific data types
/// using predefined parsing delegates. It extends the <see cref="ParameterParser"/> class and allows
/// users to check whether a type is supported and to parse context parameters into the desired type.
/// </remarks>
public class SimpleParser : ParameterParser
{
    /// <summary>
    /// Represents a delegate used for parsing a string input into an object of a specific type.
    /// </summary>
    /// <param name="input">
    /// The string input to be parsed.
    /// </param>
    /// <returns>
    /// An object representing the parsed value of the specified type.
    /// </returns>
    /// <remarks>
    /// This delegate is defined to encapsulate the logic required to convert a string into a compatible object.
    /// It is utilized in conjunction with the <see cref="SimpleParser.Delegates"/> property to provide parsing logic
    /// for various numeric types.
    /// </remarks>
    public delegate object ParseDelegate(string input);

    /// <summary>
    /// A dictionary of delegates used to parse strings into objects of specified types.
    /// </summary>
    /// <remarks>
    /// This property provides mapping between .NET types and corresponding parsing logic encapsulated by delegates.
    /// It includes predefined parsing handlers for primitive types, enumerations, and other commonly used types.
    /// Custom parsing logic can be defined and added to this dictionary for additional type support.
    /// </remarks>
    /// <value>
    /// A read-only dictionary of type-to-delegate mappings where each delegate transforms a string representation
    /// into the specified type.
    /// </value>
    public static Dictionary<Type, ParseDelegate> Delegates { get; } = new()
    {
        [typeof(byte)] = str => byte.Parse(str),
        [typeof(sbyte)] = str => sbyte.Parse(str),
        
        [typeof(short)] = str => short.Parse(str),
        [typeof(ushort)] = str => ushort.Parse(str),
        
        [typeof(int)] = str => int.Parse(str),
        [typeof(uint)] = str => uint.Parse(str),
        
        [typeof(long)] = str => long.Parse(str),
        [typeof(ulong)] = str => ulong.Parse(str),
        
        [typeof(float)] = str => float.Parse(str, NumberStyles.Float),
        [typeof(double)] = str => double.Parse(str, NumberStyles.Float),
        [typeof(decimal)] = str => decimal.Parse(str, NumberStyles.Float),
        
        [typeof(bool)] = str => bool.Parse(str),
        [typeof(char)] = str => str[0],
        [typeof(string)] = str => str,
    };

    /// <summary>
    /// Determines if the specified type is supported by the current parser.
    /// </summary>
    /// <param name="type">
    /// The type to check for compatibility with the parser.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the specified type is supported.
    /// Returns <c>true</c> if the type is supported; otherwise, <c>false</c>.
    /// </returns>
    public override bool CheckSupport(Type type)
        => Delegates.ContainsKey(type);

    /// <summary>
    /// Parses the provided parameter context into a valid <see cref="ParameterResult"/> object.
    /// </summary>
    /// <param name="context">
    /// The parameter context containing the current parameter and token information necessary
    /// for parsing.
    /// </param>
    /// <returns>
    /// A <see cref="ParameterResult"/> object that indicates the success or failure of the
    /// parsing operation. If successful, the result will contain the parsed value; if not,
    /// an error message or exception will be included.
    /// </returns>
    public override ParameterResult ParseContext(ParameterContext context)
    {
        if (!Delegates.TryGetValue(context.CurrentParameter.Type, out var tryParseDelegate))
            return context.CreateResult($"No delegate for type {context.CurrentParameter.Type.FullName}");

        if (!context.CurTokenIsString(true, out string input))
            return context.CreateResult("Unsupported token!");

        try
        {
            var output = tryParseDelegate(input);
            return context.CreateOkResult(output);
        }
        catch (Exception ex)
        {
            return context.CreateResult(ex);
        }
    }
}