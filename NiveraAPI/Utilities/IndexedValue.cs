namespace NiveraAPI.Utilities;

/// <summary>
/// Represents a structure that encapsulates a specific value in a list alongside its
/// current index, providing access to the value itself, adjacent values, their indices,
/// and tools for navigating within the list.
/// </summary>
/// <typeparam name="T">The type of the elements in the underlying list.</typeparam>
public struct IndexedValue<T>
{
    /// <summary>
    /// Gets the list of values being managed by the indexed collection.
    /// </summary>
    /// <remarks>
    /// This property provides access to the underlying list of elements that
    /// are navigable using indices such as <c>CurrentIndex</c>, <c>NextIndex</c>,
    /// and <c>PreviousIndex</c>. The list is immutable through this property.
    /// </remarks>
    public List<T> Values { get; }

    /// <summary>
    /// Gets the index of the currently selected item within the collection.
    /// </summary>
    /// <remarks>
    /// This property represents the zero-based position of the active element in the collection.
    /// It is used to identify which item is currently being referenced among the available values.
    /// </remarks>
    public int CurrentIndex { get; }

    /// <summary>
    /// Gets the index of the next element in the collection relative to the current index.
    /// </summary>
    /// <remarks>
    /// This property returns the calculated index of the element that follows the current element
    /// in the collection. If the current element is the last in the collection, the value of this
    /// property may exceed the bounds of the collection, which can be checked using <c>IsNextOutOfRange</c>.
    /// </remarks>
    public int NextIndex { get; }

    /// <summary>
    /// Gets the index of the element immediately preceding the current index in the collection.
    /// </summary>
    /// <remarks>
    /// This property represents the index of the previous element relative to the current index,
    /// as defined by <c>CurrentIndex</c>. If the current index is the first element in the collection,
    /// this property will hold a value that is out of range (negative).
    /// </remarks>
    public int PreviousIndex { get; }
    
    /// <summary>
    /// Gets the number of remaining elements in the collection after the current index.
    /// </summary>
    public int RemainingCount { get; }
    
    /// <summary>
    /// Gets a value indicating whether the current index is out of range.
    /// </summary>
    public bool IsOutOfRange { get; }
    
    /// <summary>
    /// Gets a value indicating whether the next index is out of range.
    /// </summary>
    public bool IsNextOutOfRange { get; }
    
    /// <summary>
    /// Gets a value indicating whether the previous index is out of range.
    /// </summary>
    public bool IsPreviousOutOfRange { get; }

    /// <summary>
    /// Gets the next element in the collection.
    /// </summary>
    public T? Next { get; }

    /// <summary>
    /// Gets the currently selected element in the collection.
    /// </summary>
    public T Current { get; }

    /// <summary>
    /// Gets the previous element in the collection.
    /// </summary>
    public T? Previous { get; }

    /// <summary>
    /// Represents a value in a list alongside its current index and provides
    /// access to adjacent values and their indices with range validation.
    /// </summary>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    public IndexedValue(List<T> values, int currentIndex)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        NextIndex = currentIndex + 1;
        CurrentIndex = currentIndex;
        PreviousIndex = currentIndex - 1;
        
        IsOutOfRange = currentIndex < 0 || currentIndex >= values.Count;
        IsNextOutOfRange = NextIndex >= values.Count;
        IsPreviousOutOfRange = PreviousIndex < 0 || PreviousIndex >= values.Count;
        
        Values = values;
        
        Next = IsNextOutOfRange ? default : values[NextIndex];
        Current = IsOutOfRange ? default : values[currentIndex];
        Previous = IsPreviousOutOfRange ? default : values[PreviousIndex];
        
        RemainingCount = values.Count - currentIndex - 1;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IndexedValue{T}"/> representing the next element
    /// in the list, if it exists, by advancing the current index by one.
    /// </summary>
    /// <returns>A new instance of <see cref="IndexedValue{T}"/> with the updated index.</returns>
    public IndexedValue<T> MoveNext()
        => new(Values, NextIndex);

