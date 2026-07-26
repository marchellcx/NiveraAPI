using System.Reflection;

using NiveraAPI.Extensions;
using NiveraAPI.Utilities;

namespace NiveraAPI.Attributes;

/// <summary>
/// Provides methods to process, load, and unload attributes across assemblies.
/// The <see cref="AttributeLoader"/> class identifies and executes static methods in assemblies
/// that are annotated with specific attributes.
/// </summary>
public static class AttributeLoader
{
	/// <summary>
	/// Executes the loading of attributes from the specified assemblies. This method identifies all static methods
	/// in the provided assemblies that are decorated with the <see cref="LoadAttribute"/> attribute, organizes them based
	/// on the priority defined in the attribute, and invokes them in descending order of priority.
	/// </summary>
	/// <param name="assemblies">
	/// An array of assemblies to scan for static methods that have the <see cref="LoadAttribute"/> applied.
	/// Only methods from these assemblies are processed and invoked.
	/// </param>
	public static void ExecuteLoadAttributes(params Assembly[] assemblies)
	{
		var methods = Pools.PoolList<LoadData>();
		
		assemblies.ForEach(assembly =>
		{
			assembly.GetTypes().ForEach(type =>
			{
				type.GetAllMethods().ForEach(method =>
				{
					if (method.TryGetAttribute<LoadAttribute>(out var attributeValue) && method.IsStatic)
					{
						methods.Add(new LoadData(method, attributeValue.Priority));
					}
				});
			});
		});
		
		var values = methods.OrderByDescending(data => (byte)data.priority);
		
		values.ForEach(delegate(LoadData method)
		{
			try
			{
				method.target.Invoke(null, null);
			}
			catch
			{
				// ignored
			}
		});
		
		methods.ReturnToPool();
	}

	/// <summary>
	/// Executes the unloading of attributes from the specified assemblies. This method identifies all static methods
	/// in the provided assemblies that are decorated with the <see cref="UnloadAttribute"/> attribute, organizes them based
	/// on the priority defined in the attribute, and invokes them in descending order of priority.
	/// </summary>
	/// <param name="assemblies">
	/// An array of assemblies to scan for static methods that have the <see cref="UnloadAttribute"/> applied.
	/// Only methods from these assemblies are processed and invoked.
	/// </param>
	public static void ExecuteUnloadAttributes(params Assembly[] assemblies)
	{
		var methods = Pools.PoolList<LoadData>();
		
		assemblies.ForEach(assembly =>
		{
			assembly.GetTypes().ForEach(type =>
			{
				type.GetAllMethods().ForEach(method =>
				{
					if (method.TryGetAttribute<UnloadAttribute>(out var attributeValue) && method.IsStatic)
					{
						methods.Add(new LoadData(method, attributeValue.Priority));
					}
				});
			});
		});
		
		var values = methods.OrderByDescending(data => (byte)data.priority);
		
		values.ForEach(delegate(LoadData method)
		{
			try
			{
				method.target.Invoke(null, null);
			}
			catch
			{
				// ignored
			}
		});
		
		methods.ReturnToPool();
	}

	/// <summary>
	/// Executes the reloading of attributes from the specified assemblies. This method scans the provided
	/// assemblies for all static methods decorated with the <see cref="ReloadAttribute"/> attribute,
	/// prioritizes them based on the defined priority in the attribute, and invokes them in descending order of priority.
	/// </summary>
	/// <param name="assemblies">
	/// An array of assemblies to scan for static methods that are decorated with the <see cref="ReloadAttribute"/> attribute.
	/// Only methods from these assemblies are processed and invoked.
	/// </param>
	public static void ExecuteReloadAttributes(params Assembly[] assemblies)
	{
		var methods = Pools.PoolList<LoadData>();
		
		assemblies.ForEach(assembly =>
		{
			assembly.GetTypes().ForEach(type =>
			{
				type.GetAllMethods().ForEach(method =>
				{
					if (method.TryGetAttribute<ReloadAttribute>(out var attributeValue) && method.IsStatic)
					{
						methods.Add(new LoadData(method, attributeValue.Priority));
					}
				});
			});
		});
		
		var values = methods.OrderByDescending(data => (byte)data.priority);
		
		values.ForEach(delegate(LoadData method)
		{
			try
			{
				method.target.Invoke(null, null);
			}
			catch
			{
				// ignored
			}
		});
		
		methods.ReturnToPool();
	}
}