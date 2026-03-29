using System.Collections;

namespace NiveraAPI.Utilities;

/// <summary>
/// A lightweight, dynamically sized array with stable indices, fast slot reuse via free-list,
/// automatic resizing, and thread-safe concurrent reads + exclusive writes using ReaderWriterLockSlim.
/// </summary>
/// <typeparam name="T">The type of elements stored in the array.</typeparam>
public class SimpleArray<T> : IEnumerable<T>, IDisposable
{
    /// <summary>
    /// A delegate for enumerating through the elements of a <see cref="SimpleArray{T}"/>.
    /// Allows both reading and modifying elements by reference, as well as accessing their indices.
    /// The enumeration can be terminated early by returning false from the delegate.
    /// </summary>
    /// <typeparam name="T">The type of elements stored within the <see cref="SimpleArray{T}"/>.</typeparam>
    /// <param name="value">A reference to the current element in the array being enumerated.</param>
    /// <param name="index">The index of the current element in the array.</param>
    /// <returns>A boolean value indicating whether to continue enumeration. Return false to stop iteration.</returns>
    public delegate bool ForEnumerator(ref T value, int index);
    
    /// <summary>
    /// A delegate for enumerating through the elements of a <see cref="SimpleArray{T}"/>.
    /// </summary>
    public delegate bool ForEachEnumerator(ref T value);

    private volatile T[] array;
    private volatile BitArray occupancy;
    private readonly Queue<int> freeIndices = new();

    private volatile int count;
    private volatile int multiplier;
    private volatile int nextAppendIndex;

    private volatile bool canResize;

    private readonly ReaderWriterLockSlim rwLock = new(LockRecursionPolicy.NoRecursion);

    /// <summary>
    /// Gets the number of elements currently stored in the array.
    /// </summary>
    public int Count => count;

