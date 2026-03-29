namespace NiveraAPI.TypeParsing;

/// <summary>
/// Represents the definition of a parameter, including its type,
/// parsing logic, and optional/default value metadata.
/// </summary>
public struct ParameterDefinition
{
    /// <summary>
    /// The type of the parameter.
    /// </summary>
    public readonly Type Type;
    
    /// <summary>
    /// Represents the type information of a parameter defined in the context
    /// of parameter parsing and handling.
    /// </summary>
    public readonly int Index;

    /// <summary>
    /// The parser used to convert string input to the parameter's data type.'
    /// </summary>
    public readonly ParameterParser? MainParser;

    /// <summary>
    /// A list of other possible parsers.
    /// </summary>
    public readonly List<ParameterParser>? OtherParsers;

    /// <summary>
    /// Indicates whether the parameter is optional in the context of parameter
    /// parsing and handling.
    /// </summary>
    public readonly bool IsOptional;

    /// <summary>
    /// Represents the default value assigned to a parameter in the context of
    /// parameter parsing and handling. This value is used when the parameter
    /// is marked as optional and no explicit value is provided during parsing.
    /// </summary>
    public readonly object? DefaultValue;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterDefinition"/> structure.
    /// </summary>
    /// <param name="type">The data type of the parameter.</param>
    /// <param name="index">The index of the parameter in the method signature.</param>
    /// <param name="parser">The parser used to convert string input to the parameter's data type.</param>
    /// <param name="subParsers">A list of other possible parsers.</param>
    /// <param name="isOptional">Indicates if the parameter is optional.</param>
    /// <param name="defaultValue">The default value for the parameter if optional.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided type is null.</exception>
    /// <exception cref="ArgumentException">Thrown if no parser is registered for the specified type.</exception>
    public ParameterDefinition(Type type, int index, ParameterParser? parser = null, List<ParameterParser>? subParsers = null, bool isOptional = false, object? defaultValue = null)
    {
        Type = type;
        Index = index;
        OtherParsers = subParsers;
        IsOptional = isOptional;
        DefaultValue = defaultValue;

        if (parser == null)
            ParameterParser.TryGetParser(type, out MainParser);
        else
            MainParser = parser;
    }
}