using System.Collections.Concurrent;

namespace NiveraAPI.Pooling;

/// <summary>
/// Manages a pool of fixed-size arrays, providing functionality to rent and return arrays
/// for reuse, thus minimizing the need for frequent allocations and deallocations of arrays
/// in memory. This can reduce GC pressure in scenarios involving heavy use of arrays.
/// </summary>
/// <typeparam name="T">The type of elements in the arrays managed by the pool.</typeparam>
public class FixedArrayPool<T>
{
    private volatile T[] emptyArray = [];
    
    private volatile int arraySize;
    private volatile int initialSize;
    
    private volatile ConcurrentQueue<T[]> pool;
    
    /// <summary>
    /// Gets or sets the size of arrays managed by the pool.
    /// The size determines the length of arrays created and reused in the pool.
    /// When setting a new value, the size must be greater than or equal to 1.
    /// If the new size is applied, the existing pool is cleared and reinitialized
    /// to comply with the updated array size and the initial pool size configured.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if the value is less than 1.
    /// </summary>
    public int ArraySize
    {
        get => arraySize;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            if (arraySize == value)
                return;
            
            arraySize = value;
            
            while (pool.TryDequeue(out _))
                continue;

            if (value < 1)
                return;
            
            if (initialSize > 0)
            {
                for (var x = 0; x < initialSize; x++)
                {
                    pool.Enqueue(new T[arraySize]);
                }
            }
        }
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="FixedArrayPool{T}"/> with the specified array size and initial pool size.
    /// </summary>
    /// <param name="arraySize">The size of arrays managed by the pool. Must be greater than or equal to 1.</param>
    /// <param name="initialSize">The initial number of arrays to preallocate in the pool. Must be non-negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="arraySize"/> is less than 1 or <paramref name="initialSize"/> is negative.</exception>
    public FixedArrayPool(int arraySize, int initialSize = 0)
    {
        if (arraySize < 0)
            throw new ArgumentOutOfRangeException(nameof(arraySize));
        
        this.pool = new ConcurrentQueue<T[]>();
        
        this.arraySize = arraySize;
        this.initialSize = initialSize;

        if (arraySize < 1)
            return;

        if (initialSize > 0)
        {
            for (var x = 0; x < initialSize; x++)
            {
                pool.Enqueue(new T[arraySize]);
            }
        }
    }

    /// <summary>
    /// Retrieves an array from the pool if one is available; otherwise, creates and returns a new array of the configured size.
    /// </summary>
    /// <returns>A rented array of the specified size from the pool, or a newly created array if the pool is empty.</returns>
    public T[] Rent()
    {
        if (arraySize < 1)
            return emptyArray;
        
        if (pool.TryDequeue(out var array))
            return array;
        
        return new T[arraySize];
    }

    /// <summary>
    /// Returns an array to the pool for reuse, clearing its contents before doing so.
    /// </summary>
    /// <param name="array">The array to be returned to the pool. This array must match the configured size of the pool.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided array is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided array's size does not match the configured pool size.</exception>
    public void Return(T[] array)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        if (arraySize < 1)
            return;
        
        if (array.Length != arraySize)
            throw new ArgumentException("Array size does not match the pool size.");
        
        Array.Clear(array, 0, array.Length);
        
        pool.Enqueue(array);
    }
}