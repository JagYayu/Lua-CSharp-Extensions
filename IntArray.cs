using System.Runtime.CompilerServices;
using Lua;

namespace LuaExt;

[LuaObject("IntArray")]
public sealed partial class Array : ILuaUserData
{
	public System.Array Raw { get; set; }
	public ValueType Type { get; private set; }

	[LuaMember("newInt8Array")]
	public static Array NewInt8Array(double length) => new(ValueType.Int8, (int)double.Floor(length));

	[LuaMember("newInt16Array")]
	public static Array NewInt16Array(double length) => new(ValueType.Int16, (int)double.Floor(length));

	[LuaMember("newInt32Array")]
	public static Array NewInt32Array(double length) => new(ValueType.Int32, (int)double.Floor(length));

	[LuaMember("newInt64Array")]
	public static Array NewInt64Array(double length) => new(ValueType.Int64, (int)double.Floor(length));

	private static bool _globalUserDataInit = true;

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static LuaValue GlobalUserDataRead(BinaryReader reader)
	{
		var type = (ValueType)reader.ReadSByte();
		var length = reader.ReadInt32();
		var intArray = new Array(type, length);

		switch (type)
		{
			case ValueType.Int8:
				{
					var array = (sbyte[])intArray.Raw;
					for (var i = 0; i < length; ++i)
					{
						array[i] = reader.ReadSByte();
					}
				}

				break;
			case ValueType.Int16:
				{
					var array = (short[])intArray.Raw;
					for (var i = 0; i < length; ++i)
					{
						array[i] = reader.ReadSByte();
					}
				}
				break;
			case ValueType.Int32:
				{
					var array = (int[])intArray.Raw;
					for (var i = 0; i < length; ++i)
					{
						array[i] = reader.ReadSByte();
					}
				}
				break;
			case ValueType.Int64:
				{
					var array = (long[])intArray.Raw;
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
		var intArray = (Array)userData;
		var array = intArray.Raw;
		writer.Write((sbyte)intArray.Type);
		var length = array.Length;

		unsafe
		{
			switch (intArray.Type)
			{
				case ValueType.Int8:
					fixed (sbyte* p = (sbyte[])array)
					{
						writer.Write(new ReadOnlySpan<byte>(p, length));
					}
					break;
				case ValueType.Int16:
					fixed (short* p = (short[])array)
					{
						writer.Write(new ReadOnlySpan<byte>(p, length));
					}
					break;
				case ValueType.Int32:
					fixed (int* p = (int[])array)
					{
						writer.Write(new ReadOnlySpan<byte>(p, length));
					}
					break;
				case ValueType.Int64:
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

	public Array(ValueType type, int length)
	{
		if (_globalUserDataInit)
		{
			_globalUserDataInit = false;
			StringBuffer.RegisterGlobalUserData<Array>(GlobalUserDataRead, GlobalUserDataWrite);
		}

		switch (type)
		{
			case ValueType.Int8:
				Raw = new sbyte[length];
				Type = type;
				break;
			case ValueType.Int16:
				Raw = new short[length];
				Type = type;
				break;
			case ValueType.Int32:
				Raw = new int[length];
				Type = type;
				break;
			case ValueType.Int64:
				Raw = new long[length];
				Type = type;
				break;
			default:
				Raw = System.Array.Empty<sbyte>();
				Type = ValueType.Int8;
				break;
		}
	}

	[LuaMember("get")]
	public double Get(double index)
	{
		var i = (int)index;
		return Type switch
		{
			ValueType.Int8 => ((sbyte[])Raw)[i],
			ValueType.Int16 => ((short[])Raw)[i],
			ValueType.Int32 => ((int[])Raw)[i],
			ValueType.Int64 => ((long[])Raw)[i],
			_ => (double)0,
		};
	}

	[LuaMember("set")]
	public void Set(double index, double value)
	{
		var i = (int)index;
		switch (Type)
		{
			case ValueType.Int8:
				((sbyte[])Raw)[i] = (sbyte)value;
				break;
			case ValueType.Int16:
				((short[])Raw)[i] = (short)value;
				break;
			case ValueType.Int32:
				((int[])Raw)[i] = (int)value;
				break;
			case ValueType.Int64:
				((long[])Raw)[i] = (long)value;
				break;
			default:
				break;
		}
	}

	[LuaMember("getLength")]
	public double GetLength() => Raw.Length;
}