using System.Reflection;

using NiveraAPI.IO.Serialization;

using NiveraAPI.Pooling;
using NiveraAPI.Utilities;
using NiveraAPI.Extensions;

namespace NiveraAPI.IO.Network.Entities;

/// <summary>
/// Represents a remote method that can be invoked over a network interface.
/// </summary>
public class RemoteMethod
{
    /// <summary>
    /// Represents the unique identifier for the remote method.
    /// This identifier is used to distinguish and reference the method during invocation.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Indicates whether the remote method returns a value upon execution.
    /// If true, the method produces a return value that will be written to the output serialization stream.
    /// If false, the method has no return value, and the output stream remains unused or null.
    /// </summary>
    public bool HasReturnValue { get; }

    /// <summary>
    /// Gets the delegate that represents the execution logic of the remote method.
    /// This delegate is invoked with input and output serialization streams.
    /// 
    /// The input parameter represents the deserialization stream that reads input arguments.
    /// The output parameter, if applicable, represents the serialization stream to write return values.
    /// </summary>
    public Action<ByteReader, ByteWriter?> Invoke { get; }
    
    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/>.
    /// </summary>
    /// <param name="id">The remote method identifier.</param>
    /// <param name="hasReturnValue">Indicates whether the remote method has a return value.</param>
    /// <param name="invoke">The method to invoke when the remote method is called.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> or <paramref name="invoke"/> is null.</exception>
    public RemoteMethod(string id, bool hasReturnValue, Action<ByteReader, ByteWriter?> invoke)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));
        
        HasReturnValue = hasReturnValue;
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="RemoteMethod"/> with a return value.
    /// </summary>
    /// <param name="id">The identifier for the remote method. Cannot be null or empty.</param>
    /// <param name="invoke">The method to invoke when the remote method is called, which includes both a <see cref="ByteReader"/> for reading input and a <see cref="ByteWriter"/> for writing output.</param>
    /// <returns>A new instance of <see cref="RemoteMethod"/> configured with a return value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty or null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod Return(string id, Action<ByteReader, ByteWriter> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);
        
        return new(id, true, invoke!);
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> without a return value.
    /// </summary>
    /// <param name="id">The identifier for the remote method.</param>
    /// <param name="invoke">
    /// The action to invoke when the remote method is called.
    /// It receives a <see cref="ByteReader"/> instance as an argument for data reading.
    /// </param>
    /// <returns>A new instance of <see cref="RemoteMethod"/> configured without a return value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> or <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod NoReturn(string id, Action<ByteReader> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);
        
        return new(id, false, (reader, _) => invoke(reader));
    }

    /// <summary>
    /// Creates an instance of <see cref="RemoteMethod"/> by resolving a method from the specified type and instance.
    /// </summary>
    /// <param name="id">The identifier for the remote method.</param>
    /// <param name="instance">The instance containing the method to be invoked.</param>
    /// <param name="type">The type to search for the specified method.</param>
    /// <param name="name">The name of the method to be resolved.</param>
    /// <returns>An instance of <see cref="RemoteMethod"/> configured for the resolved method.</returns>
    /// <exception cref="ArgumentException">Thrown if the method cannot be found in the specified type.</exception>
    public static RemoteMethod Anonymous(string id, object instance, Type type, string name)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(type), type);
        Exceptions.EmptyArgument(nameof(name), name);

        var method = type.FindMethod(name);
        
        if (method == null)
            throw new ArgumentException($"Method '{name}' not found in type '{type.FullName}'.");

        return Anonymous(id, instance, method);
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> based on the provided method information.
    /// </summary>
    /// <param name="id">The unique identifier for the remote method.</param>
    /// <param name="instance">The object instance on which the method is invoked. This can be null for static methods.</param>
    /// <param name="method">The method information to be used for creating the remote method.</param>
    /// <returns>A new instance of <see cref="RemoteMethod"/> configured to invoke the specified method.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> or <paramref name="method"/> is null.</exception>
    /// <exception cref="Exception">
    /// Thrown when a serializer is not available for a parameter or return type of the method.
    /// </exception>
    public static RemoteMethod Anonymous(string id, object instance, MethodInfo method)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(method), method);

        var parameters = method.GetAllParameters();
        
        var readers = new Delegate[parameters.Length];
        var writer = default(Delegate);

        foreach (var parameter in parameters)
        {
            var cache = typeof(ByteSerializer<>).MakeGenericType(parameter.ParameterType);
            var reader = cache.GetField("Deserialize").GetValue(null) as Delegate;

            if (reader == null)
                throw new Exception($"No serializer found for type {parameter.ParameterType}");
            
            readers[parameter.Position] = reader;
        }

        if (method.ReturnType != typeof(void))
        {
            var cache = typeof(ByteSerializer<>).MakeGenericType(method.ReturnType);
            var field = cache.GetField("Serialize").GetValue(null) as Delegate;
            
            if (field == null)
                throw new Exception($"No serializer found for type {method.ReturnType}");

            writer = field;
        }
        
        var argPool = new FixedArrayPool<object>(parameters.Length);

        return new RemoteMethod(id, writer != null, (reader, x) =>
        {
            var args = argPool.Rent();
            
            try
            {
                for (var y = 0; y < readers.Length; y++)
                {
                    args[y] = readers[y].DynamicInvoke(reader);
                }

                var result = method.Invoke(instance, args);

                writer?.DynamicInvoke(writer, result);
            }
            finally
            {
                argPool.Return(args);
            }
        });
    }

    #region No Return Delegates
    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> with a single argument.
    /// </summary>
    /// <typeparam name="TArg">The type of the argument to be read and passed to the method.</typeparam>
    /// <param name="id">The identifier for the remote method.</param>
    /// <param name="invoke">The action to invoke when the remote method is called, taking the argument of type <typeparamref name="TArg"/>.</param>
    /// <returns>A new instance of <see cref="RemoteMethod"/> configured with the specified identifier and invocation action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> or <paramref name="invoke"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty or whitespace.</exception>
    public static RemoteMethod Create<TArg>(string id, Action<TArg?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, false, (reader, _) =>
        {   
            invoke(reader.Read<TArg>());
        });
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> with two arguments.
    /// </summary>
    /// <param name="id">The identifier of the remote method.</param>
    /// <param name="invoke">The delegate to invoke when the remote method is called, accepting two arguments of types <typeparamref name="TArg1"/> and <typeparamref name="TArg2"/>.</param>
    /// <typeparam name="TArg1">The type of the first argument to be passed to the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument to be passed to the remote method.</typeparam>
    /// <returns>A <see cref="RemoteMethod"/> instance configured with the provided identifier and invocation logic.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> or <paramref name="invoke"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty or whitespace.</exception>
    public static RemoteMethod Create<TArg1, TArg2>(string id, Action<TArg1?, TArg2?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, false, (reader, _) =>
        {   
            invoke(reader.Read<TArg1>(), 
                reader.Read<TArg2>());
        });
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> configured to handle a remote method invocation with three arguments.
    /// </summary>
    /// <param name="id">The unique identifier for this remote method.</param>
    /// <param name="invoke">The method to invoke when the remote method is called, accepting three arguments of types <typeparamref name="TArg1"/>, <typeparamref name="TArg2"/>, and <typeparamref name="TArg3"/>.</param>
    /// <typeparam name="TArg1">The type of the first argument for the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument for the remote method.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument for the remote method.</typeparam>
    /// <returns>A configured instance of <see cref="RemoteMethod"/> capable of handling invocations with three arguments.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> or <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod Create<TArg1, TArg2, TArg3>(string id, Action<TArg1?, TArg2?, TArg3?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, false, (reader, _) =>
        {   
            invoke(reader.Read<TArg1>(), 
                reader.Read<TArg2>(), 
                reader.Read<TArg3>());
        });
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> configured to handle a method with four typed arguments.
    /// </summary>
    /// <param name="id">The identifier of the remote method.</param>
    /// <param name="invoke">The delegate to invoke when the remote method is called, which accepts four arguments of the specified types.</param>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <returns>A configured <see cref="RemoteMethod"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod Create<TArg1, TArg2, TArg3, TArg4>(string id, Action<TArg1?, TArg2?, TArg3?, TArg4?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, false, (reader, _) =>
        {
            invoke(reader.Read<TArg1>(), 
                reader.Read<TArg2>(), 
                reader.Read<TArg3>(), 
                reader.Read<TArg4>());
        });
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> representing a remote method that supports five arguments.
    /// </summary>
    /// <param name="id">The unique identifier of the remote method.</param>
    /// <param name="invoke">The delegate to be invoked when the remote method is called, accepting five optional arguments.</param>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <returns>A new instance of <see cref="RemoteMethod"/> configured with the specified identifier and invocation behavior.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty or contains only whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod Create<TArg1, TArg2, TArg3, TArg4, TArg5>(string id, Action<TArg1?, TArg2?, TArg3?, TArg4?, TArg5?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, false, (reader, _) =>
        {
            invoke(reader.Read<TArg1>(),
                reader.Read<TArg2>(), 
                reader.Read<TArg3>(), 
                reader.Read<TArg4>(), 
                reader.Read<TArg5>());
        });
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> with six generic arguments.
    /// </summary>
    /// <param name="id">The unique identifier for the remote method.</param>
    /// <param name="invoke">The action to invoke when the remote method is called, accepting six arguments.</param>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <returns>A new <see cref="RemoteMethod"/> instance configured with the specified identifier and action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> or <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod Create<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(string id, Action<TArg1?, TArg2?, TArg3?, TArg4?, TArg5?, TArg6?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, false, (reader, _) =>
        {
            invoke(reader.Read<TArg1>(),
                reader.Read<TArg2>(),
                reader.Read<TArg3>(), 
                reader.Read<TArg4>(), 
                reader.Read<TArg5>(), 
                reader.Read<TArg6>());
        });
    }
    #endregion

    #region Return Delegates
    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> for a function with one argument and a return value.
    /// </summary>
    /// <typeparam name="TReturn">The type of the return value.</typeparam>
    /// <typeparam name="TArg">The type of the argument.</typeparam>
    /// <param name="id">The unique identifier for the remote method.</param>
    /// <param name="invoke">The function to invoke when the remote method is called.</param>
    /// <returns>A new instance of <see cref="RemoteMethod"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="id"/> or <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod Create<TReturn, TArg>(string id, Func<TArg?, TReturn?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, true, (reader, writer) =>
        {   
            var result = invoke(reader.Read<TArg>());
            
            writer?.Write(result);
        });
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> with a return value and two input arguments.
    /// </summary>
    /// <typeparam name="TReturn">The type of the return value.</typeparam>
    /// <typeparam name="TArg1">The type of the first input argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second input argument.</typeparam>
    /// <param name="id">The unique identifier for the remote method.</param>
    /// <param name="invoke">The function to execute when the remote method is invoked.</param>
    /// <returns>A new instance of <see cref="RemoteMethod"/> configured with the specified parameters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> or <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod Create<TReturn, TArg1, TArg2>(string id, Func<TArg1?, TArg2?, TReturn?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, true, (reader, writer) =>
        {   
            var result = invoke(reader.Read<TArg1>(),
                reader.Read<TArg2>());
            
            writer?.Write(result);
        });
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> that can invoke a method with three arguments
    /// and return a result.
    /// </summary>
    /// <param name="id">The unique identifier for the remote method.</param>
    /// <param name="invoke">The function to be invoked when the remote method is called. Takes three arguments
    /// of types <typeparamref name="TArg1"/>, <typeparamref name="TArg2"/>, and <typeparamref name="TArg3"/>,
    /// and returns a result of type <typeparamref name="TReturn"/>.</param>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TReturn">The type of the return value.</typeparam>
    /// <returns>A new instance of <see cref="RemoteMethod"/> configured to invoke the specified function.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod Create<TReturn, TArg1, TArg2, TArg3>(string id, Func<TArg1?, TArg2?, TArg3?, TReturn?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, true, (reader, writer) =>
        {
            var result = invoke(reader.Read<TArg1>(),
                reader.Read<TArg2>(),
                reader.Read<TArg3>());
            
            writer?.Write(result);
        });
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> configured to invoke a remote method with four arguments and a return value.
    /// </summary>
    /// <typeparam name="TReturn">The type of the return value for the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument for the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument for the remote method.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument for the remote method.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument for the remote method.</typeparam>
    /// <param name="id">The identifier of the remote method.</param>
    /// <param name="invoke">The function to invoke when the remote method is called, which accepts four arguments of types
    /// <typeparamref name="TArg1"/>, <typeparamref name="TArg2"/>, <typeparamref name="TArg3"/>,
    /// <typeparamref name="TArg4"/> and returns a value of type <typeparamref name="TReturn"/>.</param>
    /// <returns>A new instance of <see cref="RemoteMethod"/> configured for the specified arguments and return value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null, empty, or whitespace,
    /// or when <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod Create<TReturn, TArg1, TArg2, TArg3, TArg4>(string id, Func<TArg1?, TArg2?, TArg3?, TArg4?, TReturn?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, true, (reader, writer) =>
        {
            var result = invoke(reader.Read<TArg1>(),
                reader.Read<TArg2>(),
                reader.Read<TArg3>(),
                reader.Read<TArg4>());
            
            writer?.Write(result);
        });
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> that supports execution with five arguments and a return value.
    /// </summary>
    /// <typeparam name="TReturn">The type of the return value.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <param name="id">The identifier for the remote method.</param>
    /// <param name="invoke">The function to invoke when the remote method is called.</param>
    /// <returns>A new instance of <see cref="RemoteMethod"/> configured with the specified identifier and invocation function.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty or null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod Create<TReturn, TArg1, TArg2, TArg3, TArg4, TArg5>(string id, Func<TArg1?, TArg2?, TArg3?, TArg4?, TArg5?, TReturn?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, true, (reader, writer) =>
        {
            var result = invoke(reader.Read<TArg1>(),
                reader.Read<TArg2>(),
                reader.Read<TArg3>(),
                reader.Read<TArg4>(),
                reader.Read<TArg5>());
            
            writer?.Write(result);
        });
    }

    /// <summary>
    /// Creates a new instance of <see cref="RemoteMethod"/> with six arguments and a return value.
    /// </summary>
    /// <param name="id">The unique identifier of the remote method.</param>
    /// <param name="invoke">The function to invoke when the remote method is called, taking six arguments and returning a value.</param>
    /// <typeparam name="TReturn">The return type of the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <returns>Returns a new instance of <see cref="RemoteMethod"/> configured for six arguments and a return value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> or <paramref name="invoke"/> is null.</exception>
    public static RemoteMethod Create<TReturn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(string id, Func<TArg1?, TArg2?, TArg3?, TArg4?, TArg5?, TArg6?, TReturn?> invoke)
    {
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(invoke), invoke);

        return new(id, true, (reader, writer) =>
        {
            var result = invoke(reader.Read<TArg1>(),
                reader.Read<TArg2>(),
                reader.Read<TArg3>(),
                reader.Read<TArg4>(),
                reader.Read<TArg5>(),
                reader.Read<TArg6>());
            
            writer?.Write(result);
        });
    }
    #endregion
}