using NiveraAPI.TokenParsing.Tokens;

namespace NiveraAPI.TokenParsing.Parsers;

/// <summary>
/// Parses string tokens delimited by "
/// </summary>
public class StringTokenParser : TokenParser
{
    /// <inheritdoc cref="TokenParser.ShouldStart"/>
    public override bool ShouldStart(TokenContext context)
        => !context.PreviousCharIsEscape() && context.CurrentCharIs(StringToken.Token);

    /// <inheritdoc cref="TokenParser.ShouldTerminate"/>
    public override bool ShouldTerminate(TokenContext context)
        => !context.PreviousCharIsEscape() && context.CurrentCharIs(StringToken.Token);

    /// <inheritdoc cref="TokenParser.ProcessContext"/>
    public override bool ProcessContext(TokenContext context)
    {
        if (context.CurrentParser is not StringTokenParser)
            return true;

        // Prevents leading whitespace.
        if (context is { IsCurrentWhiteSpace: true, Builder.Length: < 1 })
            return false;

        context.Builder.Append(context.CurrentChar);
        return false;
    }

    /// <inheritdoc cref="TokenParser.OnTerminated"/>
    public override void OnTerminated(TokenContext context)
    {
        if (context.CurrentParser is not StringTokenParser)
            return;

        if (!context.CurrentTokenIs<StringToken>(out var stringToken))
            return;
        
        // Remove all trailing whitespaces.
        while (char.IsWhiteSpace(context.Builder[context.Builder.Length - 1]))
            context.Builder.Remove(context.Builder.Length - 1, 1);
        
        stringToken.Value = context.Builder.ToString();
    }

    /// <inheritdoc cref="TokenParser.CreateToken"/>
    public override Token CreateToken(TokenContext context)
        => StringToken.Instance.NewToken();
}