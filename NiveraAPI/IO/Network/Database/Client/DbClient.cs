using System.Text;
using NiveraAPI.IO.Network.Database.Enums;
using NiveraAPI.IO.Network.Database.Messages;
using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.Pooling;
using NiveraAPI.Services;
using NiveraAPI.Services.Interfaces;
using NiveraAPI.Utilities;

namespace NiveraAPI.IO.Network.Database.Client;

/// <summary>
/// Represents a database client responsible for managing database operations
/// and interactions within the networked application.
/// </summary>
public class DbClient : NetService
{
    static DbClient()
    {
        ObjectSerializer.RegisterDefaultSerializer<DbAuthMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<DbActionMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<DbResponseMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<DbAuthResultMessage>(() => new());
    }
    
    private long longestDiff = 0;
    private long shortestDiff = 0;
    
    private ushort transId = 0;
    
    private DbConfig config;
    
    private readonly List<DbTrans> transactions = new();

    /// <summary>
    /// Whether the client is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; private set; }
    
    /// <summary>
    /// The permissions of the client.
    /// </summary>
    public DbPerms Permissions { get; private set; }
    
    /// <summary>
    /// The services required by this service.
    /// </summary>
    public override Type[] RequiredServices { get; } = [typeof(DbConfig)];
    
    /// <summary>
    /// The number of transactions that are currently waiting to be completed.
    /// </summary>
    public int WaitingTransactions => transactions.Count;
    
    /// <summary>
    /// The average duration of all transactions, in milliseconds.
    /// </summary>
    public float AverageDuration => (longestDiff + shortestDiff) / 2f;

    /// <summary>
    /// Determines whether the specified service collection allows a new service to be added.
    /// </summary>
    /// <param name="collection">The service collection to check.</param>
    /// <returns>
    /// True if the service can be added to the collection; otherwise, false.
    /// </returns>
    public override bool CanBeAdded(IServiceCollection collection)
        => collection is NetClient;

    /// <summary>
    /// Clears all tables within the database.
    /// </summary>
    /// <param name="callback">
    /// An optional callback that will be invoked with the result of the operation.
    /// The result indicates the success or failure of clearing all tables.
    /// </param>
    public void ClearAllTables(Action<DbResult>? callback = null)
    {
        if ((Permissions & DbPerms.ClearAllTables) != DbPerms.ClearAllTables)
            throw new InvalidOperationException("You do not have permission to clear all tables!");
        
        void Complete(DbTrans trans)
            => callback?.Invoke(trans.ResultType ?? DbResult.Failed);
        
        SendTransaction(Complete, new(0, DbAction.ClearAllTables, null, null));
    }

    /// <summary>
    /// Removes a table from the database if the table exists and the current permissions allow it.
    /// </summary>
    /// <param name="name">The name of the table to be removed.</param>
    /// <param name="callback">An optional callback function that is invoked with the result of the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown if the table name is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the current user does not have the necessary permissions to remove the table.</exception>
    public void RemoveTable(string name, Action<DbResult>? callback = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if ((Permissions & DbPerms.DeleteTable) != DbPerms.DeleteTable)
            throw new InvalidOperationException("You do not have permission to remove this table!");       
        
        void Complete(DbTrans trans)
            => callback?.Invoke(trans.ResultType ?? DbResult.Failed);

        SendTransaction(Complete, new(0, DbAction.RemoveTable, name, null));
    }

    /// <summary>
    /// Clears the contents of the specified table in the database.
    /// </summary>
    /// <param name="name">The name of the table to be cleared.</param>
    /// <param name="callback">
    /// An optional callback to be invoked upon completion, returning a <see cref="DbResult"/> indicating the result of the operation.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified table name is null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current instance does not have the required permission to clear the table.
    /// </exception>
    public void ClearTable(string name, Action<DbResult>? callback = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if ((Permissions & DbPerms.ClearTable) != DbPerms.ClearTable)
            throw new InvalidOperationException("You do not have permission to clear this table!");      
        
        void Complete(DbTrans trans)
            => callback?.Invoke(trans.ResultType ?? DbResult.Failed);

        SendTransaction(Complete, new(0, DbAction.ClearTable, name, null));
    }

