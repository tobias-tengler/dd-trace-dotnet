//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
#pragma warning disable CS0618, CS0649, CS1574, CS1580, CS1581, CS1584, CS1591, CS1573, CS8018, SYSLIB0011, SYSLIB0032
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8620, CS8714, CS8762, CS8765, CS8766, CS8767, CS8768, CS8769, CS8612, CS8629, CS8774
#nullable enable
// Decompiled with JetBrains decompiler
// Type: System.Memory`1
// Assembly: System.Memory, Version=4.0.1.2, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
// MVID: 805945F3-27B0-47AD-B8F6-389D9D8F82C3

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Datadog.Trace.VendoredMicrosoftCode.System.Runtime.InteropServices;
using Datadog.Trace.VendoredMicrosoftCode.System.Buffers;
using Datadog.Trace.VendoredMicrosoftCode.System.Runtime.CompilerServices.Unsafe;
using Unsafe = Datadog.Trace.VendoredMicrosoftCode.System.Runtime.CompilerServices.Unsafe.Unsafe;
using MemoryMarshal = Datadog.Trace.VendoredMicrosoftCode.System.Runtime.InteropServices.MemoryMarshal;

namespace Datadog.Trace.VendoredMicrosoftCode.System
{
    [DebuggerTypeProxy(typeof(MemoryDebugView<>))]
    [DebuggerDisplay("{ToString(),raw}")]
    internal readonly struct Memory<T>
    {
        private readonly object _object;
        private readonly int _index;
        private readonly int _length;
        private const int RemoveFlagsBitMask = 2147483647;

        [MethodImpl((MethodImplOptions)256)]
        public unsafe Memory(T[] array)
        {
            if (array == null)
            {
                Unsafe.AsRef<Memory<T>>(this) = new Memory<T>();
                //todo: fix *(Memory<T>*) ref this = new Memory<T>();
            }
            else
            {
                if ((object)default(T) == null && array.GetType() != typeof(T[]))
                    ThrowHelper.ThrowArrayTypeMismatchException();
                this._object = (object)array;
                this._index = 0;
                this._length = array.Length;
            }
        }

        [MethodImpl((MethodImplOptions)256)]
        internal unsafe Memory(T[] array, int start)
        {
            if (array == null)
            {
                if (start != 0)
                    Unsafe.AsRef<Memory<T>>(this) = new Memory<T>();
                // todo: fix *(Memory<T>*) ref this = new Memory<T>();
            }
            else
            {
                if (default(T) == null && array.GetType() != typeof(T[]))
                    ThrowHelper.ThrowArrayTypeMismatchException();
                if ((uint)start > (uint)array.Length)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this._object = array;
                this._index = start;
                this._length = array.Length - start;
            }
        }

        [MethodImpl((MethodImplOptions)256)]
        public unsafe Memory(T[] array, int start, int length)
        {
            if (array == null)
            {
                if (start != 0 || length != 0)
                    Unsafe.AsRef<Memory<T>>(this) = new Memory<T>();
                // todo: fix *(Memory<T>*) ref this = new Memory<T>();
            }
            else
            {
                if (default(T) == null && array.GetType() != typeof(T[]))
                    ThrowHelper.ThrowArrayTypeMismatchException();
                if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                this._object = array;
                this._index = start;
                this._length = length;
            }
        }

        [MethodImpl((MethodImplOptions)256)]
        internal Memory(MemoryManager<T> manager, int length)
        {
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();
            this._object = manager;
            this._index = int.MinValue;
            this._length = length;
        }

        [MethodImpl((MethodImplOptions)256)]
        internal Memory(MemoryManager<T> manager, int start, int length)
        {
            if (length < 0 || start < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException();
            this._object = manager;
            this._index = start | int.MinValue;
            this._length = length;
        }

        [MethodImpl((MethodImplOptions)256)]
        internal Memory(object obj, int start, int length)
        {
            this._object = obj;
            this._index = start;
            this._length = length;
        }

        public static implicit operator Memory<T>(T[] array) => new Memory<T>(array);

        public static implicit operator Memory<T>(ArraySegment<T> segment) => new Memory<T>(segment.Array, segment.Offset, segment.Count);

        public static implicit operator ReadOnlyMemory<T>(Memory<T> memory) => Unsafe.As<Memory<T>, ReadOnlyMemory<T>>(ref memory);

        public static Memory<T> Empty => new Memory<T>();

        public int Length => this._length & int.MaxValue;

        public bool IsEmpty => (this._length & int.MaxValue) == 0;

        public override string ToString()
        {
            if (!(typeof(T) == typeof(char)))
                return string.Format("System.Memory<{0}>[{1}]", typeof(T).Name, (this._length & int.MaxValue));
            return !(this._object is string str) ? this.Span.ToString() : str.Substring(this._index, this._length & int.MaxValue);
        }

        [MethodImpl((MethodImplOptions)256)]
        public Memory<T> Slice(int start)
        {
            int length = this._length;
            int num = length & int.MaxValue;
            if ((uint)start > (uint)num)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.start);
            return new Memory<T>(this._object, this._index + start, length - start);
        }

        [MethodImpl((MethodImplOptions)256)]
        public Memory<T> Slice(int start, int length)
        {
            int length1 = this._length;
            int num = length1 & int.MaxValue;
            if ((uint)start > (uint)num || (uint)length > (uint)(num - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();
            return new Memory<T>(this._object, this._index + start, length | length1 & int.MinValue);
        }

        public System.Span<T> Span
        {
            [MethodImpl((MethodImplOptions)256)]
            get
            {
                if (this._index < 0)
                    return ((MemoryManager<T>)this._object).GetSpan().Slice(this._index & int.MaxValue, this._length);
                if (typeof(T) == typeof(char) && this._object is string o)
                    return new System.Span<T>(Unsafe.As<Pinnable<T>>(o), MemoryExtensions.StringAdjustment, o.Length).Slice(this._index, this._length);
                return this._object != null ? new System.Span<T>((T[])this._object, this._index, this._length & int.MaxValue) : new System.Span<T>();
            }
        }

        public void CopyTo(Memory<T> destination) => this.Span.CopyTo(destination.Span);

        public bool TryCopyTo(Memory<T> destination) => this.Span.TryCopyTo(destination.Span);

        public unsafe MemoryHandle Pin()
        {
            if (this._index < 0)
                return ((MemoryManager<T>)this._object).Pin(this._index & int.MaxValue);
            if (typeof(T) == typeof(char) && this._object is string str)
            {
                GCHandle handle = GCHandle.Alloc(str, GCHandleType.Pinned);
                return new MemoryHandle(Unsafe.Add<T>((void*)handle.AddrOfPinnedObject(), this._index), handle);
            }
            if (!(this._object is T[] objArray))
                return new MemoryHandle();
            if (this._length < 0)
                return new MemoryHandle(Unsafe.Add<T>(Unsafe.AsPointer<T>(ref MemoryMarshal.GetReference<T>((System.Span<T>)objArray)), this._index));
            GCHandle handle1 = GCHandle.Alloc(objArray, GCHandleType.Pinned);
            return new MemoryHandle(Unsafe.Add<T>((void*)handle1.AddrOfPinnedObject(), this._index), handle1);
        }

        public T[] ToArray() => this.Span.ToArray();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case ReadOnlyMemory<T> readOnlyMemory:
                    return readOnlyMemory.Equals((ReadOnlyMemory<T>)this);
                case Memory<T> other:
                    return this.Equals(other);
                default:
                    return false;
            }
        }

        public bool Equals(Memory<T> other) => this._object == other._object && this._index == other._index && this._length == other._length;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            if (this._object == null)
                return 0;
            int hashCode1 = this._object.GetHashCode();
            int num = this._index;
            int hashCode2 = num.GetHashCode();
            num = this._length;
            int hashCode3 = num.GetHashCode();
            return Memory<T>.CombineHashCodes(hashCode1, hashCode2, hashCode3);
        }

        private static int CombineHashCodes(int left, int right) => (left << 5) + left ^ right;

        private static int CombineHashCodes(int h1, int h2, int h3) => Memory<T>.CombineHashCodes(Memory<T>.CombineHashCodes(h1, h2), h3);
    }
}