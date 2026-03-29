using System.Text;
using NiveraAPI.Pooling;
using NiveraAPI.TokenParsing.Interfaces;

namespace NiveraAPI.TokenParsing.Tokens;

/// <summary>
/// Represents a dictionary token.
/// </summary>
public class DictionaryToken : Token, IConvertableToken
{
    /// <summary>
    /// Gets an instance of <see cref="DictionaryToken"/>.
    /// </summary>
    public static DictionaryToken Instance { get; } = new();
    
    internal DictionaryToken() { }
    
    /// <summary>
    /// Gets or sets the starting token of a dictionary.
    /// </summary>
    public static char StartToken { get; set; } = '{';
    
    /// <summary>
    /// Gets or sets the ending token of a dictionary.
    /// </summary>
    public static char EndToken { get; set; } = '}';

    /// <summary>
    /// Gets or sets the pair splitter token of a dictionary.
    /// </summary>
    public static char SplitToken { get; set; } = ':';
    
    /// <summary>
    /// Whether or not the value is being parsed.
    /// </summary>
    public bool IsValue { get; set; }

    /// <summary>
    /// Gets the builder of the dictionary key.
    /// </summary>
    public StringBuilder KeyBuilder { get; } = new();

    /// <summary>
    /// Gets the builder of the dictionary value.
    /// </summary>
    public StringBuilder ValueBuilder { get; } = new();
    
    /// <summary>
    /// Gets the dictionary that contains parsed values.
    /// </summary>
    public Dictionary<string, string> Values { get; } = new();

    /// <inheritdoc cref="Token.NewToken"/>
    public override Token NewToken()
        => PoolBase<DictionaryToken>.Shared.Rent();

    /// <inheritdoc cref="Token.ReturnToken"/>
    public override void ReturnToken()
        => PoolBase<DictionaryToken>.Shared.Return(this);

    /// <inheritdoc cref="PoolResettable.OnPooled"/>
    public override void OnPooled()
    {
        base.OnPooled();
        
        IsValue = false;
        
        Values.Clear();
        KeyBuilder.Clear();
        ValueBuilder.Clear();
    }
    
    /// <summary>
    /// Attempts to convert the current token to the specified type.
    /// </summary>
    /// <param name="type">The target type to which the token should be converted.</param>
    /// <param name="value">When this method returns, contains the converted value if the conversion succeeded, or null if it failed.</param>
    /// <returns>True if the conversion was successful; otherwise, false.</returns>
    public bool TryConvert(Type type, out object? value)
    {
        value = null;

        if (type == typeof(Dictionary<string, string>))
        {
            value = new Dictionary<string, string>(Values);
            return true;      
        }
        
        return false;
    }
}