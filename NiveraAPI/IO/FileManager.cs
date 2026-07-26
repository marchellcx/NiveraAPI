using System.Diagnostics;
using System.Security.Cryptography;

using NiveraAPI.Extensions;

namespace NiveraAPI.IO;

/// <summary>
/// Provides functionality for managing and interacting with files.
/// </summary>
public class FileManager
{
	private FileInfo _file;
	private FileVersionInfo _fv;
	private DirectoryManager _directory;

	/// <summary>
	/// Provides a list of characters that are considered invalid in file or directory paths.
	/// These characters cannot be used as part of a valid file system path due to system-level restrictions.
	/// </summary>
	public static IReadOnlyList<char> PathIllegalCharacters { get; } = new char[9] { '/', '<', '>', ':', '"', '\\', '|', '?', '*' };

	/// <summary>
	/// Defines a collection of reserved file names that are invalid in file systems.
	/// These names cannot be used as filenames or directory names due to system-level constraints.
	/// </summary>
	public static IReadOnlyList<string> ReservedFileNames { get; } = new string[22]
	{
		"CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6",
		"COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7",
		"LPT8", "LPT9"
	};

	/// <summary>
	/// Represents information about a specific file, including its metadata, attributes,
	/// and properties. Provides access to the underlying file object for interaction
	/// and retrieval of file-specific details.
	/// </summary>
	public FileInfo Info => _file;

	/// <summary>
	/// Provides version information about the associated file, including details such as
	/// file version, product version, company name, comments, and other metadata.
	/// This property encapsulates details retrieved from the file's version resource.
	/// </summary>
	public FileVersionInfo VersionInfo => _fv;

	/// <summary>
	/// Retrieves the file extension of the associated file as a string.
	/// The file extension includes the period (e.g., ".txt") and indicates
	/// the file type or format.
	/// </summary>
	public string Extension => _file.Extension;

	/// <summary>
	/// Gets the full file path of the associated file as a string.
	/// This includes the directory path and the file name, providing
	/// the complete location of the file in the file system.
	/// </summary>
	public string Path => _file.FullName;

	/// <summary>
	/// Gets the name of the file, including its extension, as represented by the associated <see cref="FileInfo"/> object.
	/// This value corresponds to the last segment of the file path and is commonly used to identify the file.
	/// </summary>
	public string Name => _file.Name;

	/// <summary>
	/// Gets the original name of the file, as specified in its version information metadata.
	/// This value typically represents the name of the file when it was initially authored or built.
	/// </summary>
	public string OriginalName => _fv.OriginalFilename;

	/// <summary>
	/// Gets the internal name of the file, as specified in the file's version information metadata.
	/// This typically represents the original name or identifier used by the application at the time
	/// of the file's creation or packaging.
	/// </summary>
	public string InternalName => _fv.InternalName;

	/// <summary>
	/// Gets the hash value of the file, computed using the file's path.
	/// The hash value can be used to verify the integrity of the file or as a unique identifier
	/// for ensuring the file's contents have not been altered.
	/// </summary>
	public string Hash => CalculateHash(Path);

	/// <summary>
	/// Gets the comments associated with the file, as specified in the file's
	/// version information metadata. This value may contain additional
	/// descriptive information or notes about the file or its functionality.
	/// </summary>
	public string Comments => _fv.Comments;

	/// <summary>
	/// Gets the name of the company that produced the file, as specified in the file's
	/// version information metadata. This value typically identifies the organization
	/// or entity responsible for the creation or distribution of the file.
	/// </summary>
	public string CompanyName => _fv.CompanyName;

	/// <summary>
	/// Gets the product name associated with the file, as specified in the file's
	/// version information metadata. This value typically represents the name
	/// of the product that the file is a part of, such as a software application
	/// or a component.
	/// </summary>
	public string ProductName => _fv.ProductName;

	/// <summary>
	/// Retrieves the language information associated with the file, as specified in the
	/// file's version information metadata. This value typically indicates the language
	/// or locale for which the file is intended, such as "English (United States)" or other
	/// language-specific settings.
	/// </summary>
	public string Language => _fv.Language;

