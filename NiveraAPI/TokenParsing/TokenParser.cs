using NiveraAPI.Logs;
using NiveraAPI.Pooling;
using NiveraAPI.TokenParsing.Parsers;
using NiveraAPI.TokenParsing.Tokens;
using NiveraAPI.Utilities;

namespace NiveraAPI.TokenParsing;

/// <summary>
/// Represents an abstract base class for token parsers, defining methods to handle
/// various stages of token parsing within a provided context.
/// </summary>
public abstract class TokenParser
{
    private static LogSink log = LogManager.GetSource("Parsing", "Tokens");
    
    /// <summary>
    /// Whether or not to show debug logs.
    /// </summary>
    public static bool DebugLogs { get; set; }
    
    /// <summary>
    /// A static property that holds a collection of all available token parsers.
    /// This collection is used as the default set of parsers when none are explicitly provided
    /// during the parsing operation.
    /// </summary>
    /// <value>
    /// A list of <see cref="TokenParser"/> instances that can be used to parse input strings.
    /// Parsers added to this collection will be available globally for tokenization.
    /// </value>
    public static List<TokenParser> Parsers { get; } = new()
    {
        new DictionaryTokenParser(),
        new CollectionTokenParser(),
        new StringTokenParser()
    };
    
    /// <summary>
    /// Parses the given input string using the specified list of token parsers and populates
    /// the provided list of tokens based on the parsing logic.
    /// </summary>
    /// <param name="input">The input string to be tokenized.</param>
    /// <param name="parsers">A list of token parsers used to analyze the input string.</param>
    /// <param name="tokens">The list into which parsed tokens will be added.</param>
    /// <exception cref="ArgumentNullException">Thrown when input, parsers, or tokens is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the parsers list contains no elements.</exception>
    public static void Parse(string input, List<Token> tokens, List<TokenParser>? parsers = null)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));
        
        if (tokens is null)
            throw new ArgumentNullException(nameof(tokens));
        
        log.DebugIf($"Parsing tokens from input: &1{input}&r | {tokens.Count} tokens found. | Parsers: {parsers?.Count ?? 0}", DebugLogs);
        
        parsers ??= Parsers;
        
        if (parsers.Count < 1)
            throw new ArgumentException("At least one parser must be provided.", nameof(parsers));
        
        log.DebugIf($"Using {parsers.Count} parsers for tokenization.", DebugLogs);

        using (var context = new TokenContext(input, tokens))
        {
            for (var i = 0; i < input.Length; i++)
            {
                // Set context variables
                context.Index = i;
                context.CurrentChar = input[i];
                
                context.PreviousChar = i - 1 >= 0 ? input[i - 1] : null;
                context.NextChar = i + 1 < input.Length ? input[i + 1] : null;

                // Process the currently active parser.
                if (context.CurrentParser != null)
                {
                    log.DebugIf($"Processing active parser: &1{context.CurrentParser.GetType().Name}&r " +
                                $"| Current token: &1{context.CurrentToken?.GetType().Name ?? "null"}&r " +
                                $"| Current char: &1{context.CurrentChar}&r |", DebugLogs);
                    
                    // We likely hit an ending token of a parser, so just skip to the next one.
                    if (context.CurrentParser.ShouldTerminate(context))
                    {
                        log.DebugIf("Terminating active parser.", DebugLogs);
                        
                        context.TerminateToken();
                        continue;
                    }

                    if (!context.CurrentParser.ProcessContext(context))
                    {
                        log.DebugIf("Active parser returned false on context", DebugLogs);
                        continue;
                    }
                }

                var parserFoundOrPrevented = context.CurrentParser != null;
                
                log.DebugIf("Processing inactive parsers", DebugLogs);
                
                // Process other inactive parsers
                for (var x = 0; x < parsers.Count; x++)
                {
                    var parser = parsers[x];
                    
                    log.DebugIf($"Processing parser: &1{parser.GetType().Name}&r", DebugLogs);
                    
                    // Handle the start of new parsers
                    if ((context.CurrentParser == null || context.CurrentParser.AllowStart(context, parser)) 
                        && parser.ShouldStart(context))
                    {
                        log.DebugIf($"Starting new parser: &1{parser.GetType().Name}&r.", DebugLogs);
                        
                        context.TerminateToken();
                        
                        log.DebugIf("Converting trailing string to token.", DebugLogs);
            
                        if (context.EmptyBuilder.Length > 0)
                        {
                            log.DebugIf("Creating string token.", DebugLogs);
                
                            var token = StringToken.Instance.NewToken() as StringToken;

                            token.Value = context.EmptyBuilder.ToString().Trim(' ');
                            tokens.Add(token);
                
                            log.DebugIf("Token created successfully.", DebugLogs);
                
                            context.EmptyBuilder.Clear();
                        }

                        context.CurrentParser = parser;
                        context.CurrentToken = parser.CreateToken(context);

                        parserFoundOrPrevented = true;
                        break;
                    }

                    if ((context.CurrentParser == null || context.CurrentParser.AllowProcess(context, parser)) 
                        && !parser.ProcessContext(context))
                    {
                        parserFoundOrPrevented = true;
                        break;
                    }
                }
                
                log.DebugIf("Appending trailing string.", DebugLogs);

                // Let's not forget strings not handled by any parser
                // Mostly useful for commands so users don't have to use quotation marks on the last argument.
                if (!parserFoundOrPrevented)
                {
                    // Prevents leading whitespace from being added to the token.
                    if (context.EmptyBuilder.Length < 1 && context.IsCurrentWhiteSpace)
                    {
                        log.DebugIf("Skipping whitespace.", DebugLogs);
                        continue;
                    }
                    
                    // Prevents whitespaces at the end.
                    if (context is { IsCurrentWhiteSpace: true, IsEnd: true })
                    {
                        log.DebugIf("Skipping trailing whitespace.", DebugLogs);
                        continue;
                    }
                    
                    context.EmptyBuilder.Append(context.CurrentChar);
                }
            }
            
            log.DebugIf("Terminating remaining tokens.", DebugLogs);

            context.TerminateToken();

            log.DebugIf("Converting trailing string to token.", DebugLogs);
            
            if (context.EmptyBuilder.Length > 0)
            {
                log.DebugIf("Creating string token.", DebugLogs);
                
                var token = StringToken.Instance.NewToken() as StringToken;

                token.Value = context.EmptyBuilder.ToString().Trim(' ');
                tokens.Add(token);
                
                log.DebugIf("Token created successfully.", DebugLogs);
                
                context.EmptyBuilder.Clear();
            }
        }
        
        log.DebugIf($"Parsed {tokens.Count} tokens.", DebugLogs);
    }

    /// <summary>
    /// Whether or not the parser should be terminated.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual bool ShouldTerminate(TokenContext context) => false;
    
    /// <summary>
    /// Whether or not the parser should be started.
    /// </summary>
    /// <param name="context">The current parsing context.</param>
    /// <returns>true if the parser should be started.</returns>
    public virtual bool ShouldStart(TokenContext context) => false;

    /// <summary>
    /// Whether or not another parser can begin parsing while this one is active.
    /// </summary>
    /// <param name="context">The current parsing context.</param>
    /// <param name="parser">The parser that is attempting to start.</param>
    /// <returns>true if the parser should be started.</returns>
    public virtual bool AllowStart(TokenContext context, TokenParser parser) => true;
    
    /// <summary>
    /// Whether or not the parser should be allowed to process the current character.
    /// </summary>
    /// <param name="context">The current parsing context.</param>
    /// <param name="parser">The parser that is attempting to process the character.</param>
    /// <returns>true if the parser should be allowed to process the character.</returns>
    public virtual bool AllowProcess(TokenContext context, TokenParser parser) => true;

    /// <summary>
    /// Called once per each character before <see cref="ShouldTerminate"/> and after <see cref="ShouldStart"/>.
    /// </summary>
    /// <param name="context">The current parsing context.</param>
    /// <returns>true if the loop should be allowed to continue</returns>
    public virtual bool ProcessContext(TokenContext context) => false;
    
    /// <summary>
    /// Called before a token is terminated.
    /// </summary>
    /// <param name="context">The current parsing context.</param>
    /// <returns>true if the token should be terminated.</returns>
    public virtual bool OnTerminating(TokenContext context) => true;
    
    /// <summary>
    /// Called after a token is terminated.
    /// </summary>
    /// <param name="context">The current parsing context.</param>
    public virtual void OnTerminated(TokenContext context) { }
    
    /// <summary>
    /// Gets a new instance of the parser's token.
    /// </summary>
    /// <param name="context">The current parsing context.</param>
    /// <returns>The created token instance.</returns>
    public abstract Token CreateToken(TokenContext context);

    internal static void Initialize()
    {
        StaticConstructor<StringToken>.Set(() => new());
        StaticConstructor<CollectionToken>.Set(() => new());
        StaticConstructor<DictionaryToken>.Set(() => new());
    }
}