using System.Net;
using System.Text;

using NiveraAPI.Extensions;
using NiveraAPI.Pooling;

namespace NiveraAPI.IO.Serialization
{
    /// <summary>
    /// Used to write raw data.
    /// </summary>
    public class ByteWriter : PoolResettable
    {
        private byte[] buffer = new byte[IOSettings.BYTE_WRITER_BUFFER_INIT_SIZE];

        /// <summary>
        /// Gets or sets the UTF-8 encoding.
        /// </summary>
        public UTF8Encoding Encoding { get; set; } = new(false, true);

        /// <summary>
        /// Gets or sets the writer's buffer.
        /// </summary>
        public byte[] Buffer
        {
            get => buffer;
            set => buffer = value;
        }

        /// <summary>
        /// Gets or sets the position of the writer.
        /// </summary>
        public int Position { get; private set; } = 0;

        /// <summary>
        /// Writes the value of a blittable type.
        /// </summary>
        /// <typeparam name="T">The blittable type.</typeparam>
        /// <param name="value">The value to write.</param>
        public unsafe void WriteBlittable<T>(T value) where T : struct
        {
            var size = sizeof(T);

            AdjustSize(size);

            fixed (byte* ptr = &buffer[Position])
                *(T*)ptr = value;

            Position += size;
        }

        /// <summary>
        /// Writes the value of a nullable blittable type.
        /// </summary>
        /// <typeparam name="T">The blittable type.</typeparam>
        /// <param name="value">The value to write.</param>
        public void WriteBlittableNullable<T>(T? value) where T : struct
        {
            WriteByte((byte)(value.HasValue ? 1 : 0));

            if (value.HasValue)
                WriteBlittable(value.Value);
        }

