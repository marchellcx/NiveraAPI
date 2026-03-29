namespace NiveraAPI.Networking.Entities.Attributes;

/// <summary>
/// An attribute used to mark a field for network synchronization.
/// </summary>
[AttributeUsage(AttributeTargets.Field, 
    AllowMultiple = false,
    Inherited = true)]
public class SyncVarAttribute : Attribute { }