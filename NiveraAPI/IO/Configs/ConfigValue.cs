using System.Reflection;
using NiveraAPI.Extensions;

namespace NiveraAPI.IO.Configs;

/// <summary>
/// Represents a configuration value that can be bound to a field or property.
/// This struct allows for dynamic setting and retrieval of values for
/// application configuration and customization purposes.
/// </summary>
public struct ConfigValue
{
    /// <summary>
    /// Represents a delegate used to define the logic for setting a value on an object instance.
    /// </summary>
    /// <param name="instance">The instance of the object on which the value will be set. Can be null for static members.</param>
    /// <param name="value">The value to be assigned to the specified member.</param>
    public delegate void Setter(object? instance, object value);

    /// <summary>
    /// Represents a delegate used to define the logic for retrieving a value from an object instance.
    /// </summary>
    /// <param name="instance">The instance of the object from which the value will be retrieved. Can be null for static members.</param>
    /// <returns>The value retrieved from the specified member.</returns>
    public delegate object Getter(object? instance);

    /// <summary>
    /// A delegate responsible for defining the logic to set a value on a specific object instance
    /// or a static member of a class.
    /// </summary>
    public readonly Setter SetValue;

    /// <summary>
    /// A delegate responsible for defining the logic to retrieve a value from a specific object instance
    /// or a static member of a class.
    /// </summary>
    public readonly Getter GetValue;

    /// <summary>
    /// The field that this config value is bound to.
    /// </summary>
    public readonly FieldInfo? Field;

    /// <summary>
    /// The property that this config value is bound to.
    /// </summary>
    public readonly PropertyInfo? Property;

    /// <summary>
    /// The section that this config value belongs to.
    /// </summary>
    public readonly string? Section;

    /// <summary>
    /// The key of this config value.
    /// </summary>
    public readonly string Key;

    /// <summary>
    /// The comment associated with this config value.
    /// </summary>
    public readonly string? Comment;

    /// <summary>
    /// The .NET type of the value associated with this configuration entry, as determined from the
    /// linked field or property.
    /// </summary>
    public Type? ValueType => Field?.FieldType ?? Property?.PropertyType;

    /// <summary>
    /// Retrieves the type that declares the field or property associated with this configuration value.
    /// </summary>
    public Type? DeclaringType => Field?.DeclaringType ?? Property?.DeclaringType;

    /// <summary>
    /// Whether this config value is bound to a field.
    /// </summary>
    public bool IsField => Field != null;
    
    /// <summary>
    /// Whether this config value is bound to a property.
    /// </summary>
    public bool IsProperty => Property != null;

    /// <summary>
    /// Whether this config value is valid and can be used.
    /// </summary>
    public bool IsValid => SetValue != null && GetValue != null && (IsField || IsProperty);

    /// <summary>
    /// Gets the full name of the associated field or property, including its declaring type
    /// if applicable. This provides a string representation of the member for identification
    /// purposes, supporting both fields and properties.
    /// </summary>
    public string? Member => IsField ? Field.GetMemberName() : Property.GetMemberName();

    /// <summary>
    /// Creates a new <see cref="ConfigValue"/> instance.
    /// </summary>
    /// <param name="section">The section of the config key.</param>
    /// <param name="key">The key of the config value.</param>
    /// <param name="comment">The comment of the config value.</param>
    /// <param name="field">The field to bind the value to.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ConfigValue(string? section, string key, string? comment, FieldInfo field)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));
        
        if (field == null)
            throw new ArgumentNullException(nameof(field));

        Key = key;
        Field = field;
        Section = section;
        Comment = comment;
        
        GetValue = field.GetValue;
        SetValue = field.SetValue;
    }

    /// <summary>
    /// Creates a new <see cref="ConfigValue"/> instance.
    /// </summary>
    /// <param name="section">The section of the config key.</param>
    /// <param name="key">The key of the config value.</param>
    /// <param name="comment">The comment of the config value.</param>
    /// <param name="property">The property to bind the value to.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ConfigValue(string? section, string key, string? comment, PropertyInfo property)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));
        
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        Key = key;
        Section = section;
        Comment = comment;
        Property = property;
        
        GetValue = property.GetValue;
        SetValue = property.SetValue;
    }
}