using NiveraAPI.Extensions;
using NiveraAPI.IO.Network.Entities.Messages;
using NiveraAPI.IO.Serialization;

namespace NiveraAPI.IO.Network.Entities;

/// <summary>
/// A network entity.
/// </summary>
public class Entity
{
    internal bool confirmed;
    internal bool destroyed;

    internal Action<ByteReader?>?[] conversations = new Action<ByteReader?>?[byte.MaxValue - 1];

    /// <summary>
    /// Gets the entity's ID.
    /// </summary>
    public ushort Id { get; internal set; }

    /// <summary>
    /// Whether the entity is destroyed.
    /// </summary>
    public bool IsDestroyed => destroyed || Manager == null;

    /// <summary>
    /// Whether the entity spawn is confirmed by the client.
    /// </summary>
    public bool IsConfirmed => confirmed;

    /// <summary>
    /// Gets the entity information.
    /// </summary>
    public EntityInfo Info { get; internal set; }

    /// <summary>
    /// Gets the parent entity manager.
    /// </summary>
    public EntityManager Manager { get; internal set; }

    /// <summary>
    /// Gets the network time in seconds.
    /// </summary>
    public float NetworkTime => Manager.Connection.Time.Time;

    /// <summary>
    /// Gets called when the entity is destroyed.
    /// </summary>
    public virtual void OnDestroyed()
    {
        
    }

    /// <summary>
    /// Gets called when the entity is spawned on the server.
    /// </summary>
    public virtual void OnServerSpawned()
    {
        
    }

    /// <summary>
    /// Gets called when the entity is spawned on the client.
    /// </summary>
    public virtual void OnClientSpawned()
    {
        
    }

    /// <summary>
    /// Gets called when the client confirms the entity spawn.
    /// </summary>
    public virtual void OnClientConfirmed()
    {
        
    }

    /// <summary>
    /// Gets called periodically to update the entity's state.
    /// </summary>
    /// <param name="localDeltaTime">The time in seconds that has elapsed locally since the last update call.</param>
    /// <param name="networkDeltaTime">The time in seconds that has elapsed on the server since the last update call.</param>
    public virtual void OnUpdate(float localDeltaTime, float networkDeltaTime)
    {
        
    }

    /// <summary>
    /// Attempts to destroy the entity by marking it as destroyed, removing it from the managing entity list,
    /// and notifying other systems through the manager.
    /// </summary>
    /// <returns>
    /// True if the entity was successfully destroyed; otherwise, false if destruction failed or the entity
    /// was already destroyed.
    /// </returns>
    public bool Destroy() 
        => Manager?.DestroyEntity(this) ?? false;

