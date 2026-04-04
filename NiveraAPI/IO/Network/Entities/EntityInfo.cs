using System.Collections.Concurrent;
using System.Reflection;
using NiveraAPI.Extensions;
using NiveraAPI.IO.Network.Entities.Attributes;
using NiveraAPI.IO.Network.Entities.Messages;
using NiveraAPI.IO.Serialization;
using NiveraAPI.Logs;
using NiveraAPI.Pooling;

namespace NiveraAPI.IO.Network.Entities;

/// <summary>
/// Represents information about an entity.
/// </summary>
public class EntityInfo
{
    private static volatile LogSink log = LogManager.GetSource("Networking", "EntityInfo");
    private static volatile ConcurrentDictionary<Type, EntityInfo> infoCache = new();

    private List<RemoteMethod> cmds = new();
    private List<RemoteMethod> rpcs = new();
    private List<RemoteSyncVar> syncVars = new();
    
    /// <summary>
    /// The name of the entity type on the server (when accessed from the client).
    /// </summary>
    public string? ServerName { get; private set; }

    /// <summary>
    /// The name of the entity type on the client (when accessed from the server).
    /// </summary>
    public string? ClientName { get; private set; }

    /// <summary>
    /// The type of the entity.
    /// </summary>
    public Type Type { get; private set; }

    /// <summary>
    /// An array of all registered server commands.
    /// </summary>
    public IReadOnlyList<RemoteMethod> Cmds => cmds;

    /// <summary>
    /// An array of all registered client RPCs.
    /// </summary>
    public IReadOnlyList<RemoteMethod> Rpcs => rpcs;

    /// <summary>
    /// An array of all registered sync vars.
    /// </summary>
    public IReadOnlyList<RemoteSyncVar> SyncVars => syncVars;

    /// <summary>
    /// Whether or not the index fields have been bound to their respective fields.
    /// </summary>
    public bool IndexFieldsBound { get; private set; }
    
    /// <summary>
    /// The name of the entity type.
    /// </summary>
    public string LocalTypeName => Type.FullName ?? Type.Name;

    /// <summary>
    /// Constructs the fully qualified member name by combining the local entity type name with the specified member name.
    /// </summary>
    /// <param name="memberName">The name of the member to be appended to the entity type's fully qualified name.</param>
    /// <returns>The fully qualified member name, consisting of the local entity type name and the specified member name.</returns>
    public string LocalFullMemberName(string memberName)
        => string.Concat(LocalTypeName, ".", memberName);

    /// <summary>
    /// Retrieves the name of the entity type based on the context (server or client).
    /// </summary>
    /// <param name="isServer">Specifies whether the current context is server-side or client-side.</param>
    /// <returns>The entity type name relevant to the specified context or the local type name if no specific name is defined.</returns>
    public string RemoteTypeName(bool isServer)
    {
        if (isServer)
            return ClientName ?? LocalTypeName;
        
        return ServerName ?? LocalTypeName;   
    }

    /// <summary>
    /// Constructs the full name of a member by combining the entity type name
    /// (based on server or client context) and the specified member name.
    /// </summary>
    /// <param name="memberName">The name of the member for which the full name is being constructed.</param>
    /// <param name="isServer">Indicates whether the current context is server-side or client-side.</param>
    /// <returns>A fully qualified member name in the format "TypeName.MemberName".</returns>
    public string RemoteFullMemberName(string memberName, bool isServer)
    {
        var typeName = GetType().FullName;

        if (isServer && ClientName != null)
            typeName = ClientName;
        
        if (!isServer && ServerName != null)
            typeName = ServerName;
        
        return string.Concat(typeName, ".", memberName);
    }

