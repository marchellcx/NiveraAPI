namespace NiveraAPI.Utilities;

/// <summary>
/// Represents an interface that represents a class that should be instantiated and it's OnLoaded method invoked once
/// the library loads.
/// </summary>
public interface IInvokeOnLoad
{
    /// <summary>
    /// Checks if this instance was loaded.
    /// </summary>
    bool IsLoaded { get; }
    
    /// <summary>
    /// Method that will be called once the library finishes loading.
    /// </summary>
    void OnLoaded();
}