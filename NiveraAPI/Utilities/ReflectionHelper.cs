using System.Diagnostics;
using System.Reflection;
using NiveraAPI.Extensions;

namespace NiveraAPI.Utilities;

/// <summary>
/// Utilities targeting the reflection API.
/// </summary>
public static class ReflectionHelper
{
	static ReflectionHelper()
	{
		PrimitiveTypeCodes = new List<TypeCode>
		{
			TypeCode.Boolean,
			TypeCode.Byte,
			TypeCode.SByte,
			TypeCode.Int16,
			TypeCode.UInt16,
			TypeCode.Int32,
			TypeCode.UInt32,
			TypeCode.Int64,
			TypeCode.UInt64,
			TypeCode.Single,
			TypeCode.Double,
			TypeCode.Decimal,
			TypeCode.DateTime,
			TypeCode.Char,
			TypeCode.String
		};
		
		PrimitiveTypes = PrimitiveTypeCodes.Select(x => x.Type()).ToList();
	}
	
	/// <summary>
	/// Represents a combination of BindingFlags that includes instance, static, public, and non-public members.
	/// </summary>
	public const BindingFlags AllFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
	
    /// <summary>
    /// Gets called once a new type is discovered.
    /// </summary>
    public static event Action<Type>? Discovered;

    /// <summary>
    /// Gets a list of all loaded types.
    /// </summary>
    public static volatile Type[] Types = Array.Empty<Type>();
    
    /// <summary>
    /// Gets a list of all loaded assemblies.
    /// </summary>
    public static volatile Assembly[] Assemblies = Array.Empty<Assembly>();

    /// <summary>
    /// Represents a read-only list of <see cref="TypeCode"/> values that correspond to primitive types in .NET.
    /// </summary>
    public static IReadOnlyList<TypeCode> PrimitiveTypeCodes { get; }

    /// <summary>
    /// Provides a collection of types corresponding to primitive TypeCodes, including types such as Boolean, Byte, Int32, Double, String, and others.
    /// </summary>
    public static IReadOnlyList<Type> PrimitiveTypes { get; }

    /// <summary>
    /// Retrieves the method currently executing, optionally skipping a specified number of stack frames.
    /// </summary>
    /// <param name="skipFrames">
    /// The number of stack frames to skip. Use 0 to get the method directly calling this method, or increase
    /// the value to retrieve methods further up the call stack.
    /// </param>
    /// <returns>
    /// The <see cref="MethodBase"/> of the method currently executing, or null if the method cannot be resolved.
    /// </returns>
    public static MethodBase? GetExecutingMethod(int skipFrames = 0)
	    => Exceptions.StackMethods.Skip(skipFrames + 1).FirstOrDefault();

    /// <summary>
    /// Retrieves the full stack trace of the current execution context as a string.
    /// </summary>
    /// <returns>
    /// A string representation of the current stack trace.
    /// </returns>
    public static string FullTrace()
	    => Exceptions.StackToString(Exceptions.Trace, false);

    /// <summary>
    /// Attempts to load a <see cref="Type"/> instance by its fully qualified name.
    /// </summary>
    /// <param name="typeName">
    /// The fully qualified name of the type to load.
    /// </param>
    /// <param name="type">
    /// When the method returns, contains the <see cref="Type"/> instance
    /// corresponding to the specified name, if found; otherwise, null.
    /// </param>
    /// <returns>
    /// True if the type was successfully loaded; otherwise, false.
    /// </returns>
    public static bool TryLoadType(string typeName, out Type type)
    {
	    try
	    {
		    type = System.Type.GetType(typeName);
		    
		    if (type != null)
				return true;
		}
		catch
		{
			// ignored
		}
	    
		var loadedAssemblies = GetLoadedAssemblies();
		
		try
		{
			foreach (var item in loadedAssemblies)
			{
				try
				{
					var types = item.GetTypes();
					
					foreach (var type2 in types)
					{
						try
						{
							if (type2.FullName == typeName)
							{
								type = type2;
								return true;
							}
						}
						catch
						{
							 // ignored
						}
					}
				}
				catch
				{
					 // ignored
				}
			}
		}
		catch
		{
			 // ignored
		}
		
		type = null!;
		return false;
	}

