using System.Runtime.CompilerServices;
using Lua;

namespace LuaExt;

public enum IntArrayType : sbyte
{
	Int8,
	Int16,
	Int32,
	Int64,
}

[LuaObject("IntArray")]
public sealed partial class IntArray : ILuaUserData
{
	public Array Array { get; set; }
	public IntArrayType Type { get; private set; }

	[LuaMember("newInt8Array")]
	public static IntArray NewInt8Array(double length) => new(IntArrayType.Int8, (int)double.Floor(length));

	[LuaMember("newInt16Array")]
	public static IntArray NewInt16Array(double length) => new(IntArrayType.Int16, (int)double.Floor(length));

	[LuaMember("newInt32Array")]
	public static IntArray NewInt32Array(double length) => new(IntArrayType.Int32, (int)double.Floor(length));

	[LuaMember("newInt64Array")]
	public static IntArray NewInt64Array(double length) => new(IntArrayType.Int64, (int)double.Floor(length));

	private static bool _globalUserDataInit = true;

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static LuaValue GlobalUserDataRead(BinaryReader reader)
	{
		var type = (IntArrayType)reader.ReadSByte();
		var length = reader.ReadInt32();
		var intArray = new IntArray(type, length);

		switch (type)
		{
			case IntArrayType.Int8:
				{
					var array = (sbyte[])intArray.Array;
					for (var i = 0; i < length; ++i)
					{
						array[i] = reader.ReadSByte();
					}
				}

				break;
			case IntArrayType.Int16:
				{
					var array = (short[])intArray.Array;
					for (var i = 0; i < length; ++i)
					{
						array[i] = reader.ReadSByte();
					}
				}
				break;
			case IntArrayType.Int32:
				{
					var array = (int[])intArray.Array;
					for (var i = 0; i < length; ++i)
					{
						array[i] = reader.ReadSByte();
					}
				}
				break;
			case IntArrayType.Int64:
				{
					var array = (long[])intArray.Array;
					for (var i = 0; i < length; ++i)
					{
						array[i] = reader.ReadSByte();
					}
				}
				break;
			default:
				break;
		}

		return intArray;
	}

	private static void GlobalUserDataWrite(BinaryWriter writer, ILuaUserData userData)
	{
		var intArray = (IntArray)userData;
		var array = intArray.Array;
		writer.Write((sbyte)intArray.Type);
		var length = array.Length;

		unsafe
		{
			switch (intArray.Type)
			{
				case IntArrayType.Int8:
					fixed (sbyte* p = (sbyte[])array)
					{
						writer.Write(new ReadOnlySpan<byte>(p, length));
					}
					break;
				case IntArrayType.Int16:
					fixed (short* p = (short[])array)
					{
						writer.Write(new ReadOnlySpan<byte>(p, length));
					}
					break;
				case IntArrayType.Int32:
					fixed (int* p = (int[])array)
					{
						writer.Write(new ReadOnlySpan<byte>(p, length));
					}
					break;
				case IntArrayType.Int64:
					fixed (long* p = (long[])array)
					{
						writer.Write(new ReadOnlySpan<byte>(p, length));
					}
					break;
				default:
					return;
			}
		}

		writer.Write(length);
	}

	public IntArray(IntArrayType type, int length)
	{
		if (_globalUserDataInit)
		{
			_globalUserDataInit = false;
			StringBuffer.RegisterGlobalUserData<IntArray>(GlobalUserDataRead, GlobalUserDataWrite);
		}

		switch (type)
		{
			case IntArrayType.Int8:
				Array = new sbyte[length];
				Type = type;
				break;
			case IntArrayType.Int16:
				Array = new short[length];
				Type = type;
				break;
			case IntArrayType.Int32:
				Array = new int[length];
				Type = type;
				break;
			case IntArrayType.Int64:
				Array = new long[length];
				Type = type;
				break;
			default:
				Array = Array.Empty<sbyte>();
				Type = IntArrayType.Int8;
				break;
		}
	}

	[LuaMember("get")]
	public double Get(double index)
	{
		var i = (int)index;
		return Type switch
		{
			IntArrayType.Int8 => ((sbyte[])Array)[i],
			IntArrayType.Int16 => ((short[])Array)[i],
			IntArrayType.Int32 => ((int[])Array)[i],
			IntArrayType.Int64 => ((long[])Array)[i],
			_ => (double)0,
		};
	}

	[LuaMember("set")]
	public void Set(double index, double value)
	{
		var i = (int)index;
		switch (Type)
		{
			case IntArrayType.Int8:
				((sbyte[])Array)[i] = (sbyte)value;
				break;
			case IntArrayType.Int16:
				((short[])Array)[i] = (short)value;
				break;
			case IntArrayType.Int32:
				((int[])Array)[i] = (int)value;
				break;
			case IntArrayType.Int64:
				((long[])Array)[i] = (long)value;
				break;
			default:
				break;
		}
	}

	[LuaMember("getLength")]
	public double GetLength() => Array.Length;
}