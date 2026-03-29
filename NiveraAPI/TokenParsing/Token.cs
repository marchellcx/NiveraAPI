using NiveraAPI.Pooling;

namespace NiveraAPI.TokenParsing;

/// <summary>
/// Represents a token that can be pooled for reuse.
/// Provides mechanisms to reset its state when returned to the pool.
/// </summary>
/// <remarks>
/// The <see cref="Token"/> class is intended to be a base unit for token parsing logic
/// within the framework. It integrates with a pooling mechanism via the
/// <see cref="PoolResettable"/> base class, allowing efficient resource management.
/// </remarks>
public abstract class Token : PoolResettable
{
    /// <summary>
    /// Gets a new token instance of the same type.
    /// </summary>
    /// <returns>The new token instance.</returns>
    public abstract Token NewToken();

    /// <summary>
    /// Returns this token instance to the pool.
    /// </summary>
    public abstract void ReturnToken();
    
#region Overrides of PoolResettable
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
        
    }
#endregion
}