    /// <summary>
    /// Converts an action that targets a proxy type into an action that targets a specified type,
    /// with an optional allowance for null values.
    /// </summary>
    /// <param name="toProxy">
    /// The action to be executed on the proxy type.
    /// </param>
    /// <param name="allowNull">
    /// Indicates whether null values for the specified type should be allowed. If true and the input
    /// is null, the action will be invoked with the default value of the proxy type.
    /// </param>
    /// <typeparam name="TType">
    /// The original type of the input.
    /// </typeparam>
    /// <typeparam name="TProxy">
    /// The proxy type that the action targets.
    /// </typeparam>
    /// <returns>
    /// A delegate that executes the specified action after safely converting or handling the input type.
    /// </returns>
    public static Action<TType> TypeProxy<TType, TProxy>(this Action<TProxy> toProxy, bool allowNull = false)
    {
	    return x =>
	    {
		    if (x == null)
		    {
			    if (allowNull)
			    {
				    toProxy?.Invoke(default!);
			    }
			    else
			    {
				    throw new NullReferenceException("Input object is null and null values are not allowed.");
			    }
			}
			else if (x is TProxy obj)
			{
				toProxy?.Invoke(obj);
			}
			else
			{
				throw new Exception($"Unexpected type {x.GetType().FullName} for proxy type {typeof(TProxy).FullName}");
			}
		};
	}

    /// <summary>
    /// Creates a proxy for an action delegate, wrapping an action of a specific type into an action
    /// that accepts an object as its parameter.
    /// </summary>
    /// <param name="toProxy">
    /// The action delegate to be proxied, which operates on a specific type.
    /// </param>
    /// <param name="allowNull">
    /// A boolean flag indicating whether null values should be allowed and passed as default values
    /// to the proxied action.
    /// </param>
    /// <typeparam name="T">
    /// The specific type of the parameter accepted by the proxied action.
    /// </typeparam>
    /// <returns>
    /// An action delegate that accepts an object parameter, performing type checking and invoking the
    /// original action only if the input object is of the correct type or null.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown if the input object is not of the expected type and null is not allowed.
    /// </exception>
    public static Action<object> ObjectProxy<T>(this Action<T> toProxy, bool allowNull = true)
    {
	    return x =>
	    {
		    if (x == null)
		    {
			    if (allowNull)
			    {
				    toProxy?.Invoke(default!);
			    }
			    else
			    {
				    throw new NullReferenceException("Input object is null and null values are not allowed.");
			    }
		    }
		    else if (x is T obj)
		    {
				toProxy?.Invoke(obj);
			}
			else
			{
				throw new Exception($"Unexpected type {x.GetType().FullName} for proxy type {typeof(T).FullName}");
			}
		};
	}

    /// <summary>
    /// Creates a proxy function that wraps around a strongly-typed function and converts its return type to an object.
    /// </summary>
    /// <param name="toProxy">
    /// The function to proxy. This function returns a value of type <typeparamref name="T"/>.
    /// </param>
    /// <typeparam name="T">
    /// The return type of the original function.
    /// </typeparam>
    /// <returns>
    /// A new function that, when invoked, executes the original function and returns its result as an object.
    /// </returns>
    public static Func<object> ObjectProxy<T>(this Func<T> toProxy)
    {
	    return () =>
	    {
		    var val = toProxy();
		    return (val == null) ? null! : ((object)val);
		};
	}

    /// <summary>
    /// Determines whether the specified parameter is marked with the <see cref="ParamArrayAttribute"/>.
    /// </summary>
    /// <param name="parameter">
    /// The <see cref="ParameterInfo"/> object representing the parameter to check.
    /// </param>
    /// <returns>
    /// True if the parameter is marked with the <see cref="ParamArrayAttribute"/>; otherwise, false.
    /// </returns>
    public static bool IsParam(this ParameterInfo parameter)
	    => parameter.IsDefined(typeof(ParamArrayAttribute), inherit: false);

    /// <summary>
    /// Determines if any of the generic type parameters of a method have specific generic parameter attributes.
    /// </summary>
    /// <param name="method">
    /// The method to inspect for generic parameter attributes.
    /// </param>
    /// <param name="genericParameterAttributes">
    /// The <see cref="GenericParameterAttributes"/> to check against the method's generic type parameters.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether any of the generic type parameters of the method match the specified
    /// <see cref="GenericParameterAttributes"/>.
    /// </returns>
    public static bool IsConstraint(this MethodInfo method, GenericParameterAttributes genericParameterAttributes)
    {
	    method = method.MakeGenericMethod();

	    var genericArguments = method.GetGenericArguments();
		
		foreach (Type type in genericArguments)
		{
			if (type.GenericParameterAttributes == genericParameterAttributes || type.GenericParameterAttributes.HasFlag(genericParameterAttributes))
			{
				return true;
			}
		}
		
		return false;
	}

