using NiveraAPI.Networking.Database.Enums;

namespace NiveraAPI.Networking.Database.Client;

/// <summary>
/// Represents the result of a database transaction.
/// </summary>
public struct DbTransResult
{
    /// <summary>
    /// The data returned by the transaction.
    /// </summary>
    public readonly byte[]? Data;

    /// <summary>
    /// The result of the transaction.
    /// </summary>
    public readonly DbResult Result;

    /// <summary>
    /// Any exception that occurred during the transaction.
    /// </summary>
    public readonly Exception? Exception;
    
    /// <summary>
    /// Whether the transaction was successful.
    /// </summary>
    public bool IsOk => Result == DbResult.Ok;

    /// <summary>
    /// Creates a new instance of the DbTransResult struct.
    /// </summary>
    /// <param name="data">The data returned by the transaction.</param>
    /// <param name="result">The result of the transaction.</param>
    /// <param name="exception">Any exception that occurred during the transaction.</param>
    public DbTransResult(byte[]? data, DbResult result, Exception? exception)
    {
        Data = data;
        Result = result;
        Exception = exception;
    }
}