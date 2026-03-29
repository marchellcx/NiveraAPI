using System.Text;
using NiveraAPI.Pooling;

namespace NiveraAPI.TokenParsing;

/// <summary>
/// Represents the context for token parsing.
/// </summary>
public class TokenContext : IDisposable
{
    /// <summary>
    /// Gets the previous token.
    /// </summary>
    public volatile Token? PreviousToken;

    /// <summary>
    /// Gets the current token.
    /// </summary>
    public volatile Token? CurrentToken;
 
    /// <summary>
    /// Gets the token collection.
    /// </summary>
    public volatile List<Token> Tokens;

    /// <summary>
    /// Gets the builder assigned to this context.
    /// </summary>
    public volatile StringBuilder Builder;

    /// <summary>
    /// Gets the builder used to append no-parser spaces.
    /// </summary>
    public volatile StringBuilder EmptyBuilder;

    /// <summary>
    /// Gets the current token's parser.
    /// </summary>
    public volatile TokenParser? CurrentParser;
    
    /// <summary>
    /// Gets the parser of the previous token.
    /// </summary>
    public volatile TokenParser? PreviousParser;
    
    /// <summary>
    /// Gets the previous character.
    /// </summary>
    public char? PreviousChar;

    /// <summary>
    /// Gets the next character.
    /// </summary>
    public char? NextChar;

    /// <summary>
    /// Gets the current character.
    /// </summary>
    public volatile char CurrentChar;

    /// <summary>
    /// Gets the input string.
    /// </summary>
    public volatile string Input;

    /// <summary>
    /// Gets the current index.
    /// </summary>
    public volatile int Index;

    /// <summary>
    /// Gets or sets the custom state.
    /// </summary>
    public volatile object? State;

    /// <summary>
    /// Whether or not the current position is the end of the input string.
    /// </summary>
    public bool IsEnd => !NextChar.HasValue;

    /// <summary>
    /// Whether or not the current character is whitespace.
    /// </summary>
    public bool IsCurrentWhiteSpace => char.IsWhiteSpace(CurrentChar);

    /// <summary>
    /// Whether or not the previous character is whitespace.
    /// </summary>
    public bool IsPreviousWhiteSpace => PreviousChar.HasValue && char.IsWhiteSpace(PreviousChar.Value);
    
    /// <summary>
    /// Whether or not the next character is whitespace.
    /// </summary>
    public bool IsNextWhiteSpace => NextChar.HasValue && char.IsWhiteSpace(NextChar.Value);
    
    /// <summary>
    /// Creates a new <see cref="TokenContext"/> instance.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="tokens">The result token list.</param>
    public TokenContext(string input, List<Token> tokens)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));

        Builder = StringBuilderPool.Shared.Rent();
        EmptyBuilder = StringBuilderPool.Shared.Rent();
    }
    
    /// <summary>
    /// Whether or not the current character is a specific one.
    /// </summary>
    /// <param name="c">The expected character.</param>
    /// <returns>true if the current character is equal to <paramref name="c"/>.</returns>
    public bool CurrentCharIs(char c) 
        => CurrentChar == c;
    
    /// <summary>
    /// Whether or not the next character is a specific one.
    /// </summary>
    /// <param name="c">The expected character.</param>
    /// <returns>true if the next character is equal to <paramref name="c"/>.</returns>
    public bool NextCharIs(char c) 
        => NextChar == c;
    
    /// <summary>
    /// Whether or not the previous character is a specific one.
    /// </summary>
    /// <param name="c">The expected character.</param>
    /// <returns>true if the previous character is equal to <paramref name="c"/>.</returns>
    public bool PreviousCharIs(char c) 
        => PreviousChar == c;

    /// <summary>
    /// Whether or not the previous character is the escape token.
    /// </summary>
    /// <returns>true if the previous character is the escape token</returns>
    public bool PreviousCharIsEscape()
        => PreviousChar == '\\';
    
    /// <summary>
    /// Whether or not the current token is a specific type.
    /// </summary>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns>true if the current token is a specific type</returns>
    public bool CurrentTokenIs<T>() where T : Token
        => CurrentToken is T;

    /// <summary>
    /// Whether or not the current token is a specific type.
    /// </summary>
    /// <param name="token">The resulting token.</param>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns>true if the current token is a specific type</returns>
    public bool CurrentTokenIs<T>(out T? token) where T : Token
    {
        token = default;

        if (CurrentToken is not T result)
            return false;

        token = result;
        return true;
    }
    
    /// <summary>
    /// Whether or not the previous token is a specific type.
    /// </summary>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns>true if the previous token is a specific type</returns>
    public bool PreviousTokenIs<T>() where T : Token
        => PreviousToken is T;

    /// <summary>
    /// Whether or not the previous token is a specific type.
    /// </summary>
    /// <param name="token">The resulting token.</param>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns>true if the previous token is a specific type</returns>
    public bool PreviousTokenIs<T>(out T? token) where T : Token
    {
        token = default;

        if (PreviousToken is not T result)
            return false;

        token = result;
        return true;
    }

    /// <summary>
    /// Whether or not the current <see cref="State"/> is of the specific type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>true if the current state implements the type</returns>
    public bool StateIs<T>()
        => State is T;

    /// <summary>
    /// Whether or not the current <see cref="State"/> is of the specific type.
    /// </summary>
    /// <param name="state">The cast state.</param>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>true if the current state implements the type</returns>
    public bool StateIs<T>(out T? state)
    {
        state = default;
        
        if (State is not T result)
            return false;
        
        state = result;
        return true;
    }

    /// <summary>
    /// Terminates the current token.
    /// </summary>
    /// <param name="overrideParser">Whether or not to skip parser checking.</param>
    /// <param name="addToken">Whether or not the token should be added to the list of tokens.</param>
    /// <returns>true if the token was terminated</returns>
    public bool TerminateToken(bool overrideParser = false, bool addToken = true)
    {
        if (CurrentToken is null)
            return false;
        
        if (!overrideParser && CurrentParser != null && !CurrentParser.OnTerminating(this))
            return false;
        
        CurrentParser?.OnTerminated(this);

        Builder?.Clear();

        if (addToken)
        {
            Tokens.Add(CurrentToken);

            PreviousToken = CurrentToken;
            PreviousParser = CurrentParser;
        }

        CurrentToken = null;
        CurrentParser = null;

        return true;
    }

    /// <summary>
    /// Releases all resources used by the current instance of <see cref="TokenContext"/>.
    /// </summary>
    public void Dispose()
    {
        if (Builder != null)
            StringBuilderPool.Shared.Return(Builder);
        
        if (EmptyBuilder != null)
            StringBuilderPool.Shared.Return(EmptyBuilder);

        NextChar = null;

        PreviousChar = null;
        PreviousToken = null;
        PreviousParser = null;
        
        CurrentChar = default;
        
        CurrentToken = null;
        CurrentParser = null;

        Tokens = null!;
        Builder = null!;
        EmptyBuilder = null!;
    }
}