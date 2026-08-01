namespace NiveraAPI.IO.Storage;

/// <summary>
/// Extension methods for the <see cref="StorageManager"/> class.
/// </summary>
public static class StorageExtensions
{
    /// <summary>
    /// Attempts to retrieve a value of the specified type from a directory within the storage manager.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="manager">The storage manager that contains the directories and values.</param>
    /// <param name="dirName">The name of the directory from which the value is to be retrieved.</param>
    /// <param name="valueName">The name of the value to retrieve from the directory.</param>
    /// <param name="value">When this method returns, contains the value of type <typeparamref name="T"/> if found; otherwise, the default value.</param>
    /// <returns>
    /// True if the value was successfully retrieved from the specified directory; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="manager"/>, <paramref name="dirName"/>, or <paramref name="valueName"/> parameter is null or empty.
    /// </exception>
    public static bool TryGetValue<T>(this StorageManager manager, string dirName, string valueName, out T value)
    {
        if (manager == null)
            throw new ArgumentNullException(nameof(manager));

        if (string.IsNullOrEmpty(dirName))
            throw new ArgumentNullException(nameof(dirName));

        if (string.IsNullOrEmpty(valueName))
            throw new ArgumentNullException(nameof(valueName));       

        value = default!;

        if (!manager.TryGet(dirName, out var dir))
            return false;

        if (!dir.TryGetValue(valueName, out value))
            return false;

        return true;
    }
    
    /// <summary>
    /// Attempts to retrieve a storage value from the specified directory within the storage manager.
    /// </summary>
    /// <typeparam name="T">The type of the storage value to retrieve.</typeparam>
    /// <param name="manager">The storage manager that manages directories and storage values.</param>
    /// <param name="dirName">The name of the directory from which to retrieve the storage value.</param>
    /// <param name="valueName">The name of the storage value to retrieve.</param>
    /// <param name="value">When this method returns, contains the storage value of type <typeparamref name="T"/> if found; otherwise, the default value.</param>
    /// <returns>
    /// True if the storage value was found successfully in the specified directory; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="manager"/>, <paramref name="dirName"/>, or <paramref name="valueName"/> parameter is null or empty.
    /// </exception>
    public static bool TryGetStorageValue<T>(this StorageManager manager, string dirName, string valueName,
        out StorageValue<T> value)
    {
        if (manager == null)
            throw new ArgumentNullException(nameof(manager));

        if (string.IsNullOrEmpty(dirName))
            throw new ArgumentNullException(nameof(dirName));
        
        if (string.IsNullOrEmpty(valueName))
            throw new ArgumentNullException(nameof(valueName));       

        value = default!;

        if (!manager.TryGet(dirName, out var dir))
            return false;

        if (!dir.TryGetStorageValue(valueName, out value))
            return false;

        return true;
    }
}