    /// <summary>
    /// Retrieves the <see cref="EntityInfo"/> for the specified entity type.
    /// If the <see cref="EntityInfo"/> is not already cached, it initializes a new instance,
    /// caches it, and then returns it.
    /// </summary>
    /// <param name="type">The type of the entity for which the information is retrieved.</param>
    /// <returns>An instance of <see cref="EntityInfo"/> representing the entity's metadata.</returns>
    public static EntityInfo GetInfo(Type type)
    {
        if (infoCache.TryGetValue(type, out var info))
            return info;

        info = new();
        info.Type = type;
        
        if (type.HasAttribute<ClientTypeAttribute>(out var clientTypeAttribute))
            info.ClientName = clientTypeAttribute.Name;
        else
            info.ClientName = type.FullName;
        
        if (type.HasAttribute<ServerTypeAttribute>(out var serverTypeAttribute))
            info.ServerName = serverTypeAttribute.Name;
        else
            info.ServerName = type.FullName;
        
        info.RegisterCmds();
        info.RegisterRpcs();
        info.RegisterSyncVars();
        
        infoCache.TryAdd(type, info);
        return info;
    }

    internal void WriteRpcs(Entity entity, ref ConfirmSpawnMessage msg)
    {
        msg.Rpcs = new string[rpcs.Count];
        
        for (var x = 0; x < rpcs.Count; x++)
        {
            var rpc = rpcs[x];
            var name = LocalFullMemberName(rpc.Target.Name);
            
            msg.Rpcs[x] = name;
        }
    }
    
    internal void ReadRpcs(Entity entity, ConfirmSpawnMessage msg)
    {
        if (msg.Rpcs.Length > 0 && rpcs.Count < 1)
        {
            for (var x = 0; x < msg.Rpcs.Length; x++)
            {
                var rpc = msg.Rpcs[x];
                var method = new RemoteMethod();

                method.IsRemote = true;
                method.Index = (ushort)x;
                method.RemoteName = rpc;

                rpcs.Add(method);
            }

            UpdateIndexFields(entity);
        }
    }

    internal void WriteCmds(Entity entity, ref EntitySpawnMessage msg)
    {
        msg.Cmds = new string[cmds.Count];
        
        for (var x = 0; x < cmds.Count; x++)
        {
            var cmd = cmds[x];
            var name = LocalFullMemberName(cmd.Target.Name);

            msg.Cmds[x] = name;
        }
    }

    internal void ReadCmds(Entity entity, EntitySpawnMessage msg)
    {
        if (msg.Cmds.Length > 0 && cmds.Count < 1)
        {
            for (var x = 0; x < msg.Cmds.Length; x++)
            {
                var cmd = msg.Cmds[x];
                var method = new RemoteMethod();

                method.IsRemote = true;
                method.Index = (ushort)x;
                method.RemoteName = cmd;
                
                cmds.Add(method);
            }
            
            UpdateIndexFields(entity);
        }
    }

    private void RegisterRpcs()
    {
        if (rpcs.Count < 1)
        {
            var list = ListPool<RemoteMethod>.Shared.Rent();
            var methods = Type.GetAllMethods();

            for (var x = 0; x < methods.Length; x++)
            {
                var method = methods[x];

                if (!IsValidMethod<ClientRpcAttribute>(method, Type, "ClientRpc", out var hasReturnValue))
                    continue;

                var remote = new RemoteMethod();

                remote.Target = method;
                remote.HasReturnValue = hasReturnValue;

                list.Add(remote);
            }

            var ordered = list.OrderBy(x => LocalFullMemberName(x.Target.Name));
            var index = 0;
            
            foreach (var method in ordered)
            {
                method.Index = (ushort)index++;

                rpcs.Add(method);
            }

            ListPool<RemoteMethod>.Shared.Return(list);
        }
    }

    private void RegisterCmds()
    {
        if (cmds.Count < 1)
        {
            var list = ListPool<RemoteMethod>.Shared.Rent();
            var methods = Type.GetAllMethods();

            for (var x = 0; x < methods.Length; x++)
            {
                var method = methods[x];

                if (!IsValidMethod<ServerCmdAttribute>(method, Type, "ServerCmd", out var hasReturnValue))
                    continue;

                var remote = new RemoteMethod();

                remote.Target = method;
                remote.HasReturnValue = hasReturnValue;

                list.Add(remote);
            }

            var ordered = list.OrderBy(x => LocalFullMemberName(x.Target.Name));
            var index = 0;

            foreach (var method in ordered)
            {
                method.Index = (ushort)index++;

                cmds.Add(method);
            }

            ListPool<RemoteMethod>.Shared.Return(list);
        }
    }