    /// <summary>
    /// Sets the value of a specified field on a given type.
    /// </summary>
    /// <param name="type">
    /// The type that contains the field to be set.
    /// </param>
    /// <param name="fieldName">
    /// The name of the field to be set.
    /// </param>
    /// <param name="value">
    /// The value to assign to the field.
    /// </param>
    /// <param name="handle">
    /// An optional instance of the object on which the field should be set, or null if the field is static.
    /// </param>
    public static void SetField(this Type type, string fieldName, object value, object? handle = null)
    {
	    type.TrySetField(fieldName, value, handle!);
    }

    /// <summary>
    /// Sets the value of a specified field on a given type.
    /// </summary>
    /// <param name="type">
    /// The type on which the field is defined.
    /// </param>
    /// <param name="fieldName">
    /// The name of the field to be set.
    /// </param>
    /// <param name="value">
    /// The value to assign to the field.
    /// </param>
    /// <param name="handle">
    /// The object instance that holds the field if the field belongs to an instance. Use <c>null</c> for static fields.
    /// </param>
    public static void SetField<T>(string fieldName, object value, T? handle = default(T))
    {
	    typeof(T).SetField(fieldName, value, handle);
    }

    /// <summary>
    /// Attempts to set the value of a specified field on a given type.
    /// </summary>
    /// <param name="type">
    /// The <see cref="Type"/> on which the field is defined.
    /// </param>
    /// <param name="fieldName">
    /// The name of the field to set the value for.
    /// </param>
    /// <param name="value">
    /// The value to assign to the field.
    /// </param>
    /// <param name="handle">
    /// An optional object instance used for accessing the field, typically required for instance fields
    /// on non-static types. Defaults to null for static fields or when no instance is needed.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the operation was successful. Returns true if the field was
    /// successfully set; otherwise, false.
    /// </returns>
    public static bool TrySetField(this Type type, string fieldName, object value, object? handle = null)
    {
	    try
	    {
		    var fieldInfo = type.FindField(fieldName);
		    
		    if (fieldInfo == null)
			    return false;

		    if (handle == null || !fieldInfo.IsStatic)
			    return false;
		    
			fieldInfo.SetValue(value, handle);
			return true;
		}
		catch
		{
			return false;
		}
	}

    /// <summary>
    /// Sets the value of a specified property on the provided type instance.
    /// </summary>
    /// <param name="type">
    /// The type containing the property to set.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property to set.
    /// </param>
    /// <param name="value">
    /// The value to assign to the property.
    /// </param>
    /// <param name="handle">
    /// An optional instance of the object on which the property exists. If the property is static, this parameter can be null.
    /// </param>
    public static void SetProperty(this Type type, string propertyName, object value, object? handle = null)
    {
	    type.TrySetProperty(propertyName, value, handle);
    }

    /// <summary>
    /// Sets the value of a specified property on a given type, using an optional instance handle
    /// for non-static properties.
    /// </summary>
    /// <param name="type">
    /// The type that contains the property to set.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property to set.
    /// </param>
    /// <param name="value">
    /// The value to assign to the property.
    /// </param>
    /// <param name="handle">
    /// An optional instance of the type if the property is not static. Pass null for static properties.
    /// </param>
    public static void SetProperty<T>(string propertyName, object value, T? handle = default(T))
    {
	    typeof(T).TrySetProperty(propertyName, value, handle);
    }

    /// <summary>
    /// Attempts to set the value of a specified property on a given type instance.
    /// </summary>
    /// <param name="type">
    /// The type that contains the property to be set.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property whose value needs to be set.
    /// </param>
    /// <param name="value">
    /// The value to be assigned to the property.
    /// </param>
    /// <param name="handle">
    /// The instance of the type on which the property value will be set. Pass null for static properties.
    /// </param>
    /// <returns>
    /// True if the property was successfully set; otherwise, false.
    /// </returns>
    public static bool TrySetProperty(this Type type, string propertyName, object value, object? handle = null)
    {
	    var propertyInfo = type.FindProperty(propertyName);

	    if (propertyInfo == null)
		    return false;

	    try
		{
			propertyInfo.SetValue(handle, value);
			return true;
		}
		catch
		{
			return false;
		}
	}

    /// <summary>
    /// Retrieves the <see cref="Type"/> object associated with the specified type name.
    /// </summary>
    /// <param name="typeName">
    /// The fully qualified name of the type to retrieve. This includes the namespace but not the assembly name.
    /// </param>
    /// <returns>
    /// The <see cref="Type"/> object corresponding to the specified type name if found, or null if the type cannot be resolved.
    /// </returns>
    public static Type Type(string typeName)
    {
	    return System.Type.GetType(typeName);
    }

