namespace NiveraAPI.IO;

/// <summary>
/// Provides methods and properties for working with directories, including directory creation,
/// deletion, enumeration of files and subdirectories, and access to directory metadata.
/// </summary>
public class DirectoryManager
{
	private DirectoryInfo _dirInfo;

	/// <summary>
	/// Provides a static, read-only dictionary that maps <see cref="Environment.SpecialFolder"/> values
	/// to corresponding <see cref="DirectoryManager"/> instances, allowing access to standard system directories.
	/// </summary>
	public static IReadOnlyDictionary<Environment.SpecialFolder, DirectoryManager> Directories { get; }

	/// <summary>
	/// Gets the directory manager instance representing the current working directory of the application.
	/// </summary>
	public static DirectoryManager Current { get; }

	/// <summary>
	/// Gets the directory manager instance for the system directory of the operating system.
	/// </summary>
	public static DirectoryManager System { get; }

	/// <summary>
	/// Gets the directory manager instance for the roaming application data directory of the current user.
	/// </summary>
	public static DirectoryManager Roaming { get; }

	/// <summary>
	/// Gets the directory manager instance for the local application data directory of the current user.
	/// </summary>
	public static DirectoryManager Local { get; }

	/// <summary>
	/// Gets the full path of the directory represented by this instance.
	/// </summary>
	public string Path => _dirInfo.FullName;

	/// <summary>
	/// Gets the name of the directory represented by this instance.
	/// </summary>
	public string Name => _dirInfo.Name;

	/// <summary>
	/// Gets a value indicating whether the directory represented by this instance exists.
	/// </summary>
	public bool Exists => _dirInfo.Exists;

	/// <summary>
	/// Gets or sets the last write time of the directory represented by this instance.
	/// </summary>
	public DateTime LastWriteTime
	{
		get
		{
			return _dirInfo.LastWriteTime;
		}
		set
		{
			Directory.SetLastWriteTime(Path, value);
		}
	}

	/// <summary>
	/// Gets or sets the last access time of the directory represented by this instance.
	/// </summary>
	public DateTime LastAccessTime
	{
		get
		{
			return _dirInfo.LastAccessTime;
		}
		set
		{
			Directory.SetLastAccessTime(Path, value);
		}
	}

	/// <summary>
	/// Gets or sets the creation time of the directory represented by this instance.
	/// </summary>
	public DateTime CreationTime
	{
		get
		{
			return _dirInfo.CreationTime;
		}
		set
		{
			Directory.SetCreationTime(Path, value);
		}
	}

	/// <summary>
	/// Gets the parent directory of the current directory represented by this instance.
	/// </summary>
	public DirectoryManager Parent { get; }

	/// <summary>
	/// Gets the root directory of the current directory represented by this instance.
	/// </summary>
	public DirectoryManager Root { get; }

	/// <summary>
	/// Initializes a new instance of the DirectoryManager class with the specified directory path.
	/// </summary>
	public DirectoryManager(string directoryPath)
	{
		_dirInfo = new DirectoryInfo(directoryPath);
		Parent = new DirectoryManager(_dirInfo.Parent.FullName);
		Root = new DirectoryManager(_dirInfo.Root.FullName);
	}

	static DirectoryManager()
	{
		var dictionary = new Dictionary<Environment.SpecialFolder, DirectoryManager>();
		
		foreach (var item in Enum.GetValues(typeof(Environment.SpecialFolder)).Cast<Environment.SpecialFolder>())
			dictionary[item] = Get(item);
		
		Directories = dictionary;
		
		Roaming = Directories[Environment.SpecialFolder.ApplicationData];
		Local = Directories[Environment.SpecialFolder.LocalApplicationData];
		
		Current = new DirectoryManager(Directory.GetCurrentDirectory());
		System = new DirectoryManager(Environment.SystemDirectory);
	}

