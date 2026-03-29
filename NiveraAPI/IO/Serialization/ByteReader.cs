using System.Net;
using System.Text;

using NiveraAPI.Pooling;
using NiveraAPI.Extensions;

namespace NiveraAPI.IO.Serialization
{
    /// <summary>
    /// Used to read data.
    /// </summary>
    public class ByteReader : PoolResettable
    {
        private ArraySegment<byte> buffer;

        /// <summary>
        /// Gets or sets the reader encoding.
        /// </summary>
        public UTF8Encoding Encoding { get; set; } = new(false, true);

        /// <summary>
        /// Gets or sets the current count.
        /// </summary>
        public int Count => buffer.Count;

        /// <summary>
        /// Gets or sets the offset within the buffer.
        /// </summary>
        public int Offset => buffer.Offset;

        /// <summary>
        /// Gets or sets the position of the reader.
        /// </summary>
        public int Position { get; private set; } = 0;

        /// <summary>
        /// Gets the remaining amount of bytes.
        /// </summary>
        public int Remaining => Count - (Position + Offset);

        /// <summary>
        /// Whether or not the reader is at the end of the file.
        /// </summary>
        public bool IsEof => Position + Offset + 1 >= Count;

        /// <summary>
        /// Whether or not the reader is at the start of the file.
        /// </summary>
        public bool IsSof => Position == 0;

        /// <summary>
        /// Gets the byte at the current position.
        /// </summary>
        public byte Current => buffer.At(Position);

        /// <summary>
        /// Reads a blittable type.
        /// </summary>
        /// <typeparam name="T">The type to read.</typeparam>
        /// <returns>The read value.</returns>
        public unsafe T ReadBlittable<T>() where T : struct
        {
            var size = sizeof(T);
            var result = default(T);

            ThrowIfEnd(size);

            fixed (byte* ptr = &buffer.Array[Offset + Position])
                result = *(T*)ptr;

            Position += size;
            return result;
        }

        /// <summary>
        /// Reads a nullable blittable type.
        /// </summary>
        /// <typeparam name="T">The type to read.</typeparam>
        /// <returns>The read value.</returns>
        public T? ReadBlittableNullable<T>() where T : struct
        {
            var check = ReadByte();

            if (check != 0)
                return ReadBlittable<T>();

            return null;
        }
        
        /// <summary>
        /// Reads a <see cref="byte"/>.
        /// </summary>
        /// <returns>The read <see cref="byte"/>.</returns>
        public byte ReadByte()
            => ReadBlittable<byte>();

        /// <summary>
        /// Reads an array of <see cref="byte"/>.
        /// </summary>
        /// <returns>The read array.</returns>
        public byte[] ReadBytes()
            => ReadArray<byte>();

        /// <summary>
        /// Reads a <see cref="sbyte"/>.
        /// </summary>
        /// <returns>The read <see cref="sbyte"/>.</returns>
        public sbyte ReadSByte()
            => ReadBlittable<sbyte>();

        /// <summary>
        /// Reads a <see cref="short"/>.
        /// </summary>
        /// <returns>The read <see cref="short"/>.</returns>
        public short ReadInt16()
            => ReadBlittable<short>();

        /// <summary>
        /// Reads a <see cref="int"/>.
        /// </summary>
        /// <returns>The read <see cref="int"/>.</returns>
        public int ReadInt32()
            => ReadBlittable<int>();

        /// <summary>
        /// Reads a <see cref="long"/>.
        /// </summary>
        /// <returns>The read <see cref="long"/>.</returns>
        public long ReadInt64()
            => ReadBlittable<long>();

        /// <summary>
        /// Reads a <see cref="ushort"/>.
        /// </summary>
        /// <returns>The read <see cref="ushort"/>.</returns>
        public ushort ReadUInt16()
            => ReadBlittable<ushort>();

        /// <summary>
        /// Reads a <see cref="uint"/>.
        /// </summary>
        /// <returns>The read <see cref="uint"/>.</returns>
        public uint ReadUInt32()
            => ReadBlittable<uint>();

        /// <summary>
        /// Reads a <see cref="ulong"/>.
        /// </summary>
        /// <returns>The read <see cref="ulong"/>.</returns>
        public ulong ReadUInt64()
            => ReadBlittable<ulong>();

        /// <summary>
        /// Reads a <see cref="float"/>.
        /// </summary>
        /// <returns>The read <see cref="float"/>.</returns>
        public float ReadFloat()
            => ReadBlittable<float>();

