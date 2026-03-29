using System.Net;

using NiveraAPI.Commands;
using NiveraAPI.Commands.Attributes;

using NiveraAPI.Console;

using NiveraAPI.IO.Serialization;
using NiveraAPI.IO.Serialization.Interfaces;
using NiveraAPI.IO.Serialization.Serializers;

using NiveraAPI.Logs;
using NiveraAPI.Networking.Entities;
using NiveraAPI.Networking.Entities.Attributes;
using NiveraAPI.Networking.Telepathy;
using NiveraAPI.Pooling;

namespace NiveraAPI.Tests;

/// <summary>
/// Tests for the Telepathy library.
/// </summary>
public static class TelepathyTests
{
    /// <summary>
    /// Represents a log message that contains a timestamp indicating when the message was sent.
    /// </summary>
    public struct LogTimeMessage : ISerializableObject
    {
        /// <summary>
        /// The time the message was sent.
        /// </summary>
        public long SentTicks;
        
        /// <summary>
        /// Gets the objectSerializer responsible for serializing and deserializing the object.
        /// </summary>
        public IObjectSerializer Serializer => DefaultSerializer<LogTimeMessage>.Singleton;

        /// <summary>
        /// Deserializes data from the provided ByteReader instance into the current object's state.
        /// </summary>
        /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
        public void Deserialize(ByteReader reader)
        {
            SentTicks = reader.ReadInt64();
        }

        /// <summary>
        /// Serializes the current state of the object to the provided ByteWriter instance.
        /// </summary>
        /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
        public void Serialize(ByteWriter writer)
        {
            writer.WriteInt64(SentTicks);
        }
    }
    
    /// <summary>
    /// Represents a message containing a number and a string, which can be serialized
    /// and deserialized, and supports pooling for memory efficiency.
    /// </summary>
    public struct HelloMessage : ISerializableObject
    {
        /// <summary>
        /// A test number.
        /// </summary>
        public int Number;

        /// <summary>
        /// A test string.
        /// </summary>
        public string Word;
        
        /// <summary>
        /// Gets the objectSerializer responsible for serializing and deserializing the object.
        /// </summary>
        public IObjectSerializer Serializer => DefaultSerializer<HelloMessage>.Singleton;
        
        /// <summary>
        /// Creates a new instance of the HelloMessage struct.
        /// </summary>
        /// <param name="number">The test number.</param>
        /// <param name="word">The test string.</param>
        public HelloMessage(int number = 42, string word = "Hello")
        {
            Number = number;
            Word = word;
        }

        /// <summary>
        /// Serializes the current state of the object to the provided ByteWriter instance.
        /// </summary>
        /// <param name="writer">The ByteWriter instance used to write the serialized data.</param>
        public void Serialize(ByteWriter writer)
        {
            writer.WriteInt32(Number);
            writer.WriteString(Word);
        }

        /// <summary>
        /// Deserializes data from the provided ByteReader instance into the current object's state.
        /// </summary>
        /// <param name="reader">The ByteReader instance used to read the serialized data.</param>
        public void Deserialize(ByteReader reader)
        {
            Number = reader.ReadInt32();
            Word = reader.ReadString();
        }

        /// <summary>
        /// Returns a string representation of the HelloMessage instance,
        /// including the Number and Word fields.
        /// </summary>
        /// <returns>A string displaying the values of the Number and Word fields.</returns>
        public override string ToString()
            => $"[HelloMessage] Number: {Number}, Word: {Word}";
    }

    /// <summary>
    /// Represents a server-side entity in the NiveraAPI networking system.
    /// This class is responsible for managing server-specific logic and
    /// interactions, including synchronization of variables, remote
    /// procedure calls (RPCs), and server commands (ServerCmd).
    /// </summary>
    [ClientType("NiveraAPI.Tests.TelepathyTests+ClientEntity")]
    public class ServerEntity : Entity
    {
        [IndexField] private static volatile ushort syncVar_testSyncVar = 0;
        
        [IndexField] private static volatile ushort rpc_TestRpc = 0;
        [IndexField] private static volatile ushort rpc_TestReturnRpc = 0;
        
        private volatile LogSink log = LogManager.GetSource("Tests", "ServerEntity");
        
        [SyncVar]
        private volatile bool testSyncVar = false;

