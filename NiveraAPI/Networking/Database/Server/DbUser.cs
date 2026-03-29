using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.Networking.Database.Enums;
using NiveraAPI.Networking.Database.Messages;
using NiveraAPI.Services;
using NiveraAPI.Services.Interfaces;

namespace NiveraAPI.Networking.Database.Server;

/// <summary>
/// Represents a service responsible for handling operations and interactions with a database server.
/// Provides authentication management, database actions handling, and permission control.
/// </summary>
public class DbUser : NetworkService
{
    static DbUser()
    {
        ObjectSerializer.RegisterDefaultSerializer<DbAuthMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<DbActionMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<DbResponseMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<DbAuthResultMessage>(() => new());
    }

    /// <summary>
    /// The parent database server.
    /// </summary>
    public DbServer Server { get; private set; }
    
    /// <summary>
    /// Whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; private set; }
    
    /// <summary>
    /// Whether the user has attempted to authenticate.
    /// </summary>
    public bool AttemptedAuth { get; private set; }
    
    /// <summary>
    /// The permissions of the user.
    /// </summary>
    public DbPerms Permissions { get; set; } = DbPerms.None;
    
    /// <summary>
    /// The services required by this service.
    /// </summary>
    public override Type[] RequiredServices { get; } = [typeof(DbServer)];

    /// <summary>
    /// Determines whether the specified service collection allows a new service to be added.
    /// </summary>
    /// <param name="collection">The service collection to check.</param>
    /// <returns>
    /// True if the service can be added to the collection; otherwise, false.
    /// </returns>
    public override bool CanBeAdded(IServiceCollection collection)
        => collection is Peer;