    /// <summary>
    /// Gets or sets the multiplier used to increase the size of the array when it is full.
    /// </summary>
    public int Multiplier
    {
        get => multiplier;
        set
        {
            rwLock.EnterWriteLock();
            try
            {
                multiplier = value;
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Gets or sets the capacity of the array.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when setting a negative capacity.</exception>
    public int Capacity
    {
        get
        {
            rwLock.EnterReadLock();
            
            try
            {
                return array.Length;
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            rwLock.EnterWriteLock();
            
            try
            {
                if (value == array.Length) 
                    return;

                if (value > array.Length)
                {
                    if (occupancy.Length < value)
                    {
                        var old = occupancy;
                        
                        occupancy = new BitArray(value);
                        occupancy.SetAll(false);
                        
                        for (int i = 0; i < old.Length; i++)
                            occupancy.Set(i, old.Get(i));
                    }

                    var temp = array;
                    
                    Array.Resize(ref temp, value);
                    
                    array = temp;
                }
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the array can automatically resize itself
    /// when an operation would exceed its current capacity.
    /// </summary>
    public bool CanResize
    {
        get => canResize;
        set
        {
            rwLock.EnterWriteLock();
            
            try
            {
                canResize = value;
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Gets a thread-safe snapshot of the underlying array.
    /// This returns a shallow copy of the current state of the array to ensure that
    /// modifications to the array do not affect the snapshot after it is retrieved.
    /// </summary>
    public T[] ArraySnapshot
    {
        get
        {
            rwLock.EnterReadLock();
            
            try
            {
                return (T[])array.Clone();
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="SimpleArray{T}"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the array. Must be non-negative.</param>
    /// <param name="canResize">Indicates whether the array can automatically resize itself when necessary.</param>
    /// <param name="multiplier">The factor by which the array capacity is increased when resizing is required.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is negative.</exception>
    public SimpleArray(int capacity = 100, bool canResize = true, int multiplier = 2)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        this.canResize = canResize;
        this.multiplier = multiplier;

        array = new T[capacity];
        
        occupancy = new BitArray(capacity);
        occupancy.SetAll(false);

        for (int i = 0; i < capacity; i++)
            freeIndices.Enqueue(i);

        nextAppendIndex = 0;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="SimpleArray{T}"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="collection">The collection of elements to initialize the array with.</param>
    /// <param name="canResize">Indicates whether the array can automatically resize itself when necessary.</param>
    /// <param name="multiplier">The factor by which the array capacity is increased when resizing is required.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="collection"/> is null.</exception>
    public SimpleArray(IEnumerable<T> collection, bool canResize = true, int multiplier = 2)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        this.canResize = canResize;
        this.multiplier = multiplier;

        array = collection.ToArray();
        
        occupancy = new BitArray(array.Length);
        occupancy.SetAll(false);

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != null)
            {
                count++;
                occupancy.Set(i, true);
            }
            else
            {
                freeIndices.Enqueue(i);
            }
        }
        
        nextAppendIndex = array.Length;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="SimpleArray{T}"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="sourceArray">The array to initialize the SimpleArray with.</param>
    /// <param name="canResize">Indicates whether the array can automatically resize itself when necessary.</param>
    /// <param name="multiplier">The factor by which the array capacity is increased when resizing is required.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sourceArray"/> is null.</exception>
    public SimpleArray(T[] sourceArray, bool canResize = true, int multiplier = 2)
    {
        if (sourceArray == null)
            throw new ArgumentNullException(nameof(sourceArray));

        this.canResize = canResize;
        this.multiplier = multiplier;

        array = (T[])sourceArray.Clone();
        
        occupancy = new BitArray(array.Length);
        occupancy.SetAll(false);

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] != null)
            {
                count++;
                occupancy.Set(i, true);
            }
            else
            {
                freeIndices.Enqueue(i);
            }
        }
        nextAppendIndex = array.Length;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="SimpleArray{T}"/> class with the same elements as another <see cref="SimpleArray{T}"/>.
    /// </summary>
    /// <param name="other">The other SimpleArray to copy elements from.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="other"/> is null.</exception>
    public SimpleArray(SimpleArray<T> other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        other.rwLock.EnterReadLock();
        
        try
        {
            array = (T[])other.array.Clone();
            occupancy = (BitArray)other.occupancy.Clone();
            freeIndices = new Queue<int>(other.freeIndices);
            count = other.count;
            multiplier = other.multiplier;
            nextAppendIndex = other.nextAppendIndex;
            canResize = other.canResize;
        }
        finally
        {
            other.rwLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Iterates through the elements of the array and executes the specified <see cref="ForEnumerator"/> delegate for each occupied index.
    /// </summary>
    /// <param name="enumerator">
    /// A delegate that defines the operation to perform on each occupied element in the array.
    /// The delegate receives the element by reference and its corresponding index.
    /// The iteration stops if the delegate returns <c>false</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="enumerator"/> is <c>null</c>.</exception>
    public void For(ForEnumerator enumerator)
    {
        if (enumerator == null)
            throw new ArgumentNullException(nameof(enumerator));

        rwLock.EnterReadLock();
        
        try
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (!occupancy.Get(i)) 
                    continue;
                
                if (!enumerator(ref array[i], i))
                    break;
            }
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Iterates through each occupied element in the array and executes the provided delegate.
    /// </summary>
    /// <param name="enumerator">A delegate that operates on each element. Takes the element by reference.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="enumerator"/> is null.</exception>
    public void ForEach(ForEachEnumerator enumerator)
    {
        if (enumerator == null)
            throw new ArgumentNullException(nameof(enumerator));

        rwLock.EnterReadLock();
        
        try
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (!occupancy.Get(i)) 
                    continue;
                
                enumerator(ref array[i]);
            }
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Searches for the specified value in the array and returns the zero-based index of the first occurrence.
    /// </summary>
    /// <param name="value">The value to locate in the array.</param>
    /// <returns>The zero-based index of the first occurrence of <paramref name="value"/> within the array, or -1 if the value is not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public int IndexOf(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        rwLock.EnterReadLock();
        
        try
        {
            return Array.IndexOf(array, value);
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Returns the zero-based index of the first occurrence of a value that matches the specified predicate in the <see cref="SimpleArray{T}"/>.
    /// </summary>
    /// <param name="predicate">The predicate used to evaluate each element in the array.</param>
    /// <returns>The zero-based index of the first element that satisfies the predicate; or -1 if no element matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> is null.</exception>
    public int IndexOf(Predicate<T> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        rwLock.EnterReadLock();
        
        try
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (occupancy.Get(i) && predicate(array[i]))
                {
                    return i;
                }
            }
            
            return -1;
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Removes the specified value from the <see cref="SimpleArray{T}"/> instance, if it exists.
    /// </summary>
    /// <param name="value">The value to be removed. Must not be null.</param>
    /// <returns>
    /// True if the value was successfully removed; otherwise, false if the value could not be found.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public bool Remove(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var index = IndexOf(value);

        if (index == -1) 
            return false;

        rwLock.EnterWriteLock();
        
        try
        {
            SetIndexStatus(index, false);
            return true;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes the element at the specified index in the <see cref="SimpleArray{T}"/> and marks the index as unoccupied.
    /// </summary>
    /// <param name="index">The index of the element to remove. Must be a valid, occupied index within the array.</param>
    /// <returns>
    /// <c>true</c> if the element was successfully removed and the index marked as unoccupied; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is outside the bounds of the array.</exception>
    public bool RemoveAt(int index)
    {
        rwLock.EnterWriteLock();
        
        try
        {
            SetIndexStatus(index, false);
            return true;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes all elements that match the conditions defined by the specified predicate and returns the number of elements removed.
    /// </summary>
    /// <param name="predicate">A <see cref="Predicate{T}"/> delegate that defines the conditions of the elements to remove.</param>
    /// <returns>The number of elements removed from the array.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> is <c>null</c>.</exception>
    public int RemoveWhere(Predicate<T> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        int removed = 0;
        
        rwLock.EnterWriteLock();
        
        try
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (!occupancy.Get(i))
                    continue;
                
                if (!predicate(array[i])) 
                    continue;

                SetIndexStatus(i, false);
                
                removed++;
            }
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
        
        return removed;
    }

    /// <summary>
    /// Adds an element to the <see cref="SimpleArray{T}"/> at the next available index.
    /// </summary>
    /// <param name="value">The element to add to the array. Cannot be null.</param>
    /// <returns>
    /// <c>true</c> if the element was successfully added; otherwise, <c>false</c> if no available index exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public bool Add(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        rwLock.EnterWriteLock();

        try
        {
            int index = NextFreeIndex();
            
            if (index == -1) 
                return false;

            SetIndexStatus(index, true, value);
            return true;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Adds multiple values to the <see cref="SimpleArray{T}"/> instance, resizing the internal array if necessary.
    /// </summary>
    /// <param name="values">The collection of values to be added. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the array cannot be resized but additional capacity is required to add the specified values.
    /// </exception>
    public void AddMany(ICollection<T> values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        
        if (values.Count == 0)
            return;

        rwLock.EnterWriteLock();
        
        try
        {
            int needed = count + values.Count;
            
            if (array.Length < needed)
            {
                if (!canResize)
                    throw new InvalidOperationException("Cannot resize the array.");

                int newSize = Math.Max(needed, array.Length * multiplier);
                
                var temp = array;
                
                Array.Resize(ref temp, newSize);
                
                array = temp;

                var oldOcc = occupancy;
                
                occupancy = new BitArray(newSize);
                occupancy.SetAll(false);
                
                for (int i = 0; i < oldOcc.Length; i++)
                    occupancy.Set(i, oldOcc.Get(i));
            }

            foreach (var v in values)
            {
                int index = NextFreeIndex();
                
                if (index == -1) 
                    return;
                
                SetIndexStatus(index, true, v);
            }
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Determines whether the <see cref="SimpleArray{T}"/> contains the specified item.
    /// </summary>
    /// <param name="item">The item to locate in the array.</param>
    /// <returns>
    /// <c>true</c> if the item is found in the array; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="item"/> is <c>null</c>.
    /// </exception>
    public bool Contains(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        rwLock.EnterReadLock();

        try
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (occupancy.Get(i) && Equals(item, array[i]))
                {
                    return true;
                }
            }
            
            return false;
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Removes all items from the <see cref="SimpleArray{T}"/> instance, resetting its state.
    /// </summary>
    /// <remarks>
    /// This method clears the internal array, marks all indices as unoccupied, and resets counts and indices
    /// to their default values. Thread-safety is ensured through the use of a write lock.
    /// </remarks>
    public void Clear()
    {
        rwLock.EnterWriteLock();
        
        try
        {
            occupancy.SetAll(false);
            
            Array.Clear(array, 0, array.Length);
            
            freeIndices.Clear();
            
            count = 0;
            nextAppendIndex = 0;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Determines whether any element of the <see cref="SimpleArray{T}"/> satisfies the specified predicate.
    /// </summary>
    /// <param name="predicate">A delegate that defines a set of criteria and determines whether the specified element meets those criteria.</param>
    /// <returns>
    /// true if any elements match the conditions defined by the <paramref name="predicate"/>; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="predicate"/> is null.</exception>
    public bool Any(Predicate<T> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        rwLock.EnterReadLock();
        
        try
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (occupancy.Get(i) && predicate(array[i]))
                {
                    return true;
                }
            }
            
            return false;
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Determines whether the specified index in the array is occupied and retrieves the value at that index, if any.
    /// </summary>
    /// <param name="index">The index to check. Must be non-negative.</param>
    /// <param name="value">
    /// When this method returns, contains the value at the specified index if it is occupied; otherwise, the default value of type <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified index is occupied and contains a value; otherwise, <c>false</c>.
    /// </returns>
    public bool IsIndexOccupied(int index, out T? value)
    {
        value = default;

        if (index < 0)
            return false;

        rwLock.EnterReadLock();
        
        try
        {
            if (index >= array.Length || !occupancy.Get(index))
                return false;

            value = array[index];
            return value != null;
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }
    
    private void SetIndexStatus(int index, bool status, T? value = default)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        bool wasOccupied = occupancy.Get(index);

        if (!status && wasOccupied)
        {
            Interlocked.Decrement(ref count);
            
            array[index] = default;
            
            occupancy.Set(index, false);
            
            freeIndices.Enqueue(index);
        }
        else if (status && !wasOccupied)
        {
            Interlocked.Increment(ref count);
            
            array[index] = value;
            
            occupancy.Set(index, true);
        }
    }

    private int NextFreeIndex()
    {
        if (freeIndices.Count > 0)
            return freeIndices.Dequeue();

        int index = nextAppendIndex;

        if (index >= array.Length)
        {
            if (!canResize)
                return -1;

            int newSize = Math.Max(array.Length * multiplier, index + 1);
            
            var temp = array;
            
            Array.Resize(ref temp, newSize);
            
            array = temp;

            var old = occupancy;
            
            occupancy = new BitArray(newSize);
            occupancy.SetAll(false);
            
            for (int i = 0; i < old.Length; i++)
                occupancy.Set(i, old.Get(i));
        }

        nextAppendIndex++;
        return index;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the elements of the <see cref="SimpleArray{T}"/>.
    /// Only occupied indices will be included in the enumeration.
    /// </summary>
    /// <returns>An enumerator for iterating through the elements of the array.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        rwLock.EnterReadLock();
        
        try
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (occupancy.Get(i))
                {
                    yield return array[i];
                }
            }
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the elements in the <see cref="SimpleArray{T}"/>.
    /// </summary>
    /// <returns>An enumerator for the elements in the <see cref="SimpleArray{T}"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the <see cref="SimpleArray{T}"/> has been disposed.</exception>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="SimpleArray{T}"/> class.
    /// </summary>
    public void Dispose()
    {
        rwLock?.Dispose();
    }
}