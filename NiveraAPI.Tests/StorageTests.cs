using System.Diagnostics;

using NiveraAPI.Logs;
using NiveraAPI.IO.Storage;

namespace NiveraAPI.Tests;

/// <summary>
/// Tests for the <see cref="FileStorage"/> class.
/// </summary>
public static class StorageTests
{
    /// <summary>
    /// The log source.
    /// </summary>
    public static LogSink Log = LogManager.GetSource("Tests", "Storage");
    
    /// <summary>
    /// Starts the tests.
    /// </summary>
    public static void Start()
    {
        Log.Info("Creating storage ..");

        var storage = new FileStorage();

        storage.Directory = Directory.GetCurrentDirectory();
        storage.UseTypeCode = LibraryLoader.HasArgument("storagetest_use_typecode");
        
        if (LibraryLoader.HasArgument("storagetest_json_indent"))
            storage.Format = FileFormat.JsonIndented;
        else if (LibraryLoader.HasArgument("storagetest_json_noindent"))
            storage.Format = FileFormat.JsonNotIndented;

        var obj = storage.Create("Test");
        
        var propInt = obj.Add<int>("TestInt", 1);
        var propString = obj.Add<string>("TestString", "BC_");
        
        Log.Debug($"PropInt = {propInt.Value}");
        Log.Debug($"PropString = {propString.Value}");
        
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            if (stopwatch.ElapsedMilliseconds > 10000)
            {
                propInt.Value++;
                propString.Value += "_A";
                
                Log.Debug($"PropInt = {propInt.Value}");
                Log.Debug($"PropString = {propString.Value}");
                
                stopwatch.Restart();
            }
            
            storage.SaveDirty();
        }
    }
}