    /// <summary>
    /// Creates a new instance of <see cref="IndexedValue{T}"/> representing the previous element
    /// in the list, if it exists, by decrementing the current index by one.
    /// </summary>
    /// <returns>A new instance of <see cref="IndexedValue{T}"/> with the updated index.</returns>
    public IndexedValue<T> MovePrevious()
        => new(Values, PreviousIndex);

    /// <summary>
    /// Moves the current index forward by a specified count and creates a new instance
    /// of <see cref="IndexedValue{T}"/> representing the updated state.
    /// </summary>
    /// <param name="count">The number of positions to move forward in the list.</param>
    /// <returns>A new instance of <see cref="IndexedValue{T}"/> with the updated index.</returns>
    public IndexedValue<T> MoveForward(int count)
        => new(Values, CurrentIndex + count);

    /// <summary>
    /// Creates a new instance of <see cref="IndexedValue{T}"/> by moving the current index
    /// backwards by the specified number of positions, if valid.
    /// </summary>
    /// <param name="count">The number of positions to move backwards from the current index.</param>
    /// <returns>A new instance of <see cref="IndexedValue{T}"/> with the updated index.</returns>
    public IndexedValue<T> MoveBackwards(int count)
        => new(Values, CurrentIndex - count);

    /// <summary>
    /// Moves the current index by the specified relative count, forward or backward,
    /// and creates a new instance of <see cref="IndexedValue{T}"/> representing the updated state.
    /// </summary>
    /// <param name="count">
    /// The relative number of positions to move. A positive value moves forward,
    /// while a negative value moves backward.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="IndexedValue{T}"/> with the updated index.
    /// </returns>
    public IndexedValue<T> MoveRelative(int count)
        => count >= 0 ? MoveForward(count) : MoveBackwards(-count);

    /// <summary>
    /// Creates a new instance of <see cref="IndexedValue{T}"/> with the index reset to zero,
    /// representing the first element of the list.
    /// </summary>
    /// <returns>A new instance of <see cref="IndexedValue{T}"/> with the index set to zero.</returns>
    public IndexedValue<T> ZeroIndex()
        => new(Values, 0);

    /// <summary>
    /// Creates a new instance of <see cref="IndexedValue{T}"/> representing the last element
    /// in the list, if it exists, by setting the index to the last valid position in the list.
    /// </summary>
    /// <returns>A new instance of <see cref="IndexedValue{T}"/> with the index set to the last element position.</returns>
    public IndexedValue<T> LastIndex()
        => new(Values, Values.Count - 1);

    /// <summary>
    /// Adds a new value to the underlying list and returns an instance representing the next element,
    /// updating the current index to reflect the new value's position.
    /// </summary>
    /// <param name="value">The value to be added to the list.</param>
    /// <returns>An <see cref="IndexedValue{T}"/> representing the next position in the list,
    /// which accounts for the newly added value.</returns>
    public IndexedValue<T> AddAndMoveNext(T value)
    {
        Values.Add(value);
        return MoveNext();
    }

    /// <summary>
    /// Inserts a specified value at the given index in the list and returns a new
    /// <see cref="IndexedValue{T}"/> instance pointing to the newly inserted value.
    /// </summary>
    /// <param name="index">The index at which the value should be inserted.</param>
    /// <param name="value">The value to be inserted into the list.</param>
    /// <returns>A new <see cref="IndexedValue{T}"/> instance pointing to the inserted value.</returns>
    public IndexedValue<T> InsertAndMoveTo(int index, T value)
    {
        Values.Insert(index, value);
        return new(Values, index);
    }

    /// <summary>
    /// Removes the current element at the current index in the list and
    /// returns a new instance of <see cref="IndexedValue{T}"/> representing the previous element.
    /// </summary>
    /// <returns>A new instance of <see cref="IndexedValue{T}"/> positioned at the previous index after removal.</returns>
    public IndexedValue<T> RemoveCurrentAndMovePrevious()
    {
        Values.RemoveAt(CurrentIndex);
        return MovePrevious();
    }
}