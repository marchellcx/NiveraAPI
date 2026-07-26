namespace NiveraAPI.Utilities;

/// <summary>
/// Provides a base class for implementing the IDisposable interface.
/// This class includes common logic for managing the disposed state
/// of an object and ensuring proper disposal of unmanaged resources.
/// </summary>
public class DisposableBase : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the current instance has been disposed.
    /// </summary>
    /// <remarks>
    /// This property returns <c>true</c> if the instance has been disposed; otherwise, <c>false</c>.
    /// Use this property to check the disposal status before performing further operations
    /// to avoid using an already disposed object.
    /// </remarks>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// Marks the current instance as disposed and prevents further use of the instance.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when an attempt is made to dispose an already disposed instance.
    /// </exception>
    public virtual void Dispose()
    {
        if (IsDisposed)
            ThrowDisposed();
        
        IsDisposed = true;
    }

    /// <summary>
    /// Checks whether the current instance has been disposed and throws an
    /// ObjectDisposedException if the instance is already disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the current instance has already been disposed.
    /// </exception>
    public void CheckDisposed()
    {
        if (IsDisposed)
        {
            ThrowDisposed();
        }
    }

    internal void ThrowDisposed()
        => throw new ObjectDisposedException(GetType().FullName);
}