    /// <summary>
    /// Adds a new table to the database with the specified name.
    /// </summary>
    /// <param name="name">The name of the table to be added. Must not be null or empty.</param>
    /// <param name="callback">An optional callback to handle the result of the add table operation.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided table name is null or empty.</exception>
    public void AddTable(string name, Action<DbResult>? callback = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
     
        if ((Permissions & DbPerms.CreateTable) != DbPerms.CreateTable)
            throw new InvalidOperationException("You do not have permission to create this table!");      
        
        void Complete(DbTrans trans)
            => callback?.Invoke(trans.ResultType ?? DbResult.Failed);

        SendTransaction(Complete, new(0, DbAction.AddTable, name, null));
    }

    /// <summary>
    /// Retrieves a 64-bit integer value from the specified table and item in the database.
    /// </summary>
    /// <param name="table">The name of the database table to query.</param>
    /// <param name="item">The key of the database item from which to retrieve the value.</param>
    /// <param name="callback">
    /// A callback function that receives the database result and the retrieved 64-bit integer value.
    /// The callback is invoked with a <see cref="DbResult"/> indicating the operation result and
    /// the retrieved value if the operation succeeds.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="table"/>, <paramref name="item"/>, or <paramref name="callback"/> parameter is null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the permissions set for the client do not allow access to the specified item in the table.
    /// </exception>
    public void GetInt64(string table, string item, Action<DbResult, long> callback)
    {
        if (callback is null)
            throw new ArgumentNullException(nameof(callback));

        if (string.IsNullOrEmpty(item))
            throw new ArgumentNullException(nameof(item));
        
        if (string.IsNullOrEmpty(table))
            throw new ArgumentNullException(nameof(table));
        
        if ((Permissions & DbPerms.AccessItem) != DbPerms.AccessItem)
            throw new InvalidOperationException("You do not have permission to access this item!");

        void Complete(DbTrans trans)
            => callback?.Invoke(trans.ResultType ?? DbResult.Failed, BitConverter.ToInt64(trans.Data!, 0));

        SendTransaction(Complete, new(0, DbAction.GetInt64, table, item));
    }

    /// <summary>
    /// Increments the value of the specified 64-bit integer item in a table by the given amount.
    /// </summary>
    /// <param name="table">The name of the table containing the item to be incremented.</param>
    /// <param name="item">The name of the item whose value will be incremented.</param>
    /// <param name="amount">The amount by which the item value will be incremented.</param>
    /// <param name="callback">A callback function that receives the result of the operation and the updated value of the item.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="callback"/>, <paramref name="table"/>, or <paramref name="item"/> is null or empty.
    /// </exception>
    public void IncrementInt64(string table, string item, long amount, Action<DbResult, long> callback)
    {
        if (callback is null)
            throw new ArgumentNullException(nameof(callback));

        if (string.IsNullOrEmpty(table))
            throw new ArgumentNullException(nameof(table));      
        
        if (string.IsNullOrEmpty(item))
            throw new ArgumentNullException(nameof(item));
        
        void Complete(DbTrans trans)
            => callback?.Invoke(trans.ResultType ?? DbResult.Failed, BitConverter.ToInt64(trans.Data!, 0));

        SendTransaction(Complete, new(0, DbAction.IncrementInt64, table, item, BitConverter.GetBytes(amount)));
    }