    private void RegisterSyncVars()
    {
        if (syncVars.Count < 1)
        {
            var list = ListPool<RemoteSyncVar>.Shared.Rent();
            var fields = Type.GetAllFields();

            for (var x = 0; x < fields.Length; x++)
            {
                var field = fields[x];
                
                if (!field.HasAttribute<SyncVarAttribute>())
                    continue;

                if (field.IsInitOnly)
                {
                    log.Warn($"SyncVar &3{field.Name}&r of &3{Type.FullName}&r is marked as &3initonly&r and cannot be used as a sync var");
                    continue;
                }

                if (field.IsStatic)
                {
                    log.Warn($"SyncVar &3{field.Name}&r of &3{Type.FullName}&r is marked as &3static&r and cannot be used as a sync var");
                    continue;
                }
                
                var serializerType = typeof(ByteSerializer<>).MakeGenericType(field.FieldType);
                
                var readerField = serializerType.FindField("Deserialize");
                var readerValue = readerField?.GetValue(null);

                if (readerValue is not Delegate readerDelegate || readerDelegate.Method == null)
                {
                    log.Error($"SyncVar &3{field.Name}&r of &3{Type.FullName}&r has an invalid reader delegate");
                    continue;
                }
                
                var remote = new RemoteSyncVar();
                var hook = Type.FindMethod($"OnSyncVarChanged_{field.Name}");

                if (hook != null)
                {
                    if (hook.IsStatic)
                    {
                        log.Warn($"SyncVar &3{field.Name}&r of &3{Type.FullName}&r has an invalid OnChanged hook (cannot be static)");
                        
                        hook = null;
                    }
                    else if (hook.ReturnType != typeof(void))
                    {
                        log.Error($"SyncVar &3{field.Name}&r of &3{Type.FullName}&r has an invalid OnChanged hook (must return &3void&r)");
                        
                        hook = null;
                    }
                    else
                    {
                        var hookParameters = hook.GetAllParameters();

                        if (hookParameters.Length != 2
                            || hookParameters[0].ParameterType != field.FieldType
                            || hookParameters[1].ParameterType != field.FieldType)
                        {
                            log.Error(
                                $"SyncVar &3{field.Name}&r of &3{Type.FullName}&r has an invalid OnChanged hook " +
                                $"(must have two parameters of type the same type as the field).");

                            hook = null;
                        }
                    }
                }

                remote.Hook = hook;
                remote.Field = field;
                
                remote.Reader = readerDelegate.Method;
                remote.ReaderTarget = readerDelegate.Target;
                
                list.Add(remote);
            }
            
            var ordered = list.OrderBy(x => LocalFullMemberName(x.Field.Name));
            var index = 0;
            
            foreach (var remoteSyncVar in ordered)
            {
                remoteSyncVar.Index = (ushort)index++;

                syncVars.Add(remoteSyncVar);
            }
            
            ListPool<RemoteSyncVar>.Shared.Return(list);
        }
    }

    private void UpdateIndexFields(Entity entity)
    {
        if (IndexFieldsBound)
            return;

        IndexFieldsBound = true;

        var fields = Type.GetAllFields();

        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];

            if (!field.HasAttribute<IndexFieldAttribute>(out var indexFieldAttribute))
                continue;

            if (field.IsInitOnly)
            {
                entity.Manager.Log.Warn($"&3{field.Name}&r of &3{Type.FullName}&r is marked as &3initonly&r and cannot be used as an index field");
                continue;
            }

            if (field.FieldType != typeof(ushort))
            {
                entity.Manager.Log.Warn($"&3{field.Name}&r of &3{Type.FullName}&r is marked as an index field but is not of type &3ushort&r");
                continue;
            }

            if (!field.IsStatic)
            {
                entity.Manager.Log.Warn($"&3{field.Name}&r of &3{Type.FullName}&r is marked as an index field but is not static");
                continue;           
            }

            var name = field.Name;
            
            if (!string.IsNullOrEmpty(indexFieldAttribute.Name))
                name = indexFieldAttribute.Name;

            name = name.ToLower();
            