        /// <summary>
        /// Reads a <see cref="double"/>.
        /// </summary>
        /// <returns>The read <see cref="double"/>.</returns>
        public double ReadDouble()
            => ReadBlittable<double>();

        /// <summary>
        /// Reads a <see cref="decimal"/>.
        /// </summary>
        /// <returns>The read <see cref="decimal"/>.</returns>
        public decimal ReadDecimal()
            => ReadBlittable<decimal>();

        /// <summary>
        /// Reads a boolean value.
        /// </summary>
        /// <returns>The boolean value read from the stream.</returns>
        public bool ReadBool()
            => ReadBlittable<bool>();

        /// <summary>
        /// Reads a <see cref="string"/>.
        /// </summary>
        /// <returns>The read <see cref="string"/></returns>
        public string ReadString()
        {
            var sig = ReadByte();

            if (sig != 1)
                return string.Empty;

            var bytes = ReadBytes();
            return Encoding.GetString(bytes);
        }

        /// <summary>
        /// Reads an IP address.
        /// </summary>
        /// <returns>The read IP address.</returns>
        public IPAddress ReadIpAddress()
            => new(ReadInt64());

        /// <summary>
        /// Reads an internet endpoint.
        /// </summary>
        /// <returns>The read endpoint.</returns>
        public IPEndPoint ReadIpEndPoint()
            => new(ReadIpAddress(), ReadUInt16());

        /// <summary>
        /// Reads a date.
        /// </summary>
        /// <returns>The read date.</returns>
        public DateTime ReadDate()
            => new(ReadInt64());

        /// <summary>
        /// Reads a date offset.
        /// </summary>
        /// <returns>The read date offset.</returns>
        public DateTimeOffset ReadDateOffset()
            => new(ReadInt64(), TimeSpan.FromTicks(ReadInt64()));

        /// <summary>
        /// Reads a time span.
        /// </summary>
        /// <returns>The read time span.</returns>
        public TimeSpan ReadTime()
            => new(ReadInt64());

        /// <summary>
        /// Reads a byte segment.
        /// </summary>
        /// <returns>The read segment.</returns>
        public ArraySegment<byte> ReadSegment()
        {
            var length = ReadInt32();
            var segment = buffer.ToSegment(Position, length);
            
            Position += length;
            return segment;
        }

        /// <summary>
        /// Reads a generic object using the <see cref="ByteSerializer{T}.Deserialize"/> delegate.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <returns>The read object (or <see langword="null"/> if a null object was provided to the writer).</returns>
        /// <exception cref="Exception"></exception>
        public T? Read<T>()
        {
            if (ByteSerializer<T>.Deserialize is null)
                throw new($"Deserialize delegate was not set for type '{typeof(T).FullName}'");

            var nullByte = ReadByte();

            if (nullByte == 0)
                return default;

            return ByteSerializer<T>.Deserialize(this);
        }

        /// <summary>
        /// Reads an array of generic objects.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <returns>The read array.</returns>
        /// <exception cref="Exception"></exception>
        public T[] ReadArray<T>()
        {
            if (ByteSerializer<T>.Deserialize is null)
                throw new($"Deserialize delegate was not set for type '{typeof(T).FullName}'");

            var size = ReadInt32();
            var array = new T[size];

            for (var i = 0; i < size; i++)
                array[i] = ByteSerializer<T>.Deserialize(this);

            return array;
        }

        /// <summary>
        /// Reads a list of generic objects.
        /// </summary>
        /// <typeparam name="T">The type of the list element.</typeparam>
        /// <returns>The read list.</returns>
        /// <exception cref="Exception"></exception>
        public List<T> ReadList<T>()
        {
            if (ByteSerializer<T>.Deserialize is null)
                throw new($"Deserialize delegate was not set for type '{typeof(T).FullName}'");

            var size = ReadInt32();
            var list = new List<T>(size);

            for (var i = 0; i < size; i++)
                list.Add(ByteSerializer<T>.Deserialize(this));

            return list;
        }

        /// <summary>
        /// Reads a dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <returns>The read dictionary.</returns>
        /// <exception cref="Exception"></exception>
        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
        {
            if (ByteSerializer<TKey>.Deserialize is null)
                throw new($"Deserialize delegate was not set for type '{typeof(TKey).FullName}'");

            if (ByteSerializer<TValue>.Deserialize is null)
                throw new($"Deserialize delegate was not set for type '{typeof(TValue).FullName}'");

            var size = ReadInt32();
            var dict = new Dictionary<TKey, TValue>(size);

            for (var i = 0; i < size; i++)
                dict[ByteSerializer<TKey>.Deserialize(this)] = ByteSerializer<TValue>.Deserialize(this);

            return dict;
        }