        /// <summary>
        /// Writes a <see cref="byte"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="byte"/> to write.</param>
        public void WriteByte(byte value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a <see cref="sbyte"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="sbyte"/> to write.</param>
        public void WriteSByte(sbyte value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a <see cref="short"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="short"/> to write.</param>
        public void WriteInt16(short value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a <see cref="int"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="int"/> to write.</param>
        public void WriteInt32(int value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a <see cref="long"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="long"/> to write.</param>
        public void WriteInt64(long value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a <see cref="ushort"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="ushort"/> to write.</param>
        public void WriteUInt16(ushort value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a <see cref="uint"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="uint"/> to write.</param>
        public void WriteUInt32(uint value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a <see cref="ulong"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="ulong"/> to write.</param>
        public void WriteUInt64(ulong value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a <see cref="float"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="float"/> to write.</param>
        public void WriteFloat(float value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a <see cref="double"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="double"/> to write.</param>
        public void WriteDouble(double value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a <see cref="decimal"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="decimal"/> to write.</param>
        public void WriteDecimal(decimal value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a boolean value.
        /// </summary>
        /// <param name="value">The boolean value to write.</param>
        public void WriteBool(bool value)
            => WriteBlittable(value);

        /// <summary>
        /// Writes a byte array to the buffer.
        /// </summary>
        /// <param name="array">The array to write.</param>
        public void WriteBytes(byte[] array)
            => WriteBytes(array, 0, array.Length);

        /// <summary>
        /// Writes a byte array to the buffer.
        /// </summary>
        /// <param name="array">The array to write.</param>
        /// <param name="offset">The offset of the array.</param>
        public void WriteBytes(byte[] array, int offset)
            => WriteBytes(array, offset, array.Length - offset);

        /// <summary>
        /// Writes a byte array to the buffer.
        /// </summary>
        /// <param name="array">The array to write.</param>
        /// <param name="offset">The offset of the array.</param>
        /// <param name="count">The amount of bytes to write.</param>
        public void WriteBytes(byte[] array, int offset, int count)
        {
            AdjustSize(count + 4);
            WriteInt32(count);

            Array.ConstrainedCopy(array, offset, buffer, Position, count);

            Position += count;
        }

        /// <summary>
        /// Writes a <see cref="string"/> to the buffer.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to write.</param>
        public void WriteString(string value)
        {
            if (value is null)
            {
                WriteByte(0);
                return;
            }

            var bytes = Encoding.GetBytes(value);
            
            WriteByte(1);
            WriteBytes(bytes);
        }

        /// <summary>
        /// Writes an IP address.
        /// </summary>
        /// <param name="address">The address to write.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void WriteIpAddress(IPAddress address)
        {
            if (address is null)
                throw new ArgumentNullException(nameof(address));

            WriteBytes(address.GetAddressBytes());
        }

        /// <summary>
        /// Writes an internet endpoint.
        /// </summary>
        /// <param name="endPoint">The endpoint to write.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void WriteIpEndPoint(IPEndPoint endPoint)
        {
            if (endPoint is null)
                throw new ArgumentNullException(nameof(endPoint));

            WriteIpAddress(endPoint.Address);
            WriteUInt16((ushort)endPoint.Port);
        }

        /// <summary>
        /// Writes a date.
        /// </summary>
        /// <param name="date">The date to write.</param>
        public void WriteDate(DateTime date)
        { 
            WriteInt64(date.Ticks);
        }

        /// <summary>
        /// Writes a date offset.
        /// </summary>
        /// <param name="offset">The offset to write.</param>
        public void WriteDateOffset(DateTimeOffset offset)
        {
            WriteInt64(offset.DateTime.Ticks);
            WriteInt64(offset.Offset.Ticks);
        }

        /// <summary>
        /// Writes a time span.
        /// </summary>
        /// <param name="time">The time span to write.</param>
        public void WriteTime(TimeSpan time)
        {
            WriteInt64(time.Ticks);
        }

        /// <summary>
        /// Writes the specified object to the writer using the <see cref="ByteSerializer{T}.Serialize"/> delegate.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="value">The object instance.</param>
        /// <exception cref="Exception"></exception>
        public void Write<T>(T value)
        {
            if (ByteSerializer<T>.Serialize is null)
                throw new($"Serialization delegate was not set for type '{typeof(T).FullName}'");

            WriteByte((byte)(value is null ? 0 : 1));

            if (value is null)
                return;

            ByteSerializer<T>.Serialize(this, value);
        }

        /// <summary>
        /// Writes the specified array of objects to the writer using the <see cref="ByteSerializer{T}.Serialize"/> delegate.
        /// </summary>
        /// <typeparam name="T">The type of the array element.</typeparam>
        /// <param name="array">The source array.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public void WriteArray<T>(T[] array)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));

            if (ByteSerializer<T>.Serialize is null)
                throw new($"Serialization delegate was not set for type '{typeof(T).FullName}'");

            WriteInt32(array.Length);

            for (var i = 0; i < array.Length; i++)
                ByteSerializer<T>.Serialize(this, array[i]);
        }

        /// <summary>
        /// Writes the specified list of objects to the writer using the <see cref="ByteSerializer{T}.Serialize"/> delegate.
        /// </summary>
        /// <typeparam name="T">The type of the list element.</typeparam>
        /// <param name="list">The source list.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public void WriteList<T>(IList<T> list)
        {
            if (list is null)
                throw new ArgumentNullException(nameof(list));

            if (ByteSerializer<T>.Serialize is null)
                throw new($"Serialization delegate was not set for type '{typeof(T).FullName}'");

            WriteInt32(list.Count);

            for (var i = 0; i < list.Count; i++)
                ByteSerializer<T>.Serialize(this, list[i]);
        }

        /// <summary>
        /// Writes the content of an ArraySegment of bytes to the buffer.
        /// </summary>
        /// <param name="segment">The segment of bytes to write, including the offset and count.</param>
        public void WriteSegment(ArraySegment<byte> segment)
        {
            WriteInt32(segment.Count);
            
            for (var x = 0; x < segment.Count; x++)
                WriteByte(segment.At(x));
        }

        /// <summary>
        /// Writes the specified collection of objects to the writer using the <see cref="ByteSerializer{T}.Serialize"/> delegate.
        /// </summary>
        /// <typeparam name="T">The type of the collection element.</typeparam>
        /// <param name="collection">The source collection.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public void WriteCollection<T>(ICollection<T> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (ByteSerializer<T>.Serialize is null)
                throw new($"Serialization delegate was not set for type '{typeof(T).FullName}'");

            WriteInt32(collection.Count);

            foreach (var element in collection)
                ByteSerializer<T>.Serialize(this, element);
        }

        /// <summary>
        /// Writes the specified collection of objects to the writer using the <see cref="ByteSerializer{T}.Serialize"/> delegate.
        /// </summary>
        /// <typeparam name="T">The type of the collection element.</typeparam>
        /// <param name="collection">The source collection.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public void WriteEnumerable<T>(IEnumerable<T> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));

            if (ByteSerializer<T>.Serialize is null)
                throw new($"Serialization delegate was not set for type '{typeof(T).FullName}'");

            var position = Position;
            var count = 0; // Lets avoid another enumeration.

            Position += 4;

            foreach (var element in collection)
            {
                ByteSerializer<T>.Serialize(this, element);
                count++;
            }

            var curPosition = Position;

            Position = position;

            WriteInt32(count);

            Position = curPosition;
        }

        /// <summary>
        /// Writes the specified dictionary to the writer using the <see cref="ByteSerializer{T}.Serialize"/> delegate.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
        /// <param name="dictionary">The dictionary to write.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (ByteSerializer<TKey>.Serialize is null)
                throw new($"Serialization delegate was not set for type '{typeof(TKey).FullName}'");

            if (ByteSerializer<TValue>.Serialize is null)
                throw new($"Serialization delegate was not set for type '{typeof(TValue).FullName}''");

            WriteInt32(dictionary.Count);

            foreach (var pair in dictionary)
            {
                ByteSerializer<TKey>.Serialize(this, pair.Key);
                ByteSerializer<TValue>.Serialize(this, pair.Value);
            }
        }

        /// <summary>
        /// Resets the writer.
        /// </summary>
        public void Reset()
        {
            Position = 0;
        }

        /// <summary>
        /// Converts the written bytes in the buffer to a list.
        /// </summary>
        /// <returns>A list of bytes representing the current content of the buffer up to the current position.</returns>
        public List<byte> ToList()
        {
            var list = new List<byte>(Position);
            
            for (var x = 0; x < Position; x++)
                list.Add(Buffer[x]);
            
            return list;
        }

        /// <summary>
        /// Converts the written data in the buffer to a new byte array from the start of the buffer
        /// up to the current position.
        /// </summary>
        /// <returns>
        /// A byte array containing the data written to the buffer.
        /// </returns>
        public byte[] ToArray()
        {
            var bytes = new byte[Position];
            
            Array.ConstrainedCopy(Buffer, 0, bytes, 0, Position);
            return bytes;
        }

        /// <summary>
        /// Gets an array segment of the buffer.
        /// </summary>
        /// <returns>The created array segment.</returns>
        public ArraySegment<byte> ToSegment()
        {
            return new(Buffer, 0, Position);
        }

        /// <summary>
        /// Converts the written data in the buffer to a new array segment.
        /// </summary>
        /// <returns>An array segment containing a copy of the written data.</returns>
        public ArraySegment<byte> ToSegmentCopy()
        {
            return new(ToArray(), 0, Position);
        }
        
        /// <inheritdoc/>
        public override void OnUnPooled()
        {
            base.OnUnPooled();

            Reset();
        }

        /// <inheritdoc/>
        public override void OnPooled()
        {
            base.OnPooled();

            Reset();
        }

        /// <summary>
        /// Returns the writer to the pool.
        /// </summary>
        public override void ReturnToPool()
        {
            PoolBase<ByteWriter>.Shared.Return(this);
        }

        private void AdjustSize(int requiredBytes)
        {
            if (Position + requiredBytes >= buffer.Length)
            {
                if (IOSettings.BYTE_WRITER_BUFFER_RESIZING)
                {
                    Array.Resize(ref buffer, (Position + requiredBytes) * IOSettings.BYTE_WRITER_BUFFER_RESIZE_MULT);
                }
                else
                {
                    throw new EndOfStreamException($"Attempted to write beyond the allocated writer buffer (required {requiredBytes} more bytes - {Position} / {Buffer.Length}). " +
                                                   $"Set the 'DataApiSettings.WriterAllowResize' field to 'true' or write smaller messages.");
                }
            }
        }

        /// <summary>
        /// Retrieves an instance of <see cref="ByteWriter"/> from the shared pool.
        /// </summary>
        /// <returns>An instance of <see cref="ByteWriter"/> from the shared pool.</returns>
        public static ByteWriter Get()
            => PoolBase<ByteWriter>.Shared.Rent();

        /// <summary>
        /// Retrieves a pooled <see cref="ByteWriter"/>, executes the provided action on it,
        /// and returns the writer for further use.
        /// </summary>
        /// <param name="action">A delegate to perform operations on the retrieved <see cref="ByteWriter"/>.</param>
        /// <returns>The <see cref="ByteWriter"/> instance after the specified action is executed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="action"/> parameter is null.</exception>
        public static ByteWriter GetWrite(Action<ByteWriter> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var writer = Get();
            
            action(writer);
            return writer;
        }

        /// <summary>
        /// Executes a given action using a <see cref="ByteWriter"/> instance and retrieves the resulting byte array.
        /// </summary>
        /// <param name="action">
        /// The action to perform using the <see cref="ByteWriter"/> instance. This action is expected to manipulate
        /// the <see cref="ByteWriter"/> to generate the desired byte array content.
        /// </param>
        /// <returns>
        /// A byte array containing the data written to the <see cref="ByteWriter"/> during the execution of the action.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="action"/> parameter is null.
        /// </exception>
        public static byte[] GetArray(Action<ByteWriter> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (var writer = Get())
            {
                action(writer);
                return writer.ToArray();
            }
        }

        /// <summary>
        /// Executes the specified action with a <see cref="ByteWriter"/> instance and returns the resulting list of bytes.
        /// </summary>
        /// <param name="action">The action to perform using the <see cref="ByteWriter"/> instance.</param>
        /// <returns>A list of bytes generated by the <see cref="ByteWriter"/> instance after executing the action.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="action"/> parameter is null.</exception>
        public static List<byte> GetList(Action<ByteWriter> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (var writer = Get())
            {
                action(writer);
                return writer.ToList();
            }
        }

        internal static void InitWriters()
        {
            ByteSerializer<byte>.Serialize = (writer, value) => writer.WriteByte(value);
            ByteSerializer<sbyte>.Serialize = (writer, value) => writer.WriteSByte(value);

            ByteSerializer<short>.Serialize = (writer, value) => writer.WriteInt16(value);
            ByteSerializer<int>.Serialize = (writer, value) => writer.WriteInt32(value);
            ByteSerializer<long>.Serialize = (writer, value) => writer.WriteInt64(value);

            ByteSerializer<ushort>.Serialize = (writer, value) => writer.WriteUInt16(value);
            ByteSerializer<uint>.Serialize = (writer, value) => writer.WriteUInt32(value);
            ByteSerializer<ulong>.Serialize = (writer, value) => writer.WriteUInt64(value);

            ByteSerializer<float>.Serialize = (writer, value) => writer.WriteFloat(value);
            ByteSerializer<double>.Serialize = (writer, value) => writer.WriteDouble(value);
            ByteSerializer<decimal>.Serialize = (writer, value) => writer.WriteDecimal(value);

            ByteSerializer<byte[]>.Serialize = (writer, value) => writer.WriteBytes(value);
            ByteSerializer<string>.Serialize = (writer, value) => writer.WriteString(value);
            ByteSerializer<bool>.Serialize = (writer, value) => writer.WriteBool(value);

            ByteSerializer<IPAddress>.Serialize = (writer, value) => writer.WriteIpAddress(value);
            ByteSerializer<IPEndPoint>.Serialize = (writer, value) => writer.WriteIpEndPoint(value);

            ByteSerializer<DateTime>.Serialize = (writer, value) => writer.WriteDate(value);
            ByteSerializer<DateTimeOffset>.Serialize = (writer, value) => writer.WriteDateOffset(value);

            ByteSerializer<TimeSpan>.Serialize = (writer, value) => writer.WriteTime(value);
        }
    }
}