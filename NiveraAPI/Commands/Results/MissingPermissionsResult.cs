using NiveraAPI.Commands.Interfaces;

namespace NiveraAPI.Commands.Results;

/// <summary>
/// Represents a result that indicates the sender does not have the required permissions to execute the command.
/// </summary>
public struct MissingPermissionsResult : IResult
{
    /// <summary>
    /// Whether or not the command's execution was successful.
    /// </summary>
    public bool Success { get; }
    
    /// <summary>
    /// The permissions required by the command.
    /// </summary>
    public string[] Permissions { get; }
    
    /// <summary>
    /// Whether or not all of the above permissions are required.
    /// </summary>
    public bool AllPermissions { get; }
    
    /// <summary>
    /// Creates a new missing permissions result.
    /// </summary>
    /// <param name="permissions">The permissions required by the command.</param>
    /// <param name="allPermissions">Whether or not all of the above permissions are required.</param>
    public MissingPermissionsResult(string[] permissions, bool allPermissions)
    {
        Success = false;
     
        Permissions = permissions;
        AllPermissions = allPermissions;
    }
}