	/// <summary>
	/// Retrieves a list of directories within the directory represented by this instance.
	/// </summary>
	/// <returns>A list of <see cref="DirectoryManager"/> objects representing the directories in the current directory.</returns>
	public List<DirectoryManager> GetDirectories()
	{
		return (from x in _dirInfo.EnumerateDirectories()
			select new DirectoryManager(x.FullName)).ToList();
	}

	/// <summary>
	/// Retrieves a list of directories within the directory represented by this instance.
	/// </summary>
	/// <returns>A list of <see cref="DirectoryManager"/> objects representing the directories in the current directory.</returns>
	public List<DirectoryManager> GetDirectories(string searchPattern)
	{
		return (from x in _dirInfo.EnumerateDirectories(searchPattern)
			select new DirectoryManager(x.FullName)).ToList();
	}

	/// <summary>
	/// Retrieves a list of directories within the directory represented by this instance.
	/// </summary>
	/// <returns>A list of <see cref="DirectoryManager"/> objects representing the directories in the current directory.</returns>
	public List<DirectoryManager> GetDirectories(string searchPattern, SearchOption searchOption)
	{
		return (from x in _dirInfo.EnumerateDirectories(searchPattern, searchOption)
			select new DirectoryManager(x.FullName)).ToList();
	}

	/// <summary>
	/// Retrieves a list of files within the directory represented by this instance.
	/// </summary>
	/// <returns>A list of <see cref="FileManager"/> objects representing the files in the directory.</returns>
	public List<FileManager> GetFiles()
	{
		return (from x in _dirInfo.EnumerateFiles()
			select new FileManager(x.FullName)).ToList();
	}

	/// <summary>
	/// Retrieves a list of files within the directory managed by this instance.
	/// </summary>
	/// <returns>A list of <see cref="FileManager"/> objects representing the files in the directory.</returns>
	public List<FileManager> GetFiles(string searchPattern)
	{
		return (from x in _dirInfo.EnumerateFiles(searchPattern)
			select new FileManager(x.FullName)).ToList();
	}

	/// <summary>
	/// Retrieves a list of files within the directory managed by this instance.
	/// </summary>
	/// <returns>A list of <see cref="FileManager"/> objects representing the files in the directory.</returns>
	public List<FileManager> GetFiles(string searchPattern, SearchOption searchOption)
	{
		return (from x in _dirInfo.EnumerateFiles(searchPattern, searchOption)
			select new FileManager(x.FullName)).ToList();
	}

	/// <summary>
	/// Creates the directory managed by this instance if it does not already exist.
	/// </summary>
	public void Create()
	{
		if (!Exists)
		{
			_dirInfo.Create();
		}
	}

	/// <summary>
	/// Deletes the directory managed by this instance and, if necessary, its contents.
	/// </summary>
	public void Delete()
	{
		if (Exists)
		{
			_dirInfo.Delete(recursive: true);
		}
	}

	/// <summary>
	/// Creates a subdirectory within the directory managed by this instance.
	/// </summary>
	/// <param name="path">The name or relative path of the subdirectory to create.</param>
	public void CreateSubdirectory(string path)
	{
		_dirInfo.CreateSubdirectory(path);
	}

	/// <summary>
	/// Moves the directory managed by this instance to a new location.
	/// </summary>
	/// <param name="path">The destination path where the directory should be moved.</param>
	public void Move(string path)
	{
		_dirInfo.MoveTo(path);
	}

	/// <summary>
	/// Returns a string representation of the directory path managed by this instance.
	/// </summary>
	/// <returns>A string containing the full directory path.</returns>
	public override string ToString()
	{
		return Path;
	}

	/// <summary>
	/// Retrieves a directory manager instance for the specified special folder.
	/// </summary>
	/// <param name="folder">The special folder from which to retrieve the directory path.</param>
	/// <returns>An instance of <see cref="DirectoryManager"/> representing the specified folder.</returns>
	public static DirectoryManager Get(Environment.SpecialFolder folder)
	{
		return new DirectoryManager(Environment.GetFolderPath(folder));
	}
}