    /// <summary>
    /// Decrements the value of a 64-bit integer in the specified database table and item by the given amount.
    /// </summary>
    /// <param name="table">The name of the table containing the target item.</param>
    /// <param name="item">The key of the item to decrement.</param>
    /// <param name="amount">The amount to decrement from the current value of the item.</param>
    /// <param name="callback">
    /// A callback function that is invoked after the operation is completed, providing the result of the operation
    /// and the updated value of the item.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="table"/>, <paramref name="item"/>, or <paramref name="callback"/> is null or empty.
    /// </exception>
    public void DecrementInt64(string table, string item, long amount, Action<DbResult, long> callback)
    {
        if (callback is null)
            throw new ArgumentNullException(nameof(callback));

        if (string.IsNullOrEmpty(table))
            throw new ArgumentNullException(nameof(table));      
        
        if (string.IsNullOrEmpty(item))
            throw new ArgumentNullException(nameof(item));
        
        void Complete(DbTrans trans)
            => callback?.Invoke(trans.ResultType ?? DbResult.Failed, BitConverter.ToInt64(trans.Data!, 0));

        SendTransaction(Complete, new(0, DbAction.DecrementInt64, table, item, BitConverter.GetBytes(amount)));
    }

    /// <summary>
    /// Adds a new item to the specified table in the database.
    /// </summary>
    /// <param name="table">The name of the table where the item will be added.</param>
    /// <param name="item">The identifier of the item to add.</param>
    /// <param name="value">The value of the item to add, of a generic type.</param>
    /// <param name="callback">
    /// An optional callback action that is triggered after the operation completes,
    /// providing the result of the operation.
    /// </param>
    /// <typeparam name="T">The type of the value being added.</typeparam>
    public void AddItem<T>(string table, string item, T value, Action<DbResult>? callback = null)
        => AddItem(table, item, ByteWriter.GetArray(w => w.Write(value)), callback);

    /// <summary>
    /// Adds a new item to the specified table in the database.
    /// </summary>
    /// <param name="table">The name of the table where the item will be added.</param>
    /// <param name="item">The name of the item to be added.</param>
    /// <param name="data">The byte array representing the item's data.</param>
    /// <param name="callback">An optional callback that returns the result of the database operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when any of the parameters <paramref name="table"/>, <paramref name="item"/>, or <paramref name="data"/> is null or empty.</exception>
    public void AddItem(string table, string item, byte[] data, Action<DbResult>? callback = null)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        if (string.IsNullOrEmpty(item))
            throw new ArgumentNullException(nameof(item));
        
        if (string.IsNullOrEmpty(table))
            throw new ArgumentNullException(nameof(table));
        
        if ((Permissions & DbPerms.AddNewItem) != DbPerms.AddNewItem)
            throw new InvalidOperationException("You do not have permission to add items to this table!");      
        
        void Complete(DbTrans trans)
            => callback?.Invoke(trans.ResultType ?? DbResult.Failed);
        