	/// <summary>
	/// Retrieves the copyright information associated with the file, as specified in the
	/// file's version information metadata. This information typically indicates the ownership
	/// or licensing details for the file or product.
	/// </summary>
	public string Copyright => _fv.LegalCopyright;

	/// <summary>
	/// Retrieves the legal trademarks associated with the file, as specified in the
	/// file's version information metadata. This information is typically used to
	/// identify trademarks or registered trademarks linked to the product.
	/// </summary>
	public string Trademark => _fv.LegalTrademarks;

	/// <summary>
	/// Retrieves the version of the file, using file version information typically embedded
	/// in the metadata of the file. This version may represent the build or release version
	/// of the file and is specific to the file itself.
	/// </summary>
	public string FileVersion => _fv.FileVersion;

	/// <summary>
	/// Retrieves the version of the product that the file is associated with,
	/// typically specifying the version of the overall application or product
	/// that the file is part of.
	/// </summary>
	public string ProductVersion => _fv.ProductVersion;

	/// <summary>
	/// Gets the special build information of the file, generally used to provide details
	/// regarding any unique modifications or configurations that distinguish this build
	/// from standard releases.
	/// </summary>
	public string SpecialBuildData => _fv.SpecialBuild;

	/// <summary>
	/// Gets the private build information of the file, typically used to convey additional details
	/// about custom builds provided by the developer or organization.
	/// </summary>
	public string PrivateBuildData => _fv.PrivateBuild;

	/// <summary>
	/// Gets the major part of the file version number.
	/// </summary>
	public int FileVersionMajor => _fv.FileMajorPart;

	/// <summary>
	/// Gets the minor part of the file version number.
	/// </summary>
	public int FileVersionMinor => _fv.FileMinorPart;

	/// <summary>
	/// Gets the build part of the file version number.
	/// </summary>
	public int FileVersionBuild => _fv.FileBuildPart;

	/// <summary>
	/// Gets the private part of the file version number.
	/// </summary>
	public int FileVersionPrivate => _fv.FilePrivatePart;

	/// <summary>
	/// Gets the major version component of the product version.
	/// </summary>
	public int ProductVersionMajor => _fv.ProductMajorPart;

	/// <summary>
	/// Gets the minor version component of the product version.
	/// </summary>
	public int ProductVersionMinor => _fv.ProductMinorPart;

	/// <summary>
	/// Gets the build number component of the product version.
	/// </summary>
	public int ProductVersionBuild => _fv.ProductBuildPart;

	/// <summary>
	/// Gets the product-specific private version component of the file.
	/// </summary>
	public int ProductVersionPrivate => _fv.ProductPrivatePart;

	/// <summary>
	/// Gets the size of the file in bytes.
	/// </summary>
	public long Size => _file.Length;

	/// <summary>
	/// Gets a value indicating whether the file exists in the specified path.
	/// </summary>
	public bool Exists => _file.Exists;