        /// <summary>
        /// Reads an array of generic objects.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="array">The destination array.</param>
        /// <param name="offset">The zero-based offset index.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="Exception"></exception>
        public void ReadArray<T>(T[] array, int offset = 0)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (ByteSerializer<T>.Deserialize is null)
                throw new($"Deserialize delegate was not set for type '{typeof(T).FullName}'");

            var size = ReadInt32();

            if ((offset + size) > array.Length)
                throw new($"The provided array is too small! ({offset + size} / {array.Length})");

            for (var i = 0; i < size; i++)
                array[offset + i] = ByteSerializer<T>.Deserialize(this);
        }

        /// <summary>
        /// Reads a list of generic objects.
        /// </summary>
        /// <typeparam name="T">The type of the list element.</typeparam>
        /// <param name="list">The destination list.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="Exception"></exception>
        public void ReadList<T>(IList<T> list)
        {
            if (list is null)
                throw new ArgumentNullException(nameof(list));

            if (ByteSerializer<T>.Deserialize is null)
                throw new($"Deserialize delegate was not set for type '{typeof(T).FullName}'");

            var size = ReadInt32();

            for (var i = 0; i < size; i++)
                list.Add(ByteSerializer<T>.Deserialize(this));
        }

        /// <summary>
        /// Reads a dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <returns>The read dictionary.</returns>
        /// <exception cref="Exception"></exception>
        public void ReadDictionary<TKey, TValue>(IDictionary<TKey, TValue> dict)
        {
            if (dict is null)
                throw new ArgumentNullException(nameof(dict));

            if (ByteSerializer<TKey>.Deserialize is null)
                throw new($"Deserialize delegate was not set for type '{typeof(TKey).FullName}'");

            if (ByteSerializer<TValue>.Deserialize is null)
                throw new($"Deserialize delegate was not set for type '{typeof(TValue).FullName}'");

            var size = ReadInt32();

            for (var i = 0; i < size; i++)
                dict[ByteSerializer<TKey>.Deserialize(this)] = ByteSerializer<TValue>.Deserialize(this);
        }

        /// <summary>
        /// Clears the internal buffer and resets the position to its initial state.
        /// </summary>
        public void Clear()
        {
            buffer = default;
            
            Position = 0;
        }

        /// <summary>
        /// Resets the reader by setting the position to the beginning of the buffer.
        /// </summary>
        public void Reset()
            => Position = 0;

        /// <summary>
        /// Resets the state of the reader with the provided buffer, offset, and count.
        /// </summary>
        /// <param name="buffer">The byte array buffer to read from.</param>
        /// <param name="offset">The position in the buffer to start reading from.</param>
        /// <param name="count">The total number of bytes to read.</param>
        public void Reset(byte[] buffer, int offset, int count)
        {
            this.buffer = new(buffer, offset, count);
            
            Position = 0;
        }

        /// <summary>
        /// Resets the reader's position to the beginning of the current buffer.
        /// </summary>
        public void Reset(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Count < 1)
                throw new ArgumentException("Buffer must be at least 1 byte long", nameof(buffer));
            
            this.buffer = buffer;

