using NiveraAPI.IO.Network.Entities.Synchronization.Objects;
using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Network.Entities.Synchronization;

/// <summary>
/// Provides extension methods for synchronization objects to interact with remote entities and invoke operations.
/// </summary>
public static class SyncExtensions
{
    /// <summary>
    /// Removes a remote handler associated with the specified identifier on the synchronization object and invokes a callback with the result.
    /// </summary>
    /// <param name="syncObject">The synchronization object from which the handler will be removed.</param>
    /// <param name="id">The unique identifier of the handler to be removed.</param>
    /// <param name="callback">The callback function invoked with the result of the removal operation.</param>
    /// <typeparam name="TReturn">The type of the result returned after the handler is invoked remotely.</typeparam>
    public static void GetRemote<TReturn>(this SyncParent syncObject, string id, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);
        
        var callbackId = Guid.NewGuid().ToString();
        
        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader =>
        {
            callback(reader.Read<TReturn>());
        }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(callbackId);
        });
    }

    /// <summary>
    /// Executes a remote method on the specified synchronization object, using the provided method identifier, input argument,
    /// and callback function to handle the result of the remote invocation.
    /// </summary>
    /// <param name="syncObject">The synchronization object representing the entity on which the remote method will be invoked.</param>
    /// <param name="id">The unique identifier of the remote method to be executed.</param>
    /// <param name="value">The argument to be passed to the remote method.</param>
    /// <param name="callback">The callback function invoked with the result of the remote method execution.</param>
    /// <typeparam name="TReturn">The type of the result returned by the remote method.</typeparam>
    /// <typeparam name="TArg">The type of the argument to be passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg>(this SyncParent syncObject, string id, TArg value, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);
        
        var callbackId = Guid.NewGuid().ToString();
        
        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader =>
        {
            callback(reader.Read<TReturn>());
        }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(callbackId);
            
            writer.Write(value);
        });
    }

    /// <summary>
    /// Invokes a remote method on the specified synchronization object, identified by the given method identifier and arguments,
    /// and handles the response using the provided callback function.
    /// </summary>
    /// <param name="syncObject">The synchronization object that represents the entity on which the remote method will be invoked.</param>
    /// <param name="id">The unique identifier of the remote method to be executed.</param>
    /// <param name="arg1">The first argument to be passed to the remote method.</param>
    /// <param name="arg2">The second argument to be passed to the remote method.</param>
    /// <param name="callback">The callback function invoked with the result of the remote method execution.</param>
    /// <typeparam name="TReturn">The type of the result returned by the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument to be passed to the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument to be passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg1, TArg2>(this SyncParent syncObject, string id, TArg1 arg1, TArg2 arg2, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);

        var callbackId = Guid.NewGuid().ToString();
        
        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader =>
        {
            callback(reader.Read<TReturn>());
        }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(callbackId);
            
            writer.Write(arg1);
            writer.Write(arg2);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the given synchronization object,
    /// identified by the specified method identifier and arguments, and handles the response
    /// using the provided callback function.
    /// </summary>
    /// <param name="syncObject">The synchronization object that represents the entity on which the remote method will be invoked.</param>
    /// <param name="id">The unique identifier of the remote method to be executed.</param>
    /// <param name="arg1">The first argument to be passed to the remote method.</param>
    /// <param name="arg2">The second argument to be passed to the remote method.</param>
    /// <param name="arg3">The third argument to be passed to the remote method.</param>
    /// <param name="callback">The callback function invoked with the result of the remote method execution.</param>
    /// <typeparam name="TReturn">The type of the result returned by the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument to be passed to the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument to be passed to the remote method.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument to be passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg1, TArg2, TArg3>(this SyncParent syncObject, string id, TArg1 arg1, TArg2 arg2, TArg3 arg3, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);

        var callbackId = Guid.NewGuid().ToString();

        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader =>
        {
            callback(reader.Read<TReturn>());
        }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(callbackId);
            
            writer.Write(arg1);
            writer.Write(arg2);
            writer.Write(arg3);
        });
    }

    /// <summary>
    /// Calls a remote method on the entity associated with the specified synchronization object,
    /// using the provided identifier and arguments, and executes the callback with the result.
    /// </summary>
    /// <param name="syncObject">The synchronization object that identifies the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to be invoked.</param>
    /// <param name="arg1">The first argument passed to the remote method.</param>
    /// <param name="arg2">The second argument passed to the remote method.</param>
    /// <param name="arg3">The third argument passed to the remote method.</param>
    /// <param name="arg4">The fourth argument passed to the remote method.</param>
    /// <param name="callback">The callback to execute upon receiving the result of the remote method invocation.</param>
    /// <typeparam name="TReturn">The type of the result returned by the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg1, TArg2, TArg3, TArg4>(this SyncParent syncObject, string id, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);

        var callbackId = Guid.NewGuid().ToString();

        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader =>
        {
            callback(reader.Read<TReturn>());
        }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(callbackId);
            
            writer.Write(arg1);
            writer.Write(arg2);
            writer.Write(arg3);
            writer.Write(arg4);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object,
    /// passing the provided identifier and arguments, and invokes the specified callback with the result.
    /// </summary>
    /// <param name="syncObject">The synchronization object associated with the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to execute.</param>
    /// <param name="arg1">The first argument to provide to the remote method.</param>
    /// <param name="arg2">The second argument to provide to the remote method.</param>
    /// <param name="arg3">The third argument to provide to the remote method.</param>
    /// <param name="arg4">The fourth argument to provide to the remote method.</param>
    /// <param name="arg5">The fifth argument to provide to the remote method.</param>
    /// <param name="callback">The callback to execute with the result of the remote method invocation.</param>
    /// <typeparam name="TReturn">The type of the result returned by the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg1, TArg2, TArg3, TArg4, TArg5>(this SyncParent syncObject, string id, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);

        var callbackId = Guid.NewGuid().ToString();

        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader => { callback(reader.Read<TReturn>()); }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(callbackId);
            
            writer.Write(arg1);
            writer.Write(arg2);
            writer.Write(arg3);
            writer.Write(arg4);
            writer.Write(arg5);
        });
    }

    /// <summary>
    /// Retrieves a remote method's result from the entity associated with the specified synchronization object,
    /// using the provided identifier and arguments, and invokes the callback upon completion.
    /// </summary>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="arg1">The first argument to pass to the remote method.</param>
    /// <param name="arg2">The second argument to pass to the remote method.</param>
    /// <param name="arg3">The third argument to pass to the remote method.</param>
    /// <param name="arg4">The fourth argument to pass to the remote method.</param>
    /// <param name="arg5">The fifth argument to pass to the remote method.</param>
    /// <param name="arg6">The sixth argument to pass to the remote method.</param>
    /// <param name="callback">The callback to invoke with the result returned by the remote method.</param>
    /// <typeparam name="TReturn">The type of the result being returned by the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this SyncParent syncObject, string id, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);

        var callbackId = Guid.NewGuid().ToString();

        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader => { callback(reader.Read<TReturn>()); }));

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(callbackId);
            
            writer.Write(arg1);
            writer.Write(arg2);
            writer.Write(arg3);
            writer.Write(arg4);
            writer.Write(arg5);
            writer.Write(arg6);
        });
    }
    
    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object
    /// using the provided identifier.
    /// </summary>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    public static void InvokeRemote(this SyncParent syncObject, string id)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(string.Empty);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object,
    /// using the provided identifier and one additional argument.
    /// </summary>
    /// <typeparam name="TArg">The type of the argument to pass to the remote endpoint.</typeparam>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value">The argument to pass to the remote endpoint.</param>
    public static void InvokeRemote<TArg>(this SyncParent syncObject, string id, TArg? value)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(string.Empty);
            
            writer.Write(value);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object,
    /// using the provided identifier and arguments.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value1">The first argument to pass to the remote endpoint.</param>
    /// <param name="value2">The second argument to pass to the remote endpoint.</param>
    public static void InvokeRemote<TArg1, TArg2>(this SyncParent syncObject, string id, TArg1? value1, TArg2? value2)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(string.Empty);
            
            writer.Write(value1);
            writer.Write(value2);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object.
    /// The method transmits data to the remote endpoint using the provided parameters.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value1">The first argument to pass to the remote endpoint.</param>
    /// <param name="value2">The second argument to pass to the remote endpoint.</param>
    /// <param name="value3">The third argument to pass to the remote endpoint.</param>
    public static void InvokeRemote<TArg1, TArg2, TArg3>(this SyncParent syncObject, string id, TArg1? value1, TArg2? value2, TArg3? value3)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(string.Empty);
            
            writer.Write(value1);
            writer.Write(value2);
            writer.Write(value3);
        });
    }
    
    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object.
    /// The method transmits data to the remote endpoint using the provided parameters.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value1">The first argument to pass to the remote endpoint.</param>
    /// <param name="value2">The second argument to pass to the remote endpoint.</param>
    /// <param name="value3">The third argument to pass to the remote endpoint.</param>
    /// <param name="value4">The fourth argument to pass to the remote endpoint.</param>
    public static void InvokeRemote<TArg1, TArg2, TArg3, TArg4>(this SyncParent syncObject, string id, TArg1? value1, TArg2? value2, TArg3? value3, TArg4? value4)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(string.Empty);
            
            writer.Write(value1);
            writer.Write(value2);
            writer.Write(value3);
            writer.Write(value4);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object.
    /// The method transmits data to the remote endpoint using the provided parameters.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value1">The first argument to pass to the remote endpoint.</param>
    /// <param name="value2">The second argument to pass to the remote endpoint.</param>
    /// <param name="value3">The third argument to pass to the remote endpoint.</param>
    /// <param name="value4">The fourth argument to pass to the remote endpoint.</param>
    /// <param name="value5">The fifth argument to pass to the remote endpoint.</param>
    public static void InvokeRemote<TArg1, TArg2, TArg3, TArg4, TArg5>(this SyncParent syncObject, string id, TArg1? value1, TArg2? value2, TArg3? value3, TArg4? value4, TArg5? value5)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(string.Empty);

            writer.Write(value1);
            writer.Write(value2);
            writer.Write(value3);
            writer.Write(value4);
            writer.Write(value5);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object.
    /// The method sends data to the remote endpoint, identified by a unique identifier.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <param name="syncObject">The synchronization object associated with the entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value1">The first argument to send to the remote endpoint.</param>
    /// <param name="value2">The second argument to send to the remote endpoint.</param>
    /// <param name="value3">The third argument to send to the remote endpoint.</param>
    /// <param name="value4">The fourth argument to send to the remote endpoint.</param>
    /// <param name="value5">The fifth argument to send to the remote endpoint.</param>
    /// <param name="value6">The sixth argument to send to the remote endpoint.</param>
    public static void InvokeRemote<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this SyncParent syncObject, string id, TArg1? value1, TArg2? value2, TArg3? value3, TArg4? value4, TArg5? value5, TArg6? value6)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(string.Empty);
            writer.WriteString(string.Empty);

            writer.Write(value1);
            writer.Write(value2);
            writer.Write(value3);
            writer.Write(value4);
            writer.Write(value5);
            writer.Write(value6);
        });
    }
    
    /// <summary>
    /// Removes a remote handler associated with the specified identifier on the synchronization object and invokes a callback with the result.
    /// </summary>
    /// <param name="syncObject">The synchronization object from which the handler will be removed.</param>
    /// <param name="id">The unique identifier of the handler to be removed.</param>
    /// <param name="callback">The callback function invoked with the result of the removal operation.</param>
    /// <typeparam name="TReturn">The type of the result returned after the handler is invoked remotely.</typeparam>
    public static void GetRemote<TReturn>(this SyncObject syncObject, string id, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);
        
        var callbackId = Guid.NewGuid().ToString();
        
        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader =>
        {
            callback(reader.Read<TReturn>());
        }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(callbackId);
        });
    }

    /// <summary>
    /// Executes a remote method on the specified synchronization object, using the provided method identifier, input argument,
    /// and callback function to handle the result of the remote invocation.
    /// </summary>
    /// <param name="syncObject">The synchronization object representing the entity on which the remote method will be invoked.</param>
    /// <param name="id">The unique identifier of the remote method to be executed.</param>
    /// <param name="value">The argument to be passed to the remote method.</param>
    /// <param name="callback">The callback function invoked with the result of the remote method execution.</param>
    /// <typeparam name="TReturn">The type of the result returned by the remote method.</typeparam>
    /// <typeparam name="TArg">The type of the argument to be passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg>(this SyncObject syncObject, string id, TArg value, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);
        
        var callbackId = Guid.NewGuid().ToString();
        
        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader =>
        {
            callback(reader.Read<TReturn>());
        }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(callbackId);
            
            writer.Write(value);
        });
    }

    /// <summary>
    /// Invokes a remote method on the specified synchronization object, identified by the given method identifier and arguments,
    /// and handles the response using the provided callback function.
    /// </summary>
    /// <param name="syncObject">The synchronization object that represents the entity on which the remote method will be invoked.</param>
    /// <param name="id">The unique identifier of the remote method to be executed.</param>
    /// <param name="arg1">The first argument to be passed to the remote method.</param>
    /// <param name="arg2">The second argument to be passed to the remote method.</param>
    /// <param name="callback">The callback function invoked with the result of the remote method execution.</param>
    /// <typeparam name="TReturn">The type of the result returned by the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument to be passed to the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument to be passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg1, TArg2>(this SyncObject syncObject, string id, TArg1 arg1, TArg2 arg2, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);

        var callbackId = Guid.NewGuid().ToString();
        
        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader =>
        {
            callback(reader.Read<TReturn>());
        }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(callbackId);
            
            writer.Write(arg1);
            writer.Write(arg2);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the given synchronization object,
    /// identified by the specified method identifier and arguments, and handles the response
    /// using the provided callback function.
    /// </summary>
    /// <param name="syncObject">The synchronization object that represents the entity on which the remote method will be invoked.</param>
    /// <param name="id">The unique identifier of the remote method to be executed.</param>
    /// <param name="arg1">The first argument to be passed to the remote method.</param>
    /// <param name="arg2">The second argument to be passed to the remote method.</param>
    /// <param name="arg3">The third argument to be passed to the remote method.</param>
    /// <param name="callback">The callback function invoked with the result of the remote method execution.</param>
    /// <typeparam name="TReturn">The type of the result returned by the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument to be passed to the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument to be passed to the remote method.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument to be passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg1, TArg2, TArg3>(this SyncObject syncObject, string id, TArg1 arg1, TArg2 arg2, TArg3 arg3, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);

        var callbackId = Guid.NewGuid().ToString();

        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader =>
        {
            callback(reader.Read<TReturn>());
        }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(callbackId);
            
            writer.Write(arg1);
            writer.Write(arg2);
            writer.Write(arg3);
        });
    }

    /// <summary>
    /// Calls a remote method on the entity associated with the specified synchronization object,
    /// using the provided identifier and arguments, and executes the callback with the result.
    /// </summary>
    /// <param name="syncObject">The synchronization object that identifies the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to be invoked.</param>
    /// <param name="arg1">The first argument passed to the remote method.</param>
    /// <param name="arg2">The second argument passed to the remote method.</param>
    /// <param name="arg3">The third argument passed to the remote method.</param>
    /// <param name="arg4">The fourth argument passed to the remote method.</param>
    /// <param name="callback">The callback to execute upon receiving the result of the remote method invocation.</param>
    /// <typeparam name="TReturn">The type of the result returned by the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg1, TArg2, TArg3, TArg4>(this SyncObject syncObject, string id, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);

        var callbackId = Guid.NewGuid().ToString();

        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader =>
        {
            callback(reader.Read<TReturn>());
        }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(callbackId);
            
            writer.Write(arg1);
            writer.Write(arg2);
            writer.Write(arg3);
            writer.Write(arg4);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object,
    /// passing the provided identifier and arguments, and invokes the specified callback with the result.
    /// </summary>
    /// <param name="syncObject">The synchronization object associated with the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to execute.</param>
    /// <param name="arg1">The first argument to provide to the remote method.</param>
    /// <param name="arg2">The second argument to provide to the remote method.</param>
    /// <param name="arg3">The third argument to provide to the remote method.</param>
    /// <param name="arg4">The fourth argument to provide to the remote method.</param>
    /// <param name="arg5">The fifth argument to provide to the remote method.</param>
    /// <param name="callback">The callback to execute with the result of the remote method invocation.</param>
    /// <typeparam name="TReturn">The type of the result returned by the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg1, TArg2, TArg3, TArg4, TArg5>(this SyncObject syncObject, string id, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);

        var callbackId = Guid.NewGuid().ToString();

        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader => { callback(reader.Read<TReturn>()); }));
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(callbackId);
            
            writer.Write(arg1);
            writer.Write(arg2);
            writer.Write(arg3);
            writer.Write(arg4);
            writer.Write(arg5);
        });
    }

    /// <summary>
    /// Retrieves a remote method's result from the entity associated with the specified synchronization object,
    /// using the provided identifier and arguments, and invokes the callback upon completion.
    /// </summary>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="arg1">The first argument to pass to the remote method.</param>
    /// <param name="arg2">The second argument to pass to the remote method.</param>
    /// <param name="arg3">The third argument to pass to the remote method.</param>
    /// <param name="arg4">The fourth argument to pass to the remote method.</param>
    /// <param name="arg5">The fifth argument to pass to the remote method.</param>
    /// <param name="arg6">The sixth argument to pass to the remote method.</param>
    /// <param name="callback">The callback to invoke with the result returned by the remote method.</param>
    /// <typeparam name="TReturn">The type of the result being returned by the remote method.</typeparam>
    /// <typeparam name="TArg1">The type of the first argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument passed to the remote method.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument passed to the remote method.</typeparam>
    public static void GetRemote<TReturn, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this SyncObject syncObject, string id, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, Action<TReturn?> callback)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        Exceptions.NullArgument(nameof(callback), callback);

        var callbackId = Guid.NewGuid().ToString();

        syncObject.ResponseHandlers.Add(new(callbackId, DateTime.UtcNow, reader => { callback(reader.Read<TReturn>()); }));

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(callbackId);
            
            writer.Write(arg1);
            writer.Write(arg2);
            writer.Write(arg3);
            writer.Write(arg4);
            writer.Write(arg5);
            writer.Write(arg6);
        });
    }
    
    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object
    /// using the provided identifier.
    /// </summary>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    public static void InvokeRemote(this SyncObject syncObject, string id)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(string.Empty);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object,
    /// using the provided identifier and one additional argument.
    /// </summary>
    /// <typeparam name="TArg">The type of the argument to pass to the remote endpoint.</typeparam>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value">The argument to pass to the remote endpoint.</param>
    public static void InvokeRemote<TArg>(this SyncObject syncObject, string id, TArg? value)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);
        
        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(string.Empty);
            
            writer.Write(value);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object,
    /// using the provided identifier and arguments.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value1">The first argument to pass to the remote endpoint.</param>
    /// <param name="value2">The second argument to pass to the remote endpoint.</param>
    public static void InvokeRemote<TArg1, TArg2>(this SyncObject syncObject, string id, TArg1? value1, TArg2? value2)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(string.Empty);
            
            writer.Write(value1);
            writer.Write(value2);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object.
    /// The method transmits data to the remote endpoint using the provided parameters.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value1">The first argument to pass to the remote endpoint.</param>
    /// <param name="value2">The second argument to pass to the remote endpoint.</param>
    /// <param name="value3">The third argument to pass to the remote endpoint.</param>
    public static void InvokeRemote<TArg1, TArg2, TArg3>(this SyncObject syncObject, string id, TArg1? value1, TArg2? value2, TArg3? value3)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(string.Empty);
            
            writer.Write(value1);
            writer.Write(value2);
            writer.Write(value3);
        });
    }
    
    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object.
    /// The method transmits data to the remote endpoint using the provided parameters.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value1">The first argument to pass to the remote endpoint.</param>
    /// <param name="value2">The second argument to pass to the remote endpoint.</param>
    /// <param name="value3">The third argument to pass to the remote endpoint.</param>
    /// <param name="value4">The fourth argument to pass to the remote endpoint.</param>
    public static void InvokeRemote<TArg1, TArg2, TArg3, TArg4>(this SyncObject syncObject, string id, TArg1? value1, TArg2? value2, TArg3? value3, TArg4? value4)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(string.Empty);
            
            writer.Write(value1);
            writer.Write(value2);
            writer.Write(value3);
            writer.Write(value4);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object.
    /// The method transmits data to the remote endpoint using the provided parameters.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <param name="syncObject">The synchronization object representing the target entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value1">The first argument to pass to the remote endpoint.</param>
    /// <param name="value2">The second argument to pass to the remote endpoint.</param>
    /// <param name="value3">The third argument to pass to the remote endpoint.</param>
    /// <param name="value4">The fourth argument to pass to the remote endpoint.</param>
    /// <param name="value5">The fifth argument to pass to the remote endpoint.</param>
    public static void InvokeRemote<TArg1, TArg2, TArg3, TArg4, TArg5>(this SyncObject syncObject, string id, TArg1? value1, TArg2? value2, TArg3? value3, TArg4? value4, TArg5? value5)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(string.Empty);

            writer.Write(value1);
            writer.Write(value2);
            writer.Write(value3);
            writer.Write(value4);
            writer.Write(value5);
        });
    }

    /// <summary>
    /// Invokes a remote method on the entity associated with the specified synchronization object.
    /// The method sends data to the remote endpoint, identified by a unique identifier.
    /// </summary>
    /// <typeparam name="TArg1">The type of the first argument.</typeparam>
    /// <typeparam name="TArg2">The type of the second argument.</typeparam>
    /// <typeparam name="TArg3">The type of the third argument.</typeparam>
    /// <typeparam name="TArg4">The type of the fourth argument.</typeparam>
    /// <typeparam name="TArg5">The type of the fifth argument.</typeparam>
    /// <typeparam name="TArg6">The type of the sixth argument.</typeparam>
    /// <param name="syncObject">The synchronization object associated with the entity.</param>
    /// <param name="id">The unique identifier of the remote method to invoke.</param>
    /// <param name="value1">The first argument to send to the remote endpoint.</param>
    /// <param name="value2">The second argument to send to the remote endpoint.</param>
    /// <param name="value3">The third argument to send to the remote endpoint.</param>
    /// <param name="value4">The fourth argument to send to the remote endpoint.</param>
    /// <param name="value5">The fifth argument to send to the remote endpoint.</param>
    /// <param name="value6">The sixth argument to send to the remote endpoint.</param>
    public static void InvokeRemote<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this SyncObject syncObject, string id, TArg1? value1, TArg2? value2, TArg3? value3, TArg4? value4, TArg5? value5, TArg6? value6)
    {
        Exceptions.NullArgument(nameof(syncObject), syncObject);
        Exceptions.EmptyArgument(nameof(id), id);

        syncObject.Owner?.SendWrapped(EntityHeader.Method, writer =>
        {
            writer.WriteString(id);
            writer.WriteString(syncObject.Id);
            writer.WriteString(string.Empty);

            writer.Write(value1);
            writer.Write(value2);
            writer.Write(value3);
            writer.Write(value4);
            writer.Write(value5);
            writer.Write(value6);
        });
    }
}