        SendTransaction(Complete, new(0, DbAction.AddNewItem, table, item, data));
    }

    /// <summary>
    /// Updates an existing item or adds a new item to the specified table in the database.
    /// </summary>
    /// <param name="table">The name of the database table where the item will be updated or added.</param>
    /// <param name="item">The key or identifier of the item to update or add.</param>
    /// <param name="value">The value of the item to be updated or added.</param>
    /// <param name="callback">An optional callback to handle the result of the operation.</param>
    /// <typeparam name="T">The type of the item's value.</typeparam>
    public void UpdateOrAddItem<T>(string table, string item, T value, Action<DbResult>? callback = null)
        => UpdateOrAddItem(table, item, ByteWriter.GetArray(w => w.Write(value)), callback);

    /// <summary>
    /// Updates an existing item or adds a new item in the specified table.
    /// </summary>
    /// <param name="table">The name of the table in which the operation will be performed.</param>
    /// <param name="item">The identifier of the item to update or add.</param>
    /// <param name="data">The serialized data of the item.</param>
    /// <param name="callback">An optional callback invoked with the result of the operation.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the table, item, or data is null or empty.
    /// </exception>
    public void UpdateOrAddItem(string table, string item, byte[] data, Action<DbResult>? callback = null)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        if (string.IsNullOrEmpty(item))
            throw new ArgumentNullException(nameof(item));
        
        if (string.IsNullOrEmpty(table))
            throw new ArgumentNullException(nameof(table));
        
        void Complete(DbTrans trans)
            => callback?.Invoke(trans.ResultType ?? DbResult.Failed);
        
        SendTransaction(Complete, new(0, DbAction.UpdateExistingOrAddItem, table, item, data));
    }

    /// <summary>
    /// Updates an existing item in a specified database table with the provided value.
    /// </summary>
    /// <typeparam name="T">The type of the value to be updated in the database item.</typeparam>
    /// <param name="table">The name of the database table containing the item to be updated.</param>
    /// <param name="item">The identifier or key of the item to update.</param>
    /// <param name="value">The new value to update the item with.</param>
    /// <param name="callback">
    /// An optional callback to be invoked with the result of the database update operation,
    /// represented as a <see cref="DbResult"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the <paramref name="table"/>, <paramref name="item"/>, or <paramref name="value"/>
    /// argument is null or empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the serializer for the provided type <typeparamref name="T"/> is not defined.
    /// </exception>
    public void UpdateItem<T>(string table, string item, T value, Action<DbResult>? callback = null)
        => UpdateItem(table, item, ByteWriter.GetArray(w => w.Write(value)), callback);
    
    /// <summary>
    /// Updates an existing item in the specified table within the database.
    /// </summary>
    /// <param name="table">The name of the table containing the item to update. Cannot be null or empty.</param>
    /// <param name="item">The identifier of the item to be updated. Cannot be null or empty.</param>
    /// <param name="data">The updated data to replace the existing item's data. Cannot be null.</param>
    /// <param name="callback">
    /// An optional callback function that is invoked with the result of the update operation.
    /// The result is of type <see cref="DbResult"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the required parameters—<paramref name="table"/>, <paramref name="item"/>,
    /// or <paramref name="data"/>—is null or empty.
    /// </exception>
    public void UpdateItem(string table, string item, byte[] data, Action<DbResult>? callback = null)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        if (string.IsNullOrEmpty(item))
            throw new ArgumentNullException(nameof(item));
        
        if (string.IsNullOrEmpty(table))
            throw new ArgumentNullException(nameof(table));

        if ((Permissions & DbPerms.UpdateExistingItem) != DbPerms.UpdateExistingItem)
            throw new InvalidOperationException("You do not have permission to update this item!");     
        
        void Complete(DbTrans trans)
            => callback?.Invoke(trans.ResultType ?? DbResult.Failed);
        
        SendTransaction(Complete, new(0, DbAction.UpdateExistingItem, table, item, data));
    }

    /// <summary>
    /// Removes the specified item from the given table in the database.
    /// </summary>
    /// <param name="table">The name of the table from which the item should be removed.</param>
    /// <param name="item">The identifier of the item to remove.</param>
    /// <param name="callback">
    /// An optional callback that is invoked with a boolean indicating whether the
    /// operation was successfully completed.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="table"/> or <paramref name="item"/> is null or empty.
    /// </exception>
    public void RemoveItem(string table, string item, Action<DbResult>? callback = null)
    {
        if (string.IsNullOrEmpty(item))
            throw new ArgumentNullException(nameof(item));
        
        if (string.IsNullOrEmpty(table))
            throw new ArgumentNullException(nameof(table));
        
        if ((Permissions & DbPerms.RemoveItem) != DbPerms.RemoveItem)
            throw new InvalidOperationException("You do not have permission to remove this item!");      

        void Complete(DbTrans trans)
            => callback?.Invoke(trans.ResultType ?? DbResult.Failed);
        
        SendTransaction(Complete, new(0, DbAction.RemoveItem, table, item));
    }

    /// <summary>
    /// Retrieves an item of the specified type from the given database table and invokes the callback with the result.
    /// </summary>
    /// <param name="table">The name of the database table to access.</param>
    /// <param name="item">The key or identifier of the item to retrieve from the table.</param>
    /// <param name="callback">
    /// A callback to handle the retrieved item; receives the deserialized object of type <typeparamref name="T"/>
    /// or null if retrieval fails.
    /// </param>
    /// <typeparam name="T">The type of the item to retrieve.</typeparam>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the callback, table, or item is null or empty.
    /// </exception>
    public void Get<T>(string table, string item, Action<DbResult, T?> callback)
    {
        if (callback is null) 
            throw new ArgumentNullException(nameof(callback));
        
        if (string.IsNullOrEmpty(item)) 
            throw new ArgumentNullException(nameof(item));
        
        if (string.IsNullOrEmpty(table))
            throw new ArgumentNullException(nameof(table));
        
        if ((Permissions & DbPerms.AccessItem) != DbPerms.AccessItem)
            throw new InvalidOperationException("You do not have permission to access this item!");      

        void Complete(DbTrans trans)
        {
            if (trans is { IsDoneAndOk: true, Result.Data: not null })
            {
                var reader = ByteReader.Get(trans.Data!, 0, trans.Data.Length);

                try
                {
                    callback(trans.ResultType ?? DbResult.Failed, reader.Read<T>());
                }
                catch (Exception ex)
                {
                    Log.Error($"Could not invoke callback:\n{ex}");
                }
            }
            else
            {
                callback(trans.ResultType ?? DbResult.Failed, default);
            }
        }
        
        SendTransaction(Complete, new(0, DbAction.AccessItem, table, item));
    }

    /// <summary>
    /// Retrieves the raw data associated with a specified item in a given table from the database.
    /// </summary>
    /// <param name="table">The name of the table where the item is located.</param>
    /// <param name="item">The identifier of the item to retrieve.</param>
    /// <param name="callback">The callback function to execute after the operation completes, receiving the raw data or null if not found.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="table"/>, <paramref name="item"/>, or <paramref name="callback"/> is null or empty.
    /// </exception>
    public void GetRaw(string table, string item, Action<DbResult, byte[]?> callback)
    {
        if (callback is null)
            throw new ArgumentNullException(nameof(callback));
        
        if (string.IsNullOrEmpty(item)) 
            throw new ArgumentNullException(nameof(item));
        
        if (string.IsNullOrEmpty(table))
            throw new ArgumentNullException(nameof(table));
        
        if ((Permissions & DbPerms.AccessItem) != DbPerms.AccessItem)
            throw new InvalidOperationException("You do not have permission to access this item!");      

        void Complete(DbTrans trans)
            => callback(trans.ResultType ?? DbResult.Failed, trans.Data);

        SendTransaction(Complete, new(0, DbAction.AccessItem, table, item));
    }

    /// <summary>
    /// Sends a database action message as part of a transaction and returns the created transaction instance.
    /// </summary>
    /// <param name="callback">An optional callback to invoke upon completion of the transaction.</param>
    /// <param name="msg">The database action message to send.</param>
    /// <returns>
    /// The created <see cref="DbTrans"/> instance, representing the transaction associated with the sent message.
    /// </returns>
    public DbTrans SendTransaction(Action<DbTrans>? callback, DbActionMessage msg)
    {
        var transaction = CreateTransaction(callback);

        msg.Id = transaction.Id;
        
        Send(msg);
        return transaction;
    }
    
    /// <summary>
    /// Creates and initializes a new database transaction.
    /// </summary>
    /// <param name="callback">An optional callback to be invoked upon the completion of the transaction.</param>
    /// <returns>The newly created and initialized database transaction.</returns>
    public DbTrans CreateTransaction(Action<DbTrans>? callback = null)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Cannot create a transaction without being authenticated!");
        
        var transaction = PoolBase<DbTrans>.Shared.Rent();

        transaction.Id = transId++;
        transaction.OnComplete = callback;
        
        transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Starts the service.
    /// </summary>
    public override void Start()
    {
        base.Start();

        transId = 0;
        longestDiff = 0;
        shortestDiff = 0;       
        
        config = Collection.GetService<DbConfig>();
        config.Password ??= string.Empty;

        IsAuthenticated = false;
        
        Permissions = DbPerms.None;
        
        Log.Info($"Database service started, sending authentication request as &1{config.User}&r ..");
        
        Send(new DbAuthMessage(config.User, config.Password));
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    public override void Stop()
    {
        base.Stop();
        
        transId = 0;
        longestDiff = 0;
        shortestDiff = 0;      
        
        transactions.ForEach(PoolBase<DbTrans>.Shared.Return);
        transactions.Clear();
        
        IsAuthenticated = false;
        
        Permissions = DbPerms.None;

        config = null!;
    }

    /// <summary>
    /// Processes a given serializable object payload, determining if the payload can be handled by the network service.
    /// </summary>
    /// <param name="serializableObject">
    /// The payload object that implements the <see cref="ISerializableObject"/> interface.
    /// This object is intended to be processed by the network service.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the payload was successfully handled.
    /// Returns <c>true</c> if the payload was processed, otherwise <c>false</c>.
    /// </returns>
    public override bool Receive(ISerializableObject serializableObject)
    {
        switch (serializableObject)
        {
            case DbResponseMessage dbResponseMessage:
                OnDbResponseMessage(dbResponseMessage);
                return true;
            
            case DbAuthResultMessage dbAuthResultMessage:
                OnDbAuthResultMessage(dbAuthResultMessage);
                return true;
            
            default:
                return base.Receive(serializableObject);
        }
    }

    private void OnDbAuthResultMessage(DbAuthResultMessage msg)
    {
        if (!msg.IsOk)
        {
            IsAuthenticated = false;
            
            Permissions = DbPerms.None;
            
            Log.Warn("Failed to authenticate with database!");
            return;
        }
        
        IsAuthenticated = true;
        
        Permissions = msg.Permissions;
        
        Log.Info($"Authenticated with database (permissions: &3{Permissions}&r)!");
    }
    
    private void OnDbResponseMessage(DbResponseMessage msg)
    {
        var transaction = transactions.Find(t => t.Id == msg.Id);

        if (transaction is null)
        {
            Log.Error($"Received response for unknown transaction: &1{msg.Id}&r");
            return;
        }

        var diff = TimeUtils.TicksDiffMilliseconds(msg.UtcProcessed, msg.UtcReceived);
        
        if (diff > longestDiff || longestDiff == 0) longestDiff = diff;
        if (diff < shortestDiff || shortestDiff == 0) shortestDiff = diff;
        
        Log.Debug($"Received response for transaction &1{transaction.Id}&r: &6{msg.Data?.Length ?? 0} bytes&r, " +
                  $"result: &3{msg.Result}&r, duration: &1{diff}&r ms (average: &6{AverageDuration}&r ms)");
        
        transactions.Remove(transaction);

        if (msg.Result is DbResult.Exception)
        {
            if (msg.Data == null || msg.Data.Length < 1)
            {
                Log.Error($"Transaction &1{transaction.Id}&r failed, but no exception was provided!");
                
                transaction.Result = new(null, DbResult.Exception, new Exception("No exception provided!"));
            }
            else
            {
                var exceptionString = Encoding.UTF32.GetString(msg.Data);
                var exception = new Exception(exceptionString);

                Log.Error($"Transaction &1{transaction.Id}&r failed:");
                Log.Error(exception);

                transaction.Result = new(null, DbResult.Exception, exception);
            }
        }
        else
        {
            if (msg.Result != DbResult.Ok)
                Log.Warn($"Transaction &1{transaction.Id}&r failed with result: &3{msg.Result}&r!");
            
            transaction.Result = new(msg.Data, msg.Result, null);
        }

        try
        {
            transaction.OnComplete?.Invoke(transaction);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to invoke callback of transaction &1{transaction.Id}&r:\n{ex}");
        }
        
        PoolBase<DbTrans>.Shared.Return(transaction);
    }
}