    /// <summary>
    /// Sets the value of a synchronized variable, updates the field, and sends a message to the manager
    /// to notify about the change.
    /// </summary>
    /// <param name="index">The index of the synchronized variable to be updated.</param>
    /// <param name="value">The new value to assign to the synchronized variable.</param>
    /// <param name="field">A reference to the local field representing the synchronized variable.</param>
    /// <typeparam name="T">The type of the synchronized variable.</typeparam>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the provided index is greater than or equal to the number of available synchronized variables.
    /// </exception>
    public void SetSyncVar<T>(ushort index, T value, ref T field)
    {
        if (index >= Info.SyncVars.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (field != null && value != null && value.Equals(field))
            return;
        
        var syncVar = Info.SyncVars[index];
        var curValue = field;
        
        field = value;
        
        using var writer = ByteWriter.GetWrite(w => w.Write(value));
        
        Manager.Send(new EntitySyncVarMessage(Id, index, writer.ToArray()));
        
        syncVar.Hook?.Invoke(this, [curValue, value]);
    }

    /// <summary>
    /// Sends a remote synchronous request using the specified remote index and writer,
    /// and waits for a response to be received.
    /// </summary>
    /// <param name="remoteIndex">The index of the remote method.</param>
    /// <param name="writer">The writer instance used to serialize and transmit data to the remote callback.</param>
    /// <returns>The response data as a <see cref="ByteReader"/> object, or null if no response is received.</returns>
    /// <exception cref="AggregateException">
    /// Thrown if an error occurs during the communication process.
    /// </exception>
    public ByteReader? SendRemoteSync(ushort remoteIndex, Action<ByteWriter> writer)
    {
        var tcs = new TaskCompletionSource<ByteReader?>();

        try
        {
            SendRemoteCallback(remoteIndex, writer, tcs.SetResult);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }

        while (!tcs.Task.IsCompleted)
            Thread.Sleep(1);

        if (tcs.Task.Exception != null)
            throw tcs.Task.Exception;

        return tcs.Task.Result;
    }

    /// <summary>
    /// Sends a synchronous remote request to the specified remote index, optionally providing data,
    /// and waits for a response to be received.
    /// </summary>
    /// <param name="remoteIndex">The index of the remote method.</param>
    /// <param name="data">The data to be sent with the request, or null if no data is provided.</param>
    /// <returns>
    /// A <see cref="ByteReader"/> containing the response data from the remote entity, or null if no response was received.
    /// </returns>
    /// <exception cref="AggregateException">Thrown if an error occurs while processing the remote request.</exception>
    public ByteReader? SendRemoteSync(ushort remoteIndex, byte[]? data = null)
    {
        var tcs = new TaskCompletionSource<ByteReader?>();

        try
        {
            SendRemoteCallback(remoteIndex, data, tcs.SetResult);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }

        while (!tcs.Task.IsCompleted)
        {
            Thread.Sleep(1);
        }

        if (tcs.Task.Exception != null)
            throw tcs.Task.Exception;

        return tcs.Task.Result;
    }

    /// <summary>
    /// Sends a remote message asynchronously to the specified target using a custom writer action.
    /// </summary>
    /// <param name="remoteIndex">The index of the remote method.</param>
    /// <param name="writer">The action used to write data to the message payload using a <see cref="ByteWriter"/>.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="ByteReader"/> that can be used to read the response from the remote target, or null if no response is provided.
    /// </returns>
    /// <exception cref="AggregateException">Thrown if an error occurs during the message sending or processing.</exception>
    public async Task<ByteReader?> SendRemoteAsync(ushort remoteIndex, Action<ByteWriter> writer)
    {
        var tcs = new TaskCompletionSource<ByteReader?>();

        try
        {
            SendRemoteCallback(remoteIndex, writer, tcs.SetResult);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }

        var result = await tcs.Task;

        if (tcs.Task.Exception != null)
            throw tcs.Task.Exception;

        return result;
    }

    /// <summary>
    /// Sends a remote asynchronous request to a specific remote index with optional data.
    /// </summary>
    /// <param name="remoteIndex">The remote index of the method.</param>
    /// <param name="data">The optional data to be sent to the remote index.</param>
    /// <returns>A task that represents the asynchronous operation, containing the response as a <see cref="ByteReader"/> if available.</returns>
    /// <exception cref="AggregateException">Thrown if the asynchronous operation encounters an exception.</exception>
    public async Task<ByteReader?> SendRemoteAsync(ushort remoteIndex, byte[]? data = null)
    {
        var tcs = new TaskCompletionSource<ByteReader?>();

        try
        {
            SendRemoteCallback(remoteIndex, data, tcs.SetResult);
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }

        var result = await tcs.Task;

        if (tcs.Task.Exception != null)
            throw tcs.Task.Exception;

        return result;
    }

    /// <summary>
    /// Sends a remote callback to the specified index with the provided writer and optional callback function.
    /// </summary>
    /// <param name="remoteIndex">
    /// The index of the remote method.
    /// </param>
    /// <param name="writer">
    /// An action that writes the required data for the remote operation using a <see cref="ByteWriter"/> instance.
    /// </param>
    /// <param name="callback">
    /// An optional action that processes the response using a <see cref="ByteReader"/> instance, if available.
    /// </param>
    public void SendRemoteCallback(ushort remoteIndex, Action<ByteWriter> writer, Action<ByteReader?>? callback = null)
    {
        using var instance = ByteWriter.Get();

        writer(instance);

        SendRemoteCallback(remoteIndex, instance.ToArray(), callback);
    }

    /// <summary>
    /// Sends a callback to a remote entity using the specified remote method index and optional data.
    /// </summary>
    /// <param name="remoteIndex">The index of the remote method to invoke.</param>
    /// <param name="data">The optional data to be sent alongside the callback.</param>
    /// <param name="callback">
    /// The callback to execute upon receiving a response. If null, no response will be handled.
    /// </param>
    /// <exception cref="Exception">
    /// Thrown if the entity has no associated <see cref="EntityInfo"/>,
    /// if the entity has no remote methods (RPCs or CMDs),
    /// or if the remote method index is out of range.
    /// </exception>
    public void SendRemoteCallback(ushort remoteIndex, byte[]? data = null, Action<ByteReader?>? callback = null)
    {
        if (Info == null)
            throw new Exception("Entity has no info!");

        var array = Manager.IsServer ? Info.Rpcs : Info.Cmds;
        
        if (array == null)
            throw new Exception("Entity has no RPCs / CMDs!");

        if (remoteIndex >= array.Count)
            throw new Exception("Remote index out of range!");

        var id = (byte)(callback != null ? conversations.FindIndex(x => x == null) : byte.MaxValue);

        if (id != byte.MaxValue)
            conversations[id] = callback;

        Manager.Send(new EntityInvokeMessage(id, !Manager.IsClient, Id, (short)remoteIndex, data));
    }

    internal void OnEntitySyncVarMessage(EntitySyncVarMessage msg)
    {
        if (msg.Index >= Info.SyncVars.Count)
        {
            Manager.Log.Warn($"SyncVar index {msg.Index} is out of range!");
            return;
        }

        if (msg.Data?.Length < 1)
        {
            Manager.Log.Warn("Received syncvar message with null data!");
            return;
        }

        try
        {
            using var reader = ByteReader.Get(msg.Data!, 0, msg.Data.Length);
            
            var syncVar = Info.SyncVars[msg.Index];

            var newValue = syncVar.Reader.Invoke(syncVar.ReaderTarget, [reader]);
            var curValue = syncVar.Field.GetValue(this);

            syncVar.Field.SetValue(this, newValue);
            syncVar.Hook?.Invoke(this, [curValue, newValue]);
        }
        catch (Exception ex)
        {
            Manager.Log.Error($"Error while updating syncvar:\n{ex}");
        }
    }

    internal void OnEntityInvokeMessage(EntityInvokeMessage msg)
    {
        if (Manager.IsServer && msg.Rpc)
        {
            Manager.Log.Warn("Received RPC on server!");
            return;
        }

        if (Manager.IsClient && !msg.Rpc)
        {
            Manager.Log.Warn("Received CMD on client!");
            return;       
        }
        
        msg.Data ??= Array.Empty<byte>();
        
        if (msg.Index < 0) // REPLY
        {
            if (msg.Id == byte.MaxValue) // 255 is reserved for a null callback
            {
                Manager.Log.Warn("RPC returned reply without a registered callback!");
                return;
            }
            
            var callback = conversations[msg.Id];

            if (callback == null)
            {
                Manager.Log.Warn($"RPC returned reply with a null callback!");
                return;
            }
            
            conversations[msg.Id] = null;

            try
            {
                if (msg.Data?.Length > 0)
                {
                    using var reader = ByteReader.Get(msg.Data!, 0, msg.Data.Length);
                    
                    callback(reader);
                }
                else
                {
                    callback(null);
                }
            }
            catch (Exception ex)
            {
                Manager.Log.Error($"Failed to read RPC reply data:\n{ex}");
            }
        }
        else
        {
            var array = msg.Rpc ? Info.Rpcs : Info.Cmds;
            
            if (msg.Index >= array.Count)
            {
                Manager.Log.Warn($"RPC/CMD index {msg.Index} is out of range!");
                return;
            }
            
            var invoke = array[msg.Index];

            try
            {
                if (invoke.HasReturnValue)
                {
                    using var writer = ByteWriter.Get();
                    using var reader = ByteReader.Get(msg.Data!, 0, msg.Data.Length);
                    
                    invoke.Target.Invoke(this, [reader, writer]);

                    if (msg.Id != 255)
                        Manager.Send(new EntityInvokeMessage(msg.Id, !Manager.IsClient, Id, -1, writer.ToArray()));
                }
                else
                {
                    using var reader = ByteReader.Get(msg.Data!, 0, msg.Data.Length);
                    
                    invoke.Target.Invoke(this, [reader, null]);

                    if (msg.Id != 255)
                        Manager.Send(new EntityInvokeMessage(msg.Id, !Manager.IsClient, Id, -1));
                }
            }
            catch (Exception ex)
            {
                Manager.Log.Error(ex);
                
                if (msg.Id != 255) // respond with no data to avoid deadlocks on remote
                    Manager.Send(new EntityInvokeMessage(msg.Id, !Manager.IsClient, Id, -1));
            }
        }
    }
}