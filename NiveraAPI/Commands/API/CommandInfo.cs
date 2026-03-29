namespace NiveraAPI.Commands.API;

/// <summary>
/// Represents metadata and operational details for a command.
/// </summary>
/// <typeparam name="TSender">
/// The type of the sender or context that is associated with the command.
/// </typeparam>
public class CommandInfo<TSender> where TSender : class
{
    /// <summary>
    /// Gets the name of the command.
    /// </summary>
    public string[] Name { get; }

    /// <summary>
    /// Gets the full name of the command.
    /// </summary>
    public string FullName { get; }
    
    /// <summary>
    /// Gets the declaring type.
    /// </summary>
    public Type Type { get; }
    
    /// <summary>
    /// Gets the manager that registered this command.
    /// </summary>
    public CommandManager<TSender> Manager { get; }

    /// <summary>
    /// Gets the collection of additional command flags.
    /// </summary>
    public List<object> Flags { get; } = new();

    /// <summary>
    /// Gets the collection of registered overloads.
    /// </summary>
    public List<CommandOverload<TSender>> Overloads { get; } = new();

    /// <summary>
    /// Gets or sets the constructor used to create instances of the command.
    /// </summary>
    public CommandManager<TSender>.TypeConstructor? Constructor { get; set; }

    /// <summary>
    /// Gets a value indicating whether all overloads are static.
    /// </summary>
    public bool AllStatic => Overloads.All(o => o.Method.IsStatic);

    /// <summary>
    /// Creates a new instance of <see cref="CommandInfo{TSender}"/>.
    /// </summary>
    /// <param name="name">The names of the command.</param>
    /// <param name="type">The type that declares the command.</param>
    /// <param name="manager">The manager that registered this command.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when the name array is empty.</exception>
    public CommandInfo(string[] name, Type type, CommandManager<TSender> manager)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        
        FullName = string.Join(" ", name);
    }
}