        /// <summary>
        /// Represents a synchronized variable that is automatically propagated
        /// between server and clients when its value is updated.
        /// </summary>
        public bool TestSyncVar
        {
            get => testSyncVar;
            set => SetSyncVar(syncVar_testSyncVar, value, ref testSyncVar);
        }

        /// <summary>
        /// Sends a remote procedure call (RPC) to execute the TestRpc method on the connected clients.
        /// </summary>
        /// <param name="message">The string message to be transmitted with the RPC.</param>
        public void CallTestRpc(string message)
            => SendRemoteCallback(rpc_TestRpc, writer => writer.WriteString(message));

        /// <summary>
        /// Sends a remote callback with a string message and processes the result upon completion.
        /// </summary>
        /// <param name="message">The message to be sent to the remote callback.</param>
        public void CallTestReturnRpc(string message)
        {
            SendRemoteCallback(rpc_TestReturnRpc, writer => writer.WriteString(message), 
                result => log.Debug($"TestReturnRpc result: {result?.ReadString() ?? "null"}"));
        }

        /// <summary>
        /// Executes the TestCmd server command.
        /// </summary>
        /// <param name="reader">The <see cref="ByteReader"/> instance used to read input data for the command.</param>
        /// <param name="writer">The <see cref="ByteWriter"/> instance used to write output data for the command.</param>
        [ServerCmd]
        public void TestCmd(ByteReader reader, ByteWriter writer)
        {
            log.Debug($"TestCmd({reader?.ReadString() ?? "null"})");
        }

        /// <summary>
        /// Executes the server command with a return value by processing input data from the ByteReader
        /// and writing the response using the ByteWriter.
        /// </summary>
        /// <param name="reader">The ByteReader instance to read input data from the client.</param>
        /// <param name="writer">The ByteWriter instance to send a response back to the client.</param>
        [ServerCmd(true)]
        public void TestReturnCmd(ByteReader reader, ByteWriter writer)
        {
            log.Debug($"TestReturnCmd({reader?.ReadString() ?? "null"})");

            writer.WriteString("There (server)!");
        }
        
        /// <summary>
        /// Gets called when the entity is spawned on the server.
        /// </summary>
        public override void OnServerSpawned()
        {
            base.OnServerSpawned();
            
            log.Debug("OnServerSpawned()");
        }

        /// <summary>
        /// Gets called when the client confirms the entity spawn.
        /// </summary>
        public override void OnClientConfirmed()
        {
            base.OnClientConfirmed();
            
            CallTestRpc("Hello from server!");
            CallTestReturnRpc("Hello from server!");
            
            TestSyncVar = true;
            
            log.Debug("OnClientConfirmed()");
        }

        /// <summary>
        /// Gets called when the entity is destroyed.
        /// </summary>
        public override void OnDestroyed()
        {
            base.OnDestroyed();
            
            log.Debug("OnDestroyed()");
        }
        
        private void OnSyncVarChanged_testSyncVar(bool oldValue, bool newValue)
        {
            log.Debug($"TestSyncVar changed from {oldValue} to {newValue}");
        }
    }

    /// <summary>
    /// Represents a client-side entity in the networking layer.
    /// Inherits from the base Entity class and provides specialized functionality
    /// for client-specific Remote Procedure Call (RPC) handling and synchronization operations.
    /// </summary>
    [ServerType("NiveraAPI.Tests.TelepathyTests+ServerEntity")]
    public class ClientEntity : Entity
    {
        [IndexField] private static volatile ushort syncVar_testSyncVar = 0;
        
        [IndexField] private static volatile ushort cmd_TestCmd = 0;
        [IndexField] private static volatile ushort cmd_TestReturnCmd = 0;
        
        private volatile LogSink log = LogManager.GetSource("Tests", "ClientEntity");
        
        [SyncVar]
        private volatile bool testSyncVar = false;

        /// <summary>
        /// Represents a synchronized variable that is automatically propagated between the server and client.
        /// </summary>
        public bool TestSyncVar
        {
            get => testSyncVar;
            set => SetSyncVar(syncVar_testSyncVar, value, ref testSyncVar);
        }

        /// <summary>
        /// Sends a remote command to the server with the specified message.
        /// This method utilizes the internal RPC framework for client-to-server communication.
        /// </summary>
        /// <param name="message">The message string to be transmitted as part of the command.</param>
        public void CallTestCmd(string message)
            => SendRemoteCallback(cmd_TestCmd, writer => writer.WriteString(message));