            Position = 0;
        }
        
        /// <summary>
        /// Places the object back into the pool for reuse by resetting its state and performing any necessary cleanup.
        /// This method must be implemented by derived classes to define specific reset behavior.
        /// </summary>
        /// <remarks>
        /// This method is intended to be called when the object is no longer in use and should be returned to a reusable state.
        /// Implementations should ensure that the object is properly prepared for its next usage and does not retain any stale references or data.
        /// </remarks>
        public override void ReturnToPool()
        {
            buffer = default;

            Position = 0;
        }

        private void ThrowIfEnd(int requiredBytes)
        {
            if (Position + requiredBytes > (Count - Offset))
                throw new EndOfStreamException(
                    $"Attempted to read more bytes than available! (Required={requiredBytes}; Available={Count - Offset}; " +
                    $"Position={Position}; Remaining={Remaining})");
        }

        /// <summary>
        /// Reads data from the specified buffer using the provided action on a <see cref="ByteReader"/> instance.
        /// </summary>
        /// <param name="buffer">The byte array containing the data to read.</param>
        /// <param name="offset">The position in the buffer where reading begins.</param>
        /// <param name="count">The number of bytes to read from the buffer.</param>
        /// <param name="action">The action to execute with the initialized <see cref="ByteReader"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="action"/> is null.</exception>
        /// <exception cref="Exception">Throws any exception encountered while executing the provided action.</exception>
        public static void Read(byte[] buffer, int offset, int count, Action<ByteReader> action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            var reader = Get(buffer, offset, count);
            var thrown = default(Exception);

            try
            {
                action(reader);
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            PoolBase<ByteReader>.Shared.Return(reader);

            if (thrown != null)
                throw thrown;
        }

        /// <summary>
        /// Reads data from the provided buffer using the specified action.
        /// </summary>
        /// <param name="buffer">The buffer containing the data to be read.</param>
        /// <param name="action">The action to perform with the <see cref="ByteReader"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="action"/> parameter is null.</exception>
        /// <exception cref="Exception">Thrown if the <paramref name="action"/> throws an exception during execution.</exception>
        public static void Read(ArraySegment<byte> buffer, Action<ByteReader> action)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));

            var reader = Get(buffer);
            var thrown = default(Exception);

            try
            {
                action(reader);
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            PoolBase<ByteReader>.Shared.Return(reader);

            if (thrown != null)
                throw thrown;
        }

        /// <summary>
        /// Retrieves a pooled instance of the <see cref="ByteReader"/> and initializes it with the specified buffer, offset, and count.
        /// </summary>
        /// <param name="buffer">The buffer containing the data to be read.</param>
        /// <param name="offset">The starting position within the buffer.</param>
        /// <param name="count">The number of bytes available in the buffer from the offset.</param>
        /// <returns>An initialized <see cref="ByteReader"/> instance.</returns>
        public static ByteReader Get(byte[] buffer, int offset, int count)
        {
            var reader = PoolBase<ByteReader>.Shared.Rent();

            reader.Reset(buffer, offset, count);
            return reader;
        }
        
        /// <summary>
        /// Retrieves a pooled instance of the <see cref="ByteReader"/> and initializes it with the specified buffer, offset, and count.
        /// </summary>
        /// <param name="buffer">The buffer containing the data to be read.</param>
        /// <returns>An initialized <see cref="ByteReader"/> instance.</returns>
        public static ByteReader Get(ArraySegment<byte> buffer)
        {
            var reader = PoolBase<ByteReader>.Shared.Rent();

            reader.Reset(buffer);
            return reader;
        }

        internal static void InitReaders()
        {
            ByteSerializer<byte>.Deserialize = reader => reader.ReadByte();
            ByteSerializer<sbyte>.Deserialize = reader => reader.ReadSByte();

            ByteSerializer<short>.Deserialize = reader => reader.ReadInt16();
            ByteSerializer<int>.Deserialize = reader => reader.ReadInt32();
            ByteSerializer<long>.Deserialize = reader => reader.ReadInt64();

            ByteSerializer<ushort>.Deserialize = reader => reader.ReadUInt16();
            ByteSerializer<uint>.Deserialize = reader => reader.ReadUInt32();
            ByteSerializer<ulong>.Deserialize = reader => reader.ReadUInt64();

            ByteSerializer<float>.Deserialize = reader => reader.ReadFloat();
            ByteSerializer<double>.Deserialize = reader => reader.ReadDouble();
            ByteSerializer<decimal>.Deserialize = reader => reader.ReadDecimal();

            ByteSerializer<byte[]>.Deserialize = reader => reader.ReadBytes();
            ByteSerializer<string>.Deserialize = reader => reader.ReadString();
            ByteSerializer<bool>.Deserialize = reader => reader.ReadBool();

            ByteSerializer<IPAddress>.Deserialize = reader => reader.ReadIpAddress();
            ByteSerializer<IPEndPoint>.Deserialize = reader => reader.ReadIpEndPoint();

            ByteSerializer<DateTime>.Deserialize = reader => reader.ReadDate();
            ByteSerializer<DateTimeOffset>.Deserialize = reader => reader.ReadDateOffset();

            ByteSerializer<TimeSpan>.Deserialize = reader => reader.ReadTime();
            ByteSerializer<ArraySegment<byte>>.Deserialize = reader => reader.ReadSegment();
        }
    }
}