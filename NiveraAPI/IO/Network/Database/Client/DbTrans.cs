using NiveraAPI.IO.Network.Database.Enums;
using NiveraAPI.Pooling;

namespace NiveraAPI.IO.Network.Database.Client;

/// <summary>
/// Represents a database transaction.
/// </summary>
public class DbTrans : PoolResettable
{
    /// <summary>
    /// The ID of the transaction.
    /// </summary>
    public ushort Id { get; internal set; }

    /// <summary>
    /// The client that started the transaction.
    /// </summary>
    public DbClient? Client { get; internal set; }

    /// <summary>
    /// The result of the transaction.
    /// </summary>
    public DbTransResult? Result { get; internal set; }
    
    /// <summary>
    /// Whether the transaction is done.
    /// </summary>
    public bool IsDone => Result != null;
    
    /// <summary>
    /// Whether the transaction is done and successful.
    /// </summary>
    public bool IsDoneAndOk => Result?.IsOk ?? false;
    
    /// <summary>
    /// The data returned by the transaction.
    /// </summary>
    public byte[]? Data => Result?.Data;
    
    /// <summary>
    /// The exception thrown by the transaction.
    /// </summary>
    public Exception? Exception => Result?.Exception;
    
    /// <summary>
    /// The result type of the transaction.
    /// </summary>
    public DbResult? ResultType => Result?.Result;
    
    /// <summary>
    /// The action to perform when the transaction is complete.
    /// </summary>
    public Action<DbTrans>? OnComplete { get; set; }

    /// <summary>
    /// Places the object back into the pool for reuse by resetting its state and performing any necessary cleanup.
    /// This method must be implemented by derived classes to define specific reset behavior.
    /// </summary>
    /// <remarks>
    /// This method is intended to be called when the object is no longer in use and should be returned to a reusable state.
    /// Implementations should ensure that the object is properly prepared for its next usage and does not retain any stale references or data.
    /// </remarks>
    public override void ReturnToPool()
    {
        Id = 0;
        Client = null;
        Result = null;
        OnComplete = null;
    }
}