namespace NiveraAPI.Utilities;

/// <summary>
/// Represents a delegate that defines a method which performs an operation on a value type parameter passed by reference.
/// </summary>
/// <typeparam name="T">The type of the value to which the operation applies.</typeparam>
public delegate void RefAction<T>(ref T value);

/// <summary>
/// Represents a delegate that defines a method which takes a value type parameter passed by reference
/// and returns a result of the specified type.
/// </summary>
/// <typeparam name="TIn">The type of the value passed by reference.</typeparam>
/// <typeparam name="TOut">The type of the result returned by the delegate.</typeparam>
public delegate TOut RefFunc<TIn, out TOut>(ref TIn value);