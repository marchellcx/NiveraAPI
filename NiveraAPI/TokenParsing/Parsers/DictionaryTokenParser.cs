using NiveraAPI.TokenParsing.Tokens;
using NiveraAPI.Extensions;

namespace NiveraAPI.TokenParsing.Parsers;

/// <summary>
/// Parses dictionary tokens.
/// </summary>
public class DictionaryTokenParser : TokenParser
{
    /// <inheritdoc cref="TokenParser.ShouldStart"/>
    public override bool ShouldStart(TokenContext context)
        => !context.PreviousCharIsEscape() && context.CurrentCharIs(DictionaryToken.StartToken);

    /// <inheritdoc cref="TokenParser.ShouldTerminate"/>
    public override bool ShouldTerminate(TokenContext context)
        => !context.PreviousCharIsEscape() && context.CurrentCharIs(DictionaryToken.EndToken);

    /// <inheritdoc cref="TokenParser.OnTerminated"/>
    public override void OnTerminated(TokenContext context)
    {
        if (!context.CurrentTokenIs<DictionaryToken>(out var dictionaryToken))
            return;

        if (dictionaryToken.KeyBuilder.Length < 1 || dictionaryToken.ValueBuilder.Length < 1)
            return;

        dictionaryToken.KeyBuilder.RemoveTrailingWhiteSpaces();
        dictionaryToken.ValueBuilder.RemoveTrailingWhiteSpaces();
        
        var key = dictionaryToken.KeyBuilder.ToString();
        var value = dictionaryToken.ValueBuilder.ToString();
        
        dictionaryToken.KeyBuilder.Clear();
        dictionaryToken.ValueBuilder.Clear();
        
        if (!dictionaryToken.Values.ContainsKey(key))
            dictionaryToken.Values.Add(key, value);
    }

    /// <inheritdoc cref="TokenParser.ProcessContext"/>
    public override bool ProcessContext(TokenContext context)
    {
        if (context.CurrentParser is not DictionaryTokenParser)
            return true;

        if (!context.CurrentTokenIs<DictionaryToken>(out var dictionaryToken))
            return true;

        if (!dictionaryToken.IsValue)
        {
            if (context.IsCurrentWhiteSpace && dictionaryToken.KeyBuilder.Length < 1) 
                return false;

            if (context.CurrentCharIs(DictionaryToken.SplitToken) && !context.PreviousCharIsEscape())
            {
                dictionaryToken.IsValue = true;
                return false;
            }
            
            dictionaryToken.KeyBuilder.Append(context.CurrentChar);
            return false;
        }

        if (context.IsCurrentWhiteSpace && dictionaryToken.ValueBuilder.Length < 1)
            return false;
        
        if (context.CurrentCharIs(CollectionToken.SplitToken) && !context.PreviousCharIsEscape())
        {
            dictionaryToken.IsValue = false;

            dictionaryToken.KeyBuilder.RemoveTrailingWhiteSpaces();
            dictionaryToken.ValueBuilder.RemoveTrailingWhiteSpaces();

            var key = dictionaryToken.KeyBuilder.ToString();
            var value = dictionaryToken.ValueBuilder.ToString();

            if (!dictionaryToken.Values.ContainsKey(key))
                dictionaryToken.Values.Add(key, value);

            dictionaryToken.KeyBuilder.Clear();
            dictionaryToken.ValueBuilder.Clear();

            return false;
        }
        
        dictionaryToken.ValueBuilder.Append(context.CurrentChar);
        return false;
    }

    /// <inheritdoc cref="TokenParser.CreateToken"/>
    public override Token CreateToken(TokenContext context)
        => DictionaryToken.Instance.NewToken();
}