    /// <summary>
    /// Starts the service.
    /// </summary>
    public override void Start()
    {
        base.Start();
        
        Server = Collection.GetService<DbServer>();

        IsAuthenticated = false;
        AttemptedAuth = false;
        
        Permissions = DbPerms.None;
        
        Log.Info("Database service started, waiting for authentication ..");
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
    public override bool HandlePayload(ISerializableObject serializableObject)
    {
        switch (serializableObject)
        {
            case DbActionMessage dbActionMessage:
                OnDbActionMessage(dbActionMessage);
                return true;
            
            case DbAuthMessage dbAuthMessage:
                OnDbAuthMessage(dbAuthMessage);
                return true;
            
            default:
                return base.HandlePayload(serializableObject);
        }
    }

    private void GetItem(DbActionMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Table) || string.IsNullOrEmpty(msg.Item))
        {
            Log.Warn("Attempted to get item without specifying a table or item!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.InvalidArguments));
            return; 
        }
        
        if ((Permissions & DbPerms.AccessItem) != DbPerms.AccessItem)
        {
            Log.Warn($"Attempted to access item &1{msg.Item}&r in table &3{msg.Table}&r without sufficient permissions!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Unauthorized));
            return;
        }

        try
        {
            var tableObj = Server.File.GetTable(msg.Table);

            if (tableObj == null)
            {
                Log.Warn($"Attempted to access an unknown table: {msg.Table}");

                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.TableNotFound));
                return;
            }

            var itemObj = tableObj.GetItem(msg.Item);

            if (itemObj == null)
            {
                Log.Warn($"Attempted to access an unknown item: {msg.Item} in table {msg.Table}");

                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.ItemNotFound));
                return;
            }
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Ok, itemObj));
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, ex));
        }
    }

    private void AddItem(DbActionMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Table) || string.IsNullOrEmpty(msg.Item)
            || msg.Data?.Length < 1)
        {
            Log.Warn("Attempted to add item without specifying a table, item, or data!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.InvalidArguments));
            return;
        }
        
        if ((Permissions & DbPerms.AddNewItem) != DbPerms.AddNewItem)
        {
            Log.Warn($"Attempted to add item &1{msg.Item}&r to table &1{msg.Table}&r without sufficient permissions!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Unauthorized));
            return;
        }

        try
        {
            var tableObj = Server.File.GetTable(msg.Table);

            if (tableObj == null)
            {
                Log.Warn($"Attempted to add an item to an unknown table: {msg.Table}");

                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.TableNotFound));
                return;
            }

            tableObj.UpdateItem(msg.Item, msg.Data);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, ex));
            return;
        }

        Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Ok));
        
        Log.Debug($"Added item &1{msg.Item}&r to table &1{msg.Table}&r");       
    }

    private void UpdateItem(DbActionMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Table) || string.IsNullOrEmpty(msg.Item)
            || msg.Data?.Length < 1)
        {
            Log.Warn("Attempted to update item without specifying a table, item, or data!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.InvalidArguments));
            return;  
        }
        
        if ((Permissions & DbPerms.UpdateExistingItem) != DbPerms.UpdateExistingItem)
        {
            Log.Warn($"Attempted to update item &1{msg.Item}&r in table &1{msg.Table}&r without sufficient permissions!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Unauthorized));
            return;
        }

        try
        {
            var tableObj = Server.File.GetTable(msg.Table);

            if (tableObj == null)
            {
                Log.Warn($"Attempted to update an item in an unknown table: {msg.Table}");

                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.TableNotFound));
                return;
            }

            if (tableObj.GetItem(msg.Item) == null)
            {
                Log.Warn($"Attempted to update an item that does not exist: {msg.Item} in table {msg.Table}");

                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.ItemNotFound));
                return;
            }

            tableObj.UpdateItem(msg.Item, msg.Data);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, ex));
            return;
        }

        Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Ok));       
        
        Log.Debug($"Updated item &1{msg.Item}&r in table &1{msg.Table}&r");
    }

    private void UpdateItemOrAddNew(DbActionMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Table) || string.IsNullOrEmpty(msg.Item)
            || msg.Data?.Length < 1)
        {
            Log.Warn("Attempted to update item without specifying a table, item, or data!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.InvalidArguments));
            return;   
        }
        
        try
        {
            var tableObj = Server.File.GetTable(msg.Table);

            if (tableObj == null)
            {
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.TableNotFound));
                return;
            }

            var exists = tableObj.GetItem(msg.Item) != null;

            if (exists)
            {
                if ((Permissions & DbPerms.UpdateExistingItem) != DbPerms.UpdateExistingItem)
                {
                    Log.Warn(
                        $"Attempted to update item &1{msg.Item}&r in table &1{msg.Table}&r without sufficient permissions!");

                    Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,
                        DbResult.Unauthorized));
                    return;
                }
            }
            else
            {
                if ((Permissions & DbPerms.AddNewItem) != DbPerms.AddNewItem)
                {
                    Log.Warn(
                        $"Attempted to add item &1{msg.Item}&r to table &1{msg.Item}&r without sufficient permissions!");

                    Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,
                        DbResult.Unauthorized));
                    return;
                }
            }

            tableObj.UpdateItem(msg.Item, msg.Data);
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, ex));
            return;
        }

        Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Ok));
        
        Log.Debug($"Updated item &1{msg.Item}&r in table &1{msg.Table}&r");       
    }

    private void RemoveItem(DbActionMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Table) || string.IsNullOrEmpty(msg.Item))
        {
            Log.Warn("Attempted to remove item without specifying a table or item!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.InvalidArguments));
            return;    
        }
        
        if ((Permissions & DbPerms.RemoveItem) != DbPerms.RemoveItem)
        {
            Log.Warn($"Attempted to remove item &1{msg.Item}&r from table &1{msg.Table}&r without sufficient permissions!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Unauthorized));
            return;
        }
        
        var tableObj = Server.File.GetTable(msg.Table);
        
        if (tableObj == null)
        {
            Log.Warn($"Attempted to remove an item from an unknown table: {msg.Table}");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.TableNotFound));
            return;
        }

        try
        {
            if (tableObj.RemoveItem(msg.Item))
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Ok));
            else
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Failed));
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, ex));
            return;
        }
    }
    
    private void RemoveTable(DbActionMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Table))
        {
            Log.Warn("Attempted to delete table without specifying a table!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.InvalidArguments));
            return;     
        }
        
        if ((Permissions & DbPerms.DeleteTable) != DbPerms.DeleteTable)
        {
            Log.Warn($"Attempted to delete table '{msg.Table}' without sufficient permissions!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Unauthorized));
            return;
        }

        try
        {
            if (Server.File.RemoveTable(msg.Table))
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Ok));
            else
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Failed));
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, ex));
            return;
        }
    }

    private void AddTable(DbActionMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Table))
        {
            Log.Warn("Attempted to create table without specifying a table!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.InvalidArguments));
            return;      
        }
        
        if ((Permissions & DbPerms.CreateTable) != DbPerms.CreateTable)
        {
            Log.Warn($"Attempted to create table '{msg.Table}' without sufficient permissions!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Unauthorized));
            return;
        }

        try
        {
            if (Server.File.AddTable(msg.Table) != null)
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Ok));
            else
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Failed));
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, ex));
            return;
        }
    }

    private void ClearTable(DbActionMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Table))
        {
            Log.Warn("Attempted to clear table without specifying a table!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.InvalidArguments));
            return;       
        }
        
        if ((Permissions & DbPerms.ClearTable) != DbPerms.ClearTable)
        {
            Log.Warn($"Attempted to clear table '{msg.Table}' without sufficient permissions!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Unauthorized));
            return;
        }

        try
        {
            var tableObj = Server.File.GetTable(msg.Table);

            if (tableObj == null)
            {
                Log.Warn($"Attempted to clear an unknown table: {msg.Table}");

                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.TableNotFound));
                return;
            }

            tableObj.Clear();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, ex));
            return;
        }

        Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks,DbResult.Ok));
    }

    private void ClearTables(DbActionMessage msg)
    {
        if ((Permissions & DbPerms.ClearAllTables) != DbPerms.ClearAllTables)
        {
            Log.Warn("Attempted to clear all tables without sufficient permissions!");
            return;
        }

        try
        {
            Server.File.RemoveTables();
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, ex));
            return;
        }

        Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Ok));
    }

    // Serves as a fast increment / decrement path for statistics tracking, etc.
    // This avoids doing a double call to first retrieve the value and then send the updated value.
    private void ModifyInt64(DbActionMessage msg, bool increment = false)
    {
        if (string.IsNullOrEmpty(msg.Table) || string.IsNullOrEmpty(msg.Item)
            || msg.Data?.Length != 8)
        {
            Log.Warn("Attempted to modify int64 without specifying a table or item!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.InvalidArguments));
            return;
        }

        if ((Permissions & DbPerms.UpdateExistingItem) != DbPerms.UpdateExistingItem)
        {
            Log.Warn("Attempted to modify int64 without sufficient permissions!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Unauthorized));
            return;
        }
        
        try
        {
            var tableObj = Server.File.GetTable(msg.Table);

            if (tableObj == null)
            {
                Log.Warn($"Attempted to modify an unknown table: {msg.Table}");
                
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.TableNotFound));
                return;
            }
            
            var itemObj = tableObj.GetItem(msg.Item);
            
            if (itemObj != null && itemObj.Length == 8)
            {
                var curVal = BitConverter.ToInt64(itemObj, 0);
                var modifyVal = msg.Data != null ? BitConverter.ToInt64(msg.Data, 0) : 0;
                var newVal = increment ? curVal + modifyVal : curVal - modifyVal;
                var newObj = BitConverter.GetBytes(newVal);

                tableObj.UpdateItem(msg.Item, newObj);
                
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Ok, newObj));
            }
            else
            {
                tableObj.UpdateItem(msg.Item, msg.Data);
                
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Ok, msg.Data));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, ex));
        }
    }

    private void GetInt64(DbActionMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Table) || string.IsNullOrEmpty(msg.Item))
        {
            Log.Warn("Attempted to get int64 without specifying a table or item!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.InvalidArguments));
            return;
        }
        
        if ((Permissions & DbPerms.AccessItem) != DbPerms.AccessItem)
        {
            Log.Warn($"Attempted to access item &1{msg.Item}&r in table &1{msg.Table}&r without sufficient permissions!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Unauthorized));
            return;
        }

        try
        {
            var tableObj = Server.File.GetTable(msg.Table);

            if (tableObj == null)
            {
                Log.Warn($"Attempted to access an unknown table: {msg.Table}");
                
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.TableNotFound));
                return;           
            }
            
            var itemObj = tableObj.GetItem(msg.Item);

            if (itemObj == null || itemObj.Length != 8)
            {
                Log.Warn($"Attempted to access an item that is not an Int64: {msg.Item} in table {msg.Table}");
                
                Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.InvalidArguments));
                return;
            }
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Ok, itemObj));
        }
        catch (Exception ex)
        {
            Log.Error(ex);
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.Exception));
        }
    }

    private void OnDbActionMessage(DbActionMessage msg)
    {
        if (!IsAuthenticated)
        {
            Log.Warn("Attempted to get data from database without authentication!");
            
            Send(new DbResponseMessage(msg.Id, msg.UtcTicks, DateTime.UtcNow.Ticks, DbResult.NotAuthenticated));
            return;
        }

        switch (msg.Action)
        {
            case DbAction.AccessItem:
                GetItem(msg);
                break;
            
            case DbAction.AddNewItem:
                AddItem(msg);
                break;
            
            case DbAction.UpdateExistingItem:
                UpdateItem(msg);
                break;
            
            case DbAction.UpdateExistingOrAddItem:
                UpdateItemOrAddNew(msg);
                break;
            
            case DbAction.RemoveItem:
                RemoveItem(msg);
                break;
            
            case DbAction.AddTable:
                AddTable(msg);
                break;
            
            case DbAction.ClearTable:
                ClearTable(msg);
                break;
            
            case DbAction.RemoveTable:
                RemoveTable(msg);
                break;
            
            case DbAction.ClearAllTables:
                ClearTables(msg);
                break;
            
            case DbAction.GetInt64:
                GetInt64(msg);
                break;
            
            case DbAction.IncrementInt64 or DbAction.DecrementInt64:
                ModifyInt64(msg, msg.Action == DbAction.IncrementInt64);
                break;
        }
    }
    
    private void OnDbAuthMessage(DbAuthMessage msg)
    {
        if (AttemptedAuth)
        {
            Log.Warn("Received a duplicate DbAuth request!");
            
            Send(new DbAuthResultMessage(false, DbPerms.None));
            return;
        }

        AttemptedAuth = true;

        if (!Server.Config.IsValidUser(msg.User, msg.Password))
        {
            Log.Warn("Invalid user/password combination!");
            
            Send(new DbAuthResultMessage(false, DbPerms.None));
            return;
        }

        IsAuthenticated = true;

        Permissions = Server.Config.GetPermissions(msg.User);
        
        Send(new DbAuthResultMessage(true, Permissions));
        
        Log.Info($"Authenticated user &1{msg.User}&r, permissions: &3{Permissions}&r");
    }
}