        /// <summary>
        /// Sends a remote procedure call (RPC) with a string parameter to the server
        /// and logs the server's response.
        /// </summary>
        /// <param name="message">The string message to be sent to the server.</param>
        public void CallTestReturnCmd(string message)
        {
            SendRemoteCallback(cmd_TestReturnCmd, writer => writer.WriteString(message), 
                result => log.Debug($"TestReturnCmd result: {result?.ReadString() ?? "null"}"));
        }

        /// <summary>
        /// Executes a test Remote Procedure Call (RPC) on the client side.
        /// </summary>
        /// <param name="reader">The <see cref="ByteReader"/> instance responsible for reading serialized data from the network.</param>
        /// <param name="writer">The <see cref="ByteWriter"/> instance responsible for writing serialized data to the network.</param>
        [ClientRpc(false)]
        public void TestRpc(ByteReader reader, ByteWriter writer)
        {
            log.Debug($"TestRpc({reader?.ReadString() ?? "null"})");
        }

        /// <summary>
        /// Executes a client-to-server Remote Procedure Call (RPC) that reads and processes input data
        /// from the specified ByteReader and writes a response using the specified ByteWriter.
        /// </summary>
        /// <param name="reader">The ByteReader instance used to read input data for the RPC call.</param>
        /// <param name="writer">The ByteWriter instance used to write the RPC response data.</param>
        [ClientRpc(true)]
        public void TestReturnRpc(ByteReader reader, ByteWriter writer)
        {
            log.Debug($"TestReturnRpc({reader.ReadString()})");
            
            writer.WriteString("There (client)!");
        }
        
        /// <summary>
        /// Gets called when the entity is spawned on the client.
        /// </summary>
        public override void OnClientSpawned()
        {
            base.OnClientSpawned();
            
            CallTestCmd("Hello from client!");
            CallTestReturnCmd("Hello from client!");
            
            TestSyncVar = true;
            
            log.Debug("OnClientSpawned()");
        }

        /// <summary>
        /// Gets called when the entity is destroyed.
        /// </summary>
        public override void OnDestroyed()
        {
            base.OnDestroyed();
            
            log.Debug("OnDestroyed()");
        }

        private void OnSyncVarChanged_testSyncVar(bool oldValue, bool newValue)
        {
            log.Debug($"TestSyncVar changed from {oldValue} to {newValue}");
        }
    }
    
    private static volatile TelepathyClient client;
    private static volatile TelepathyServer server;
    
    /// <summary>
    /// The log sink for the tests.
    /// </summary>
    public static volatile LogSink Log = LogManager.GetSource("Tests", "Telepathy");
    
    internal static void Client()
    {
        var port = 7777;

        if (LibraryLoader.HasArgument("telepathy.port", out var value) 
            && int.TryParse(value, out var customPort))
            port = customPort;
        
        ConsoleCommands.Manager.RegisterCommand(typeof(TelepathyTests), null, out _, out _);
        
        ObjectSerializer.RegisterDefaultSerializer<HelloMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<LogTimeMessage>(() => new());
        
        Log.Info("Creating client ..");
        
        client = new();
        
        client.Connected += () =>
        {
            Log.Info("Client has connected");
            
            client.LocalPeer.SetHandler<HelloMessage>(msg => Log.Info($"Received message: &1{msg}&r"));
            
            client.LocalPeer.SetHandler<LogTimeMessage>(msg =>
            {
                var timeTicks = client.LocalPeer.TimeTicks + (DateTime.UtcNow.Ticks - msg.SentTicks);
                var timeMs = (float)(timeTicks / TimeSpan.TicksPerMillisecond);
                
                Log.Info($"Time: &1{timeMs}&r ms (&3{timeTicks}&r ticks)");
            });
        };
        
        client.Disconnected += () => Log.Info("Client has disconnected");

        EntityManager.RegisterEntity<ClientEntity>(() => new ClientEntity());
        
        client.Services = [typeof(EntityManager)];
        client.Start();
        
        Log.Info("Connecting to server ..");
        
        client.Connect(new(IPAddress.Loopback, port));
    }
    
