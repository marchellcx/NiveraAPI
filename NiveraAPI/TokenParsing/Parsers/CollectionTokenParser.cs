using NiveraAPI.TokenParsing.Tokens;
using NiveraAPI.Extensions;

namespace NiveraAPI.TokenParsing.Parsers;

/// <summary>
/// Parsers collection tokens.
/// </summary>
public class CollectionTokenParser : TokenParser
{
    /// <inheritdoc cref="TokenParser.ShouldStart"/>
    public override bool ShouldStart(TokenContext context)
        => !context.PreviousCharIsEscape() && context.CurrentCharIs(CollectionToken.StartToken);

    /// <inheritdoc cref="TokenParser.ShouldTerminate"/>
    public override bool ShouldTerminate(TokenContext context)
        => !context.PreviousCharIsEscape() && context.CurrentCharIs(CollectionToken.EndToken);

    /// <inheritdoc cref="TokenParser.OnTerminated"/>
    public override void OnTerminated(TokenContext context)
    {
        if (!context.CurrentTokenIs<CollectionToken>(out var collectionToken))
            return;

        if (context.Builder.Length > 0)
        {
            context.Builder.RemoveTrailingWhiteSpaces();
            
            collectionToken.Values.Add(context.Builder.ToString());
        }
    }

    /// <inheritdoc cref="TokenParser.ProcessContext"/>
    public override bool ProcessContext(TokenContext context)
    {
        if (context.CurrentParser is not CollectionTokenParser)
            return true;

        if (!context.CurrentTokenIs<CollectionToken>(out var collectionToken))
            return true;

        // Prevent a leading whitespace
        if (context is { IsCurrentWhiteSpace: true, Builder.Length: < 1 })
            return true;

        if (!context.PreviousCharIsEscape() && context.CurrentCharIs(CollectionToken.SplitToken))
        {
            context.Builder.RemoveTrailingWhiteSpaces();
            
            collectionToken.Values.Add(context.Builder.ToString());
            
            context.Builder.Clear();
            return false;
        }
        
        context.Builder.Append(context.CurrentChar);
        return false;
    }

    /// <inheritdoc cref="TokenParser.CreateToken"/>
    public override Token CreateToken(TokenContext context)
        => CollectionToken.Instance.NewToken();
}