	/// <summary>
	/// Gets or sets a value indicating whether the file is read-only.
	/// </summary>
	public bool IsReadOnly
	{
		get
		{
			return _file.IsReadOnly;
		}
		set
		{
			_file.IsReadOnly = value;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the file is compressed.
	/// </summary>
	public bool IsCompressed => _file.Attributes.HasFlag(FileAttributes.Compressed);

	/// <summary>
	/// Gets a value indicating whether the file is encrypted.
	/// </summary>
	public bool IsEncrypted => _file.Attributes.HasFlag(FileAttributes.Encrypted);

	/// <summary>
	/// Gets a value indicating whether the file is marked as temporary.
	/// </summary>
	public bool IsTemporary => _file.Attributes.HasFlag(FileAttributes.Temporary);

	/// <summary>
	/// Gets a value indicating whether the file is marked as hidden.
	/// </summary>
	public bool IsHidden => _file.Attributes.HasFlag(FileAttributes.Hidden);

	/// <summary>
	/// Gets a value indicating whether the file is a system file.
	/// </summary>
	public bool IsSystemFile => _file.Attributes.HasFlag(FileAttributes.System);

	/// <summary>
	/// Gets a value indicating whether the file is a debug version.
	/// </summary>
	public bool IsDebug => _fv.IsDebug;

	/// <summary>
	/// Gets a value indicating whether the file has been patched.
	/// </summary>
	public bool IsPatched => _fv.IsPatched;

	/// <summary>
	/// Gets a value indicating whether the file is marked as a pre-release version.
	/// </summary>
	public bool IsPreRelease => _fv.IsPreRelease;

	/// <summary>
	/// Gets a value indicating whether the file is marked as a private build.
	/// </summary>
	public bool IsPrivateBuild => _fv.IsPrivateBuild;

	/// <summary>
	/// Gets a value indicating whether the file is marked as a special build.
	/// </summary>
	public bool IsSpecialBuild => _fv.IsSpecialBuild;

	/// <summary>
	/// Gets a value indicating whether the file is an assembly based on its extension.
	/// </summary>
	public bool IsAssembly => Extension.Contains("dll");

	/// <summary>
	/// Gets or sets the creation date and time of the file.
	/// </summary>
	public DateTime CreationTime
	{
		get
		{
			return _file.CreationTime;
		}
		set
		{
			File.SetCreationTime(Path, value);
		}
	}

	/// <summary>
	/// Gets or sets the date and time the file was last accessed.
	/// </summary>
	public DateTime LastAccessTime
	{
		get
		{
			return _file.LastAccessTime;
		}
		set
		{
			File.SetLastAccessTime(Path, value);
		}
	}

	/// <summary>
	/// Gets or sets the date and time the file was last written to.
	/// </summary>
	public DateTime LastWriteTime
	{
		get
		{
			return _file.LastWriteTime;
		}
		set
		{
			File.SetLastWriteTime(Path, value);
		}
	}

	/// <summary>
	/// Gets the associated <see cref="DirectoryManager"/> instance for managing directory operations
	/// related to the current file.
	/// </summary>
	public DirectoryManager Directory => _directory;

	/// <summary>
	/// Initializes a new instance of the FileManager class with the specified file path.
	/// </summary>
	public FileManager(string filePath)
	{
		_file = new FileInfo(filePath);
		if (!_file.Exists)
		{
			_file.Create().Close();
		}
		_fv = FileVersionInfo.GetVersionInfo(filePath);
		_directory = new DirectoryManager(_file.DirectoryName);
	}

	internal FileManager(FileInfo fileInfo)
	{
		_file = fileInfo;
		_directory = new DirectoryManager(fileInfo.DirectoryName);
		_fv = FileVersionInfo.GetVersionInfo(fileInfo.FullName);
	}

	/// <summary>
	/// Encrypts the current file represented by this instance using the underlying file system's encryption mechanism.
	/// If the file is already encrypted, no action is taken.
	/// </summary>
	/// <exception cref="System.IO.IOException">
	/// Thrown when an I/O error occurs during the encryption process.
	/// </exception>
	/// <exception cref="System.UnauthorizedAccessException">
	/// Thrown when the caller does not have the required permission to encrypt the file.
	/// </exception>
	/// <exception cref="System.NotSupportedException">
	/// Thrown when the file system does not support file encryption.
	/// </exception>
	public void EncryptFile()
	{
		_file.Encrypt();
	}

	/// <summary>
	/// Decrypts the current file represented by this instance using the underlying file system's decryption mechanism.
	/// If the file is not encrypted, no action is taken.
	/// </summary>
	/// <exception cref="System.IO.IOException">
	/// Thrown when an I/O error occurs during the decryption process.
	/// </exception>
	/// <exception cref="System.UnauthorizedAccessException">
	/// Thrown when the caller does not have the required permission to decrypt the file.
	/// </exception>
	/// <exception cref="System.NotSupportedException">
	/// Thrown when the file system does not support file decryption.
	/// </exception>
	public void DecryptFile()
	{
		_file.Decrypt();
	}

	/// <summary>
	/// Deletes the current file represented by this instance.
	/// If the file does not exist, no action is taken.
	/// </summary>
	/// <exception cref="System.IO.IOException">
	/// Thrown when the file is in use by another process or an I/O error occurs.
	/// </exception>
	/// <exception cref="System.UnauthorizedAccessException">
	/// Thrown when the caller does not have the required permission to delete the file.
	/// </exception>
	public void Delete()
	{
		_file.Delete();
	}

	/// <summary>
	/// Moves the current file to a new location specified by the target path.
	/// </summary>
	/// <param name="path">The target path where the file will be moved.</param>
	public void Move(string path)
	{
		_file.MoveTo(path);
	}

	/// <summary>
	/// Creates a copy of the current file at the specified destination path.
	/// </summary>
	/// <param name="destination">The destination path where the file will be copied.</param>
	/// <returns>A FileManager instance associated with the newly copied file.</returns>
	public FileManager Copy(string destination)
	{
		return new FileManager(_file.CopyTo(destination));
	}

	/// <summary>
	/// Opens the file associated with the current FileManager instance for writing purposes.
	/// </summary>
	/// <returns>A StreamWriter object that can be used to write to the file.</returns>
	public StreamWriter OpenWriter()
	{
		return new StreamWriter(OpenFile());
	}

	/// <summary>
	/// Opens the file associated with the current FileManager instance for reading purposes.
	/// </summary>
	/// <returns>A StreamReader object that can be used to read from the file.</returns>
	public StreamReader OpenReader()
	{
		return new StreamReader(OpenFile());
	}

	/// <summary>
	/// Opens the file associated with the current FileManager instance for reading and writing.
	/// If the file does not exist, it will be created.
	/// </summary>
	/// <returns>A FileStream object that can be used to read from and write to the file.</returns>
	public FileStream OpenFile()
	{
		return _file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
	}

	/// <summary>
	/// Creates a new file represented by the FileManager instance. If the file already exists, it can be optionally overwritten.
	/// </summary>
	/// <param name="overwrite">Specifies whether an existing file should be overwritten. If set to true, the existing file will be replaced.</param>
	public void Create(bool overwrite = false)
	{
		if (!Exists || overwrite)
		{
			File.Create(Path).Close();
		}
	}

	/// <summary>
	/// Writes the specified byte array to the file represented by the FileManager instance.
	/// </summary>
	/// <param name="bytes">The array of bytes to be written to the file.</param>
	public void WriteBytes(byte[] bytes)
	{
		File.WriteAllBytes(Path, bytes);
	}

	/// <summary>
	/// Writes a collection of lines of text to the file represented by the FileManager instance.
	/// </summary>
	/// <param name="lines">The collection of text lines to be written to the file.</param>
	public void WriteLines(IEnumerable<string> lines)
	{
		File.WriteAllLines(Path, lines);
	}

	/// <summary>
	/// Writes multiple lines of text to the file represented by the FileManager instance.
	/// </summary>
	/// <param name="lines">The collection of lines to be written to the file.</param>
	public void WriteLines(string[] lines)
	{
		File.WriteAllLines(Path, lines);
	}

	/// <summary>
	/// Writes a single line of text to the file represented by the FileManager instance, overwriting its contents if it already exists.
	/// </summary>
	/// <param name="line">The line of text to write to the file.</param>
	public void WriteLine(string line)
	{
		File.WriteAllLines(Path, new string[1] { line });
	}

	/// <summary>
	/// Writes the specified text to the file represented by the FileManager instance, overwriting its contents if it already exists.
	/// </summary>
	/// <param name="text">The text to write to the file.</param>
	public void WriteText(string text)
	{
		File.WriteAllText(Path, text);
	}

	/// <summary>
	/// Reads all bytes from the file represented by the FileManager instance.
	/// </summary>
	/// <returns>A byte array containing the contents of the file.</returns>
	public byte[] ReadBytes()
	{
		return File.ReadAllBytes(Path);
	}

	/// <summary>
	/// Reads all lines from the file represented by the FileManager instance.
	/// </summary>
	/// <returns>An enumerable collection of strings, each representing a line from the file.</returns>
	public string[] ReadLines()
	{
		return File.ReadAllLines(Path);
	}

	/// <summary>
	/// Reads the entire text content of the file represented by the FileManager instance.
	/// </summary>
	/// <returns>The text content of the file as a string.</returns>
	public string ReadText()
	{
		return File.ReadAllText(Path);
	}

	/// <summary>
	/// Determines whether the specified file name is valid based on certain conditions.
	/// </summary>
	/// <param name="fileName">The file name to validate.</param>
	/// <returns>True if the file name is valid; otherwise, false.</returns>
	public static bool IsValidFileName(string fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
			return false;
		
		if (ReservedFileNames.Contains(fileName))
			return false;
		
		if (PathIllegalCharacters.Any(x => fileName.Contains(x)))
			return false;
		
		return true;
	}

	/// <summary>
	/// Determines whether the specified path is valid based on a set of predefined checks.
	/// </summary>
	/// <param name="path">The path string to validate.</param>
	/// <returns>True if the path is valid; otherwise, false.</returns>
	public static bool IsValidPath(string path)
	{
		if (path == null)
			return false;

		if (string.IsNullOrWhiteSpace(path))
			return false;
		
		if (path.EndsWith("."))
			return false;
		
		return true;
	}

	/// <summary>
	/// Computes the MD5 hash of the specified file's content and returns it as a lowercase hexadecimal string.
	/// </summary>
	/// <param name="path">The full path to the file for which the hash is to be calculated.</param>
	/// <returns>A string representing the calculated MD5 hash in lowercase hexadecimal format.</returns>
	public static string CalculateHash(string path)
	{
		using var mD = MD5.Create();
		using var inputStream = File.OpenRead(path);
		
		var array = mD.ComputeHash(inputStream);
		return BitConverter.ToString(array).Replace("-", string.Empty).ToLower();
	}

	/// <summary>
	/// Determines whether a specified file is locked by another process or application.
	/// </summary>
	/// <param name="filePath">The full path to the file to check for a locked status.</param>
	/// <returns>True if the file is locked; otherwise, false.</returns>
	public static bool IsFileLocked(string filePath)
	{
		var fileInfo = new FileInfo(filePath);
		
		try
		{
			using (fileInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			{
				
			}
		}
		catch (IOException)
		{
			return true;
		}
		
		return false;
	}

	/// <summary>
	/// Writes the specified text to the given file, creating or overwriting the file as necessary.
	/// </summary>
	/// <param name="filePath">The path of the file to which the text will be written.</param>
	/// <param name="content">The text content to write to the file.</param>
	public static void WriteAllText(string filePath, string content)
	{
		using var stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
		using var streamWriter = new StreamWriter(stream);
		
		streamWriter.Write(content);
	}

	/// <summary>
	/// Writes a collection of lines to the specified file.
	/// </summary>
	/// <param name="filePath">The path of the file to which the lines will be written.</param>
	/// <param name="lines">The collection of strings representing the lines to write to the file.</param>
	public static void WriteAllLines(string filePath, IEnumerable<string> lines)
	{
		using var stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
		using var streamWriter = new StreamWriter(stream);
		
		lines.ForEach(streamWriter.WriteLine);
	}

	/// <summary>
	/// Writes a sequence of bytes to the specified file.
	/// </summary>
	/// <param name="filePath">The path of the file to which the bytes will be written.</param>
	/// <param name="bytes">The array of bytes to write to the file.</param>
	public static void WriteBytes(string filePath, byte[] bytes)
	{
		using var fileStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
		
		fileStream.Write(bytes, 0, bytes.Length);
	}

	/// <summary>
	/// Reads the entire content of a file as a string.
	/// </summary>
	/// <param name="filePath">The full path of the file to read.</param>
	/// <returns>The content of the file as a string.</returns>
	public static string ReadAllText(string filePath)
	{
		using var stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
		using var streamReader = new StreamReader(stream);
		
		return streamReader.ReadToEnd();
	}

	/// <summary>
	/// Reads all lines from the specified file.
	/// </summary>
	/// <param name="filePath">The full path of the file to read from.</param>
	/// <returns>An array of strings where each element represents a line from the file.</returns>
	public static string[] ReadAllLines(string filePath)
	{
		using var stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
		using var streamReader = new StreamReader(stream);
		
		return streamReader.ReadToEnd().SplitLines();
	}

	/// <summary>
	/// Reads all bytes from the specified file.
	/// </summary>
	/// <param name="filePath">The full path of the file to read from.</param>
	/// <returns>An array of bytes containing the file's contents.</returns>
	public static byte[] ReadBytes(string filePath)
	{
		using var fileStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
		using var memoryStream = new MemoryStream();
		
		fileStream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}
}