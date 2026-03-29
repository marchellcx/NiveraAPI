using NiveraAPI.Pooling;
using NiveraAPI.TokenParsing.Interfaces;

namespace NiveraAPI.TokenParsing.Tokens;

/// <summary>
/// Represents a collection token.
/// </summary>
public class CollectionToken : Token, IConvertableToken
{
    /// <summary>
    /// Gets an instance of <see cref="CollectionToken"/>.
    /// </summary>
    public static CollectionToken Instance { get; } = new();
    
    internal CollectionToken() { }
    
    /// <summary>
    /// Gets or sets the character used to identify collections.
    /// </summary>
    public static char StartToken { get; set; } = '[';
    
    /// <summary>
    /// Gets or sets the character used to identify the end of a collection.
    /// </summary>
    public static char EndToken { get; set; } = ']';

    /// <summary>
    /// Gets or sets the character used to split items.
    /// </summary>
    public static char SplitToken { get; set; } = ',';

    /// <summary>
    /// Gets a list of all parsed values.
    /// </summary>
    public List<string> Values { get; } = new();

    /// <inheritdoc cref="Token.NewToken"/>
    public override Token NewToken()
        => PoolBase<CollectionToken>.Shared.Rent();

    /// <inheritdoc cref="Token.ReturnToken"/>
    public override void ReturnToken()
        => PoolBase<CollectionToken>.Shared.Return(this);

    /// <inheritdoc cref="PoolResettable.OnPooled"/>
    public override void OnPooled()
    {
        base.OnPooled();

        Values.Clear();
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

        if (type == typeof(List<string>))
        {
            value = new List<string>(Values);
            return true;
        }

        if (type == typeof(string[]))
        {
            value = Values.ToArray();
            return true;       
        }

        if (type == typeof(HashSet<string>))
        {
            value = new HashSet<string>(Values);
            return true;      
        }

        if (type == typeof(char[]))
        {
            var list = ListPool<char>.Shared.Rent();

            for (var i = 0; i < Values.Count; i++)
            {
                var str = Values[i];

                for (var x = 0; x < str.Length; x++)
                {
                    list.Add(str[x]);
                }
            }
            
            value = ListPool<char>.ReturnToArray(list);
            return true;
        }

        if (type == typeof(List<char>))
        {
            var list = new List<char>();

            for (var i = 0; i < Values.Count; i++)
            {
                var str = Values[i];

                for (var x = 0; x < str.Length; x++)
                {
                    list.Add(str[x]);
                }
            }

            value = list;
            return true;
        }

        if (type == typeof(HashSet<char>))
        {
            var list = new HashSet<char>();

            for (var i = 0; i < Values.Count; i++)
            {
                var str = Values[i];

                for (var x = 0; x < str.Length; x++)
                {
                    list.Add(str[x]);
                }
            }

            value = list;
            return true;
        }
        
        return false;
    }
}