    /// <summary>
	/// Retrieves the <see cref="Type"/> associated with the specified <see cref="TypeCode"/>.
	/// </summary>
	/// <param name="typeCode">
	/// The <see cref="TypeCode"/> to retrieve the corresponding <see cref="Type"/> for.
	/// </param>
	/// <returns>
	/// The <see cref="Type"/> corresponding to the specified <see cref="TypeCode"/>, or null if no corresponding type exists.
	/// </returns>
	public static Type? Type(this TypeCode typeCode)
	{
		return typeCode switch
		{
			TypeCode.Byte => typeof(byte), 
			TypeCode.SByte => typeof(sbyte), 
			TypeCode.Int16 => typeof(short), 
			TypeCode.UInt16 => typeof(ushort), 
			TypeCode.Int32 => typeof(int), 
			TypeCode.UInt32 => typeof(uint), 
			TypeCode.Int64 => typeof(long), 
			TypeCode.UInt64 => typeof(ulong), 
			TypeCode.Single => typeof(float), 
			TypeCode.Double => typeof(double), 
			TypeCode.Decimal => typeof(decimal), 
			TypeCode.DateTime => typeof(DateTime), 
			TypeCode.Char => typeof(char), 
			TypeCode.String => typeof(string), 
			TypeCode.Boolean => typeof(bool), 
			
			_ => null, 
		};
	}

	/// <summary>
	/// Retrieves an assembly from the currently loaded assemblies by its name or partial name.
	/// </summary>
	/// <param name="assemblyName">
	/// The name or partial name of the assembly to retrieve. This is case-insensitive and can match the full name
	/// or the short name of the assembly.
	/// </param>
	/// <returns>
	/// The <see cref="Assembly"/> that matches the given name, or null if no such assembly is found.
	/// </returns>
	public static Assembly Assembly(string assemblyName)
	{
		return GetLoadedAssemblies().FirstOrDefault(x => 
			x.FullName.ToLower().Contains(assemblyName.ToLower()) 
				|| x.GetName().Name.ToLower().Contains(assemblyName.ToLower()));
	}

	/// <summary>
	/// Retrieves the set of assemblies currently loaded in the application domain.
	/// </summary>
	/// <returns>
	/// A <see cref="HashSet{T}"/> containing the assemblies loaded into the application domain. If the loaded assemblies cannot be accessed,
	/// the resulting set may be incomplete or empty.
	/// </returns>
	public static HashSet<Assembly> GetLoadedAssemblies()
	{
		var hashSet = new HashSet<Assembly>();
		
		try
		{
			if (AppDomain.CurrentDomain != null)
			{
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				
				foreach (var item in assemblies)
				{
					try
					{
						hashSet.Add(item);
					}
					catch
					{
						// ignored
					}
				}
			}
		}
		catch
		{
			// ignored
		}

		try
		{
			var callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
			var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();

			hashSet.Add(callingAssembly);
			hashSet.Add(executingAssembly);
		}
		catch
		{
			// ignored
		}

		return hashSet;
	}
	