    internal static void Server()
    {
        ConsoleCommands.Manager.RegisterCommand(typeof(TelepathyTests), null, out _, out _);
        
        var port = 7777;

        if (LibraryLoader.HasArgument("telepathy.port", out var value) 
            && int.TryParse(value, out var customPort))
            port = customPort;
        
        ObjectSerializer.RegisterDefaultSerializer<HelloMessage>(() => new());
        ObjectSerializer.RegisterDefaultSerializer<LogTimeMessage>(() => new());
        
        Log.Info("Creating server ..");
        
        server = new();
        
        server.Started += () => Log.Info("Server has started");
        server.Stopped += () => Log.Info("Server has stopped");
        
        server.Listening += port => Log.Info($"Server is listening on &1{port}&r");
        
        server.Connected += peer =>
        {
            Log.Info($"Client has connected from &1{peer.RemoteEndPoint}&r");
            
            peer.SetHandler<HelloMessage>(msg => Log.Info($"Received message: &1{msg}&r"));
            
            peer.SetHandler<LogTimeMessage>(msg =>
            {
                var timeTicks = peer.TimeTicks + (DateTime.UtcNow.Ticks - msg.SentTicks);
                var timeMs = (float)(timeTicks / TimeSpan.TicksPerMillisecond);
                
                Log.Info($"Time: &1{timeMs}&r ms (&3{timeTicks}&r ticks)");
            });
            
            var manager = peer.GetService(typeof(EntityManager)) as EntityManager;

            if (manager is null)
            {
                Log.Error($"Entity manager not found for peer &1{peer.RemoteEndPoint}&r");
                return;
            }

            manager.TrySpawnEntity<ServerEntity>(out _);
        };
        
        server.Disconnected += peer => Log.Info($"Client has disconnected from &1{peer.RemoteEndPoint}&r");

        EntityManager.RegisterEntity<ServerEntity>(() => new ServerEntity());
        
        server.ProvidedServices = [typeof(EntityManager)];
        server.Start();
        
        Log.Info("Server started");
        
        server.Listen(port);
    }

    [Overload("ping", "Shows the ping of the client / server peer.")]
    private static void Ping(ref CommandContext<object> ctx)
    {
        if (client != null)
        {
            if (client.LocalPeer == null)
            {
                ctx.SetFailText("Not connected.");
                return;
            }
            
            ctx.SetOkText($"Ping: {client.LocalPeer.Latency} ms");
        }
        else if (server != null)
        {
            var peer = server.Peers.FirstOrDefault();

            if (peer == null)
            {
                ctx.SetFailText("No peers connected.");
                return;
            }
            
            ctx.SetOkText($"Ping: {peer.Latency} ms");
        }
        else
        {
            ctx.SetFailText("Client and server are null!");
        }
    }

    [Overload("time", "Shows the current time of the client / server peer.")]
    private static void Time(ref CommandContext<object> ctx)
    {
        if (client != null)
        {
            if (client.LocalPeer == null)
            {
                ctx.SetFailText("Not connected.");
                return;
            }
            
            var timeTicks = client.LocalPeer.TimeTicks;
            var timeMs = client.LocalPeer.TimeMilliseconds;
            
            var curTicks = DateTime.UtcNow.Ticks;
            
            client.LocalPeer.Send(new LogTimeMessage() { SentTicks = curTicks });
            
            ctx.SetOkText($"Time: {timeMs} ms ({timeTicks} ticks)");
        }
        else if (server != null)
        {
            var peer = server.Peers.FirstOrDefault();

            if (peer == null)
            {
                ctx.SetFailText("No peers connected.");
                return;
            }
            
            var timeTicks = peer.TimeTicks;
            var timeMs = peer.TimeMilliseconds;
            
            var curTicks = DateTime.UtcNow.Ticks;
            
            peer.Send(new LogTimeMessage() { SentTicks = curTicks });
            
            ctx.SetOkText($"Time: {timeMs} ms ({timeTicks} ticks)");
        }
        else
        {
            ctx.SetFailText("Client and server are null!");
        }
    }

    [Overload("hello", "Sends a HelloMessage to the client / server peer.")]
    private static void Hello(ref CommandContext<object> ctx)
    {
        if (client != null)
        {
            if (client.LocalPeer == null)
            {
                ctx.SetFailText("Not connected.");
                return;
            }
            
            client.LocalPeer.Send(new HelloMessage());
            
            ctx.SetOkText("Hello message sent FROM client");
        }
        else if (server != null)
        {
            var peer = server.Peers.FirstOrDefault();

            if (peer == null)
            {
                ctx.SetFailText("No peers connected.");
                return;
            }
            
            peer.Send(new HelloMessage());
            
            ctx.SetOkText("Hello message sent TO client");
        }
        else
        {
            ctx.SetFailText("Client and server are null!");
        }
    }
}