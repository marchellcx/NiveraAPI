using NiveraAPI.IO.Network.Entities.Synchronization.Interfaces;
using NiveraAPI.IO.Serialization;
using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Network.Entities.Synchronization.Objects;

/// <summary>
/// Represents a synchronized variable that allows bidirectional communication
/// of a value between a client and a server in a networked environment.
/// </summary>
/// <typeparam name="T">The type of the value being synchronized.</typeparam>
public class SyncVar<T> : ISyncVar
{
    private T? syncVarValue;

    /// <summary>
    /// Gets the custom ID of the sync-var.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the synchronization parent associated with this SyncVar,
    /// which manages synchronization logic and ownership relationships
    /// in the networked environment.
    /// </summary>
    public SyncParent Parent { get; }

    /// <summary>
    /// Gets the synchronized object associated with this instance of the SyncVar.
    /// The SyncObject allows tracking and manipulation of the parent entity
    /// within the synchronization context.
    /// </summary>
    public SyncObject? Object { get; }
    
    /// <summary>
    /// Gets or sets the value of the sync-var.
    /// </summary>
    public T? Value
    {
        get => syncVarValue;
        set
        {
            if (!Parent.HasAuthority)
                throw new InvalidOperationException("Cannot set value of sync-var without authority.");

            syncVarValue = value;
            SendUpdate();
        }
    }
    
    /// <summary>
    /// Creates a new instance of the SyncVar class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the SyncVar.</param>
    /// <param name="syncParent">The parent of the sync-var.</param>
    /// <param name="syncObject">The sync-object associated with the sync-var.</param>
    /// <param name="defaultValue">The default value for the SyncVar.</param>
    public SyncVar(string id, SyncObject? syncObject, SyncParent syncParent, T? defaultValue)
    {
        Exceptions.NullArgument(nameof(syncParent), syncParent);

        Id = id;

        Parent = syncParent;
        Object = syncObject;
        
        syncVarValue = defaultValue;
        
        if (Parent.Owner.IsConfirmed && Parent.HasAuthority)
            SendUpdate();
    }

    /// <summary>
    /// Sends an update for the SyncVar to the associated entity's connection,
    /// indicating that the value of the SyncVar has changed.
    /// This method serializes the updated value and sends it encapsulated
    /// in an EntityWrappedMessage with the SyncVar header.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the entity associated with the SyncVar does not have a valid connection.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the serializer for the message is not registered or is improperly configured.
    /// </exception>
    public void SendUpdate()
    {
        Parent?.Owner?.SendWrapped(EntityHeader.SyncVar, writer =>
        {
            writer.WriteString(Object?.Id ?? string.Empty);
            writer.WriteString(Id);
            
            writer.Write(Value);
        });
    }

    /// <summary>
    /// Removes the current SyncVar instance by invoking any necessary destruction logic,
    /// including detaching event listeners and cleaning up resources.
    /// </summary>
    public void Remove()
    {
        
    }

    /// <summary>
    /// Confirms the current synchronized variable, typically to indicate that changes have been acknowledged
    /// or processed within the synchronization system.
    /// </summary>
    public void Confirmed()
    {
        if (Parent.HasAuthority)
        {
            SendUpdate();
        }
    }

    /// <summary>
    /// Processes data read from a ByteReader instance.
    /// </summary>
    /// <param name="reader">The ByteReader instance providing serialized data to be deserialized and processed.</param>
    public void Receive(ByteReader reader)
    {
        var curValue = syncVarValue;
        
        syncVarValue = reader.Read<T>();
        
        Object?.OnSyncVarUpdated(curValue, this);
    }
}