    /// <summary>
    /// Attempts to find a type by its name among the currently loaded types, with an option
    /// to perform a case-insensitive comparison.
    /// </summary>
    /// <param name="name">The name of the type to search for. This can be the full name, short name, or assembly-qualified name of the type.</param>
    /// <param name="ignoreCase">true to perform a case-insensitive comparison of the name; false to require an exact match.</param>
    /// <param name="type">When this method returns, contains the type that matches the specified name, if found; otherwise, null.</param>
    /// <returns>true if a type matching the specified name is found; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided name is null, empty, or consists only of whitespace.</exception>
    public static bool TryFindType(string name, bool ignoreCase, out Type? type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        type = null;

        for (var x = 0; x < Types.Length; x++)
        {
            var cur = Types[x];

            if (cur.FullName == name
                || cur.Name == name
                || cur.AssemblyQualifiedName == name
                || (ignoreCase && (cur.FullName ?? string.Empty).Equals(name, StringComparison.OrdinalIgnoreCase)
                    || cur.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    || (cur.AssemblyQualifiedName ?? string.Empty).Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                type = cur;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the assembly of the calling method in the current call stack, with options to skip frames and filter
    /// assemblies.
    /// </summary>
    /// <remarks>This method can be used to identify the assembly that invoked the current code, which is
    /// useful for scenarios such as plugin discovery or diagnostics. The skipFrameCount parameter allows you to control
    /// how many stack frames to skip, which can be helpful when wrapping this method in utility functions. If
    /// ignoreAssembly is provided, any assemblies for which the predicate returns true will be skipped when searching
    /// for the caller.</remarks>
    /// <param name="skipFrameCount">The number of stack frames to skip before determining the caller assembly. Must be zero or greater.</param>
    /// <param name="throwIfNotFound">true to throw an exception if no suitable assembly is found; otherwise, false.</param>
    /// <param name="ignoreAssembly">A predicate used to exclude specific assemblies from consideration. If null, no assemblies are ignored.</param>
    /// <returns>The assembly of the first calling method in the stack that is not ignored by the specified predicate.</returns>
    /// <exception cref="Exception">Thrown if no suitable calling assembly is found and throwIfNotFound is true.</exception>
    public static Assembly? GetCallerAssembly(int skipFrameCount, bool throwIfNotFound, Predicate<Assembly>? ignoreAssembly = null)
    {
        var frames = new StackTrace().GetFrames();

        for (var i = 0 + skipFrameCount; i < frames.Length; i++)
        {
            var method = frames[i].GetMethod();

            if (method is null)
                continue;

            var assembly = method.DeclaringType?.Assembly ?? method.ReflectedType.Assembly;

            if (ignoreAssembly is null || !ignoreAssembly(assembly))
                return assembly;
        }

        if (throwIfNotFound)
            throw new Exception("Could not find caller assembly.");

        return null!;
    }

    /// <summary>
    /// Retrieves the method calling this method, with optional skipping of specific stack frames, and optional filtering based on a predicate.
    /// </summary>
    /// <param name="skipFrameCount">
    /// The number of stack frames to skip. Use 0 to get the direct caller of this method, or increase this value to retrieve methods further up the call stack.
    /// </param>
    /// <param name="throwIfNotFound">
    /// Indicates whether to throw an exception if no suitable calling method is found.
    /// </param>
    /// <param name="ignoreMethod">
    /// A predicate to exclude specific methods from being considered as the caller. If null, no methods are excluded.
    /// </param>
    /// <returns>
    /// The <see cref="MethodBase"/> of the calling method, or null if no method is found and <paramref name="throwIfNotFound"/> is false.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when no method is found and <paramref name="throwIfNotFound"/> is true.
    /// </exception>
    public static MethodBase? GetCallerMethod(int skipFrameCount, bool throwIfNotFound,
	    Predicate<MethodBase>? ignoreMethod = null)
    {
	    var frames = new StackTrace().GetFrames();

	    if (frames?.Length < 1)
		    return null;

	    for (var i = 0 + skipFrameCount; i < frames.Length; i++)
	    {
		    var method = frames[i].GetMethod();

		    if (method is null)
			    continue;

		    if (ignoreMethod is null || !ignoreMethod(method))
			    return method;
	    }

	    if (throwIfNotFound)
		    throw new Exception("Could not find caller method.");

	    return null;
    }

    /// <summary>
    /// Invokes the <see cref="Discovered"/> event for all types in the <see cref="Types"/> collection.
    /// </summary>
    /// <remarks>
    /// This method iterates through the <see cref="Types"/> collection and triggers the <see cref="Discovered"/> event
    /// for each <see cref="Type"/>. Exceptions thrown by individual event handlers are caught and ignored.
    /// </remarks>
    /// <exception cref="NullReferenceException">Thrown if the <see cref="Discovered"/> event is null when invoked. This should generally not occur unless the method is improperly modified.</exception>
    public static void CallDiscovered()
    {
        if (Discovered != null)
        {
            foreach (var type in Types)
            {
                try
                {
                    Discovered.Invoke(type);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    private static void OnLoaded(object _, AssemblyLoadEventArgs ev)
    {
        try
        {
            Assemblies = Assemblies
                .Append(ev.LoadedAssembly)
                .ToArray();
            
            var types = ev.LoadedAssembly.GetTypes();
            
            Types = types.Where(x => !Types.Contains(x))
                .Concat(Types)
                .ToArray();
            
            foreach (var type in types)
            {
                try
                {
                    Discovered?.Invoke(type);
                }
                catch
                {
                    // ignored
                }
            }
        }
        catch
        {
            // ignored
        }
    }
    
    internal static void Initialize()
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Assemblies = assemblies.ToArray();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    Types = Types
                        .Concat(assembly.GetTypes())
                        .ToArray();
                }
                catch
                {
                    // ignored
                }
            }
        }
        catch
        {
            // ignored
        }

        AppDomain.CurrentDomain.AssemblyLoad += OnLoaded;
    }
}