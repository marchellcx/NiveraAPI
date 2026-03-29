using NiveraAPI.Pooling;
using NiveraAPI.TokenParsing.Interfaces;

namespace NiveraAPI.TokenParsing.Tokens;

/// <summary>
/// Represents a string word.
/// </summary>
public class StringToken : Token, IConvertableToken
{
    /// <summary>
    /// Gets an instance of the string token.
    /// </summary>
    public static StringToken Instance { get; } = new();
    
    internal StringToken() { }
    
    /// <summary>
    /// Gets or sets the character used to identify a full string token.
    /// </summary>
    public static char Token { get; set; } = '\"';

    /// <summary>
    /// Gets or sets the token's value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <inheritdoc cref="Token.NewToken"/>
    public override Token NewToken()
        => PoolBase<StringToken>.Shared.Rent();
    
    /// <inheritdoc cref="Token.ReturnToken"/>
    public override void ReturnToken()
        => PoolBase<StringToken>.Shared.Return(this);
    
    /// <inheritdoc/>
    public override void OnPooled()
    {
        base.OnPooled();
        
        Value = string.Empty;
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
        => $"[StringToken] ({Value ?? string.Empty})";

    /// <summary>
    /// Attempts to convert the current token to the specified type.
    /// </summary>
    /// <param name="type">The target type to which the token should be converted.</param>
    /// <param name="value">When this method returns, contains the converted value if the conversion succeeded, or null if it failed.</param>
    /// <returns>True if the conversion was successful; otherwise, false.</returns>
    public bool TryConvert(Type type, out object? value)
    {
        value = null;

        if (type == typeof(string[]))
        {
            value = new string[] { Value };
            return true;
        }

        if (type == typeof(List<string>))
        {
            value = new List<string> { Value };
            return true;      
        }

        if (type == typeof(HashSet<string>))
        {
            value = new HashSet<string> { Value };
            return true;     
        }
        
        return false;
    }
}