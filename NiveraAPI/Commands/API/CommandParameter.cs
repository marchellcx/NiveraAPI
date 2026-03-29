using System.Reflection;
using NiveraAPI.Commands.Attributes;
using NiveraAPI.Pooling;
using NiveraAPI.TypeParsing;
using NiveraAPI.Utilities;
using NiveraAPI.Extensions;

namespace NiveraAPI.Commands.API;

/// <summary>
/// Represents a parameter of a command overload.
/// </summary>
public class CommandParameter
{
    /// <summary>
    /// Represents the attributes associated with a command parameter,
    /// including the parameter's metadata, main attribute, and sub-attributes.
    /// </summary>
    public struct ParameterAttributes
    {
        /// <summary>
        /// The targeted parameter.
        /// </summary>
        public ParameterInfo Target;

        /// <summary>
        /// The main attribute of the parameter.
        /// </summary>
        public ParameterAttribute? MainAttribute;

        /// <summary>
        /// The sub-attributes of the parameter.
        /// </summary>
        public readonly List<ParameterSubAttribute> SubAttributes;

        /// <summary>
        /// Creates a new instance of the <see cref="ParameterAttributes"/> struct.
        /// </summary>
        public ParameterAttributes()
            => SubAttributes = ListPool<ParameterSubAttribute>.Shared.Rent();

        /// <summary>
        /// Retrieves a list of <see cref="ParameterAttributes"/> associated with the parameters of a given method.
        /// </summary>
        /// <param name="method">The method for which to retrieve the parameter attributes.</param>
        /// <param name="parameters">The parameters of the method.</param>       
        /// <returns>A list of <see cref="ParameterAttributes"/> containing information about the attributes of the method's parameters.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="method"/> parameter is null.</exception>
        public static List<ParameterAttributes> GetAttributes(MethodInfo method, ParameterInfo[] parameters)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));           

            var list = ListPool<ParameterAttributes>.Shared.Rent();

            if (parameters.Length < 1)
                return list;

            var attributes = method.GetCustomAttributes(false);
            
            if (attributes.Length < 1)
                return list;

            var position = 0;
            
            ParameterAttributes current = default;

            for (var x = 0; x < attributes.Length; x++)
            {
                var attribute = attributes[x];

                if (attribute is ParameterAttribute parameterAttribute)
                {
                    if (current.Target != null
                        && current.MainAttribute != null
                        && current.SubAttributes != null)
                        list.Add(current);

                    if (position < parameters.Length)
                    {
                        current = new();
                        current.Target = parameters[position];
                        current.MainAttribute = parameterAttribute;
                        
                        position++;
                    }
                    else
                    {
                        current = default;
                    }
                }
                else if (attribute is ParameterSubAttribute parameterSubAttribute)
                {
                    if (current.Target != null 
                        && current.MainAttribute != null
                        && current.SubAttributes != null)
                    {
                        current.SubAttributes.Add(parameterSubAttribute);
                    }
                }
            }
            
            if (current.Target != null
                && current.MainAttribute != null
                && current.SubAttributes != null
                && !list.Any(x => x.Target == current.Target))
                list.Add(current);
            
            return list;
        }
    }
    
    /// <summary>
    /// Gets the name of the parameter.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Gets the description of the parameter.
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Gets the type of the parameter.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Whether or not the parameter is optional.
    /// </summary>
    public bool IsOptional { get; }

    /// <summary>
    /// Gets the default value of the parameter.
    /// </summary>
    public object? DefaultValue { get; }

    /// <summary>
    /// Gets the parameter's information.
    /// </summary>
    public ParameterInfo Info { get; }

    /// <summary>
    /// Gets the parameter's position in the command's overload.
    /// </summary>
    public int PositionIndex => Info.Position;

    /// <summary>
    /// Gets the zero-based index of the parameter in the command context.
    /// </summary>
    public int ContextIndex => PositionIndex - 1;

    /// <summary>
    /// Gets the parsers associated with the parameter.
    /// </summary>
    public Dictionary<ParameterParser, PropertyPath?> Parsers { get; } = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandParameter"/> class.
    /// </summary>
    /// <param name="info">The parameter information.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="description">The description of the parameter.</param>
    public CommandParameter(ParameterInfo info, string? name, string? description)
    {
        Info = info;
        Type = info.ParameterType;
        IsOptional = info.IsOptional;
        DefaultValue = info.DefaultValue;
        
        Name = name ?? info.Name;
        Description = description ?? string.Empty;
    }
}