            if (name.StartsWith("cmd_"))
            {
                var cmdName = name.Substring(4);
                var cmd = Cmds.FirstOrDefault(x => CompareRemoteMethod(cmdName, x));
                
                if (cmd != null)
                {
                    field.SetValue(null, cmd.Index);
                }
                else
                {
                    entity.Manager.Log.Warn($"Could not find CMD &6{cmdName}&r (field &3{field.Name}&r)");
                }
            }
            else if (name.StartsWith("rpc_"))
            {
                var rpcName = name.Substring(4);
                var rpc = Rpcs.FirstOrDefault(x => CompareRemoteMethod(rpcName, x));
                
                if (rpc != null)
                {
                    field.SetValue(null, rpc.Index);
                }
                else
                {
                    entity.Manager.Log.Warn($"Could not find RPC &6{rpcName}&r (field &3{field.Name}&r)");
                }
            }
            else if (name.StartsWith("syncvar_"))
            {
                var syncVarName = name.Substring(8);
                var syncVar = SyncVars.FirstOrDefault(x => string.Equals(x.Field.Name, syncVarName, StringComparison.OrdinalIgnoreCase));

                if (syncVar == null)
                {
                    entity.Manager.Log.Warn($"Could not find sync var &6{syncVarName}&r (field &3{field.Name}&r)");
                }
                else
                {
                    field.SetValue(null, syncVar.Index);
                }
            }
            else
            {
                entity.Manager.Log.Warn($"Invalid index field name &3{name}&r");
            }
        }
    }

    private bool CompareRemoteMethod(string name, RemoteMethod method)
    {
        if (method.IsRemote)
        {
            if (string.IsNullOrWhiteSpace(method.RemoteName))
                return false;
            
            if (method.RemoteName!.TrySplit('.', true, null, out var parts))
            {
                var target = parts[parts.Length - 1];

                if (string.Equals(target, name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        if (method.Target != null)
        {
            return string.Equals(method.Target.Name, name, StringComparison.OrdinalIgnoreCase);
        }
        
        return false;
    }
    
    private bool IsValidMethod<T>(MethodInfo method, Type curType, string type, out bool hasReturnValue)
        where T : Attribute
    {
        hasReturnValue = false;
        
        if (!method.HasAttribute<T>(out var attribute))
            return false;
        
        if (attribute is ClientRpcAttribute clientRpcAttribute)
            hasReturnValue = clientRpcAttribute.HasReturnValue;
        
        if (attribute is ServerCmdAttribute serverCmdAttribute)
            hasReturnValue = serverCmdAttribute.HasReturnValue;

        if (method.DeclaringType == null || method.DeclaringType != curType)
        {
            log.Error($"{type}: &3{method.Name}&r of &3{curType.FullName}&r is not a member of the entity");
            return false;
        }

        if (method.IsStatic)
        {
            log.Error($"{type}: &3{method.Name}&r of &3{curType.FullName}&r is static");
            return false;
        }

        if (method.ReturnType != typeof(void))
        {
            log.Error($"{type}: &3{method.Name}&r of &3{curType.FullName}&r must return &3void&r");
            return false;
        }
            
        var parameters = method.GetAllParameters();
        
        // parsing parameters via reflection would be too slow so we'll directly provide the received data instead
        // users then can create helper methods to invoke
        
        // [ClientRpc(true)] // true = HasReturnValue
        // private void GetMessageRpc(ByteReader reader, ByteWriter writer)
        // {
        //     var number = reader.ReadUInt16();
        //     var message = reader.ReadString();
        //  
        //     var result = GetMessage(number, message);
        //
        //     writer.WriteString(result);
        // }
        
        // public string GetMessage(int number, string message)
        // {
        //     return string.Concat(number, message);
        // }

        if (parameters.Length != 2
            || parameters[0].ParameterType != typeof(ByteReader)
            || parameters[1].ParameterType != typeof(ByteWriter))
        {
            log.Error(
                $"{type}: &3{method.Name}&r of &3{curType.FullName}&r must have two parameters of type &3ByteReader&r and &3ByteWriter&r");
            return false;
        }
        
        return true;
    }
}