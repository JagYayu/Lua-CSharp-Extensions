using System.IO.Compression;
using System.Runtime.CompilerServices;
using Lua;

namespace LuaExt;

using UserDataReader = Func<IStringBufferReader, LuaValue>;
using UserDataWriter = Action<IStringBufferWriter, ILuaUserData>;

public interface IStringBufferReader
{
	public bool ReadBoolean();
	public byte ReadByte();
	public float ReadFloat();
	public double ReadDouble();
	public int ReadInt();
	public long ReadLong();
	public sbyte ReadSByte();
	public short ReadShort();
	public string ReadString();
}

public interface IStringBufferWriter
{
	public void Write(bool value);
	public void Write(byte value);
	public void Write(float value);
	public void Write(double value);
	public void Write(int value);
	public void Write(long value);
	public void Write(sbyte value);
	public void Write(short value);
	public void Write(string value);

	public void Write(sbyte[] value);
	public void Write(short[] value);
	public void Write(int[] value);
	public void Write(long[] value);
}

internal sealed class StringBufferBinaryReader(Stream stream) : BinaryReader(stream), IStringBufferReader
{
	public float ReadFloat() => ReadSingle();
	public int ReadInt() => ReadInt32();
	public long ReadLong() => ReadInt64();
	public short ReadShort() => ReadShort();
}

internal sealed class StringBufferBinaryWriter(Stream stream) : BinaryWriter(stream), IStringBufferWriter
{
	public void Write(sbyte[] value)
	{
		unsafe
		{
			fixed (sbyte* p = value)
			{
				Write(new ReadOnlySpan<byte>(p, value.Length));
			}
		}
	}

	public void Write(short[] value)
	{
		unsafe
		{
			fixed (short* p = value)
			{
				Write(new ReadOnlySpan<byte>(p, value.Length));
			}
		}
	}

	public void Write(int[] value)
	{
		unsafe
		{
			fixed (int* p = value)
			{
				Write(new ReadOnlySpan<byte>(p, value.Length));
			}
		}
	}

	public void Write(long[] value)
	{
		unsafe
		{
			fixed (long* p = value)
			{
				Write(new ReadOnlySpan<byte>(p, value.Length));
			}
		}
	}
}

[LuaObject("StringBufferData")]
public sealed partial class StringBufferData
{
	public byte[] Bytes;

	internal StringBufferData(byte[] bytes)
	{
		Bytes = bytes;
	}

	[LuaMember("getSize")]
	internal double GetSize() => Bytes.Length;
}

[LuaObject("StringBufferOptions")]
public sealed partial class StringBufferOptions
{
	public bool Compress { get; init; } = false;
	public int MaxDepth { get; init; } = 32;
	public bool SuppressError { get; init; } = true;
	public UserDataReader? UserDataReader { get; init; }
	public UserDataWriter? UserDataWriter { get; init; }
}

/// <summary>
/// Serialize lua values into binary string, or deserialize into lua values.
/// ⚠ Does not support tables that contains lua threads, lua functions, or have circular references.
/// ⚠ Serializing tables with the same reference will create different copies.
/// In addition, `Serialize` & `Deserialize` functions are used for library usages, you can wrap custom writer and reader. (e.g. serializing in LiteNetLib)
/// </summary>
[LuaObject("StringBuffer")]
public sealed partial class StringBuffer
{
	public static StringBufferOptions DefaultOptions { get; private set; } = new();

	private static StringBuffer? _instance;
	public static StringBuffer Instance => _instance ??= new(DefaultOptions);

	private static readonly Dictionary<string, (UserDataReader read, UserDataWriter write)> _globalUserData = [];

	public static void RegisterGlobalUserData<T>(UserDataReader read, UserDataWriter write)
	{
		var fullName = typeof(T).FullName;
		if (fullName == null)
		{
			throw new Exception("Invalid userdata class");
		}
		else if (_globalUserData.ContainsKey(fullName))
		{
			throw new Exception("Already registered");
		}

		string.Intern(fullName);
		_globalUserData[fullName] = new(read, write);
	}

	private readonly StringBufferOptions _options;

	public StringBuffer()
	{
		_options = DefaultOptions;
	}

	public StringBuffer(StringBufferOptions options)
	{
		_options = options;
	}

	[LuaMember("encode")]
	public StringBufferData Encode(LuaValue luaValue)
	{
		using var baseStream = new MemoryStream();
		using Stream stream = _options.Compress ? new DeflateStream(baseStream, CompressionLevel.Fastest, leaveOpen: true) : baseStream;
		using var writer = new StringBufferBinaryWriter(stream);
		WriteValue(writer, luaValue, 1);
		return new(baseStream.GetBuffer());
	}

	[LuaMember("encodeString")]
	public string EncodeString(LuaValue value) => Convert.ToBase64String(Encode(value).Bytes);

	public void Serialize(LuaValue luaValue, IStringBufferWriter writer) => WriteValue(writer, luaValue, 1);

	private void WriteValue(IStringBufferWriter writer, LuaValue luaValue, int depth)
	{
		writer.Write((byte)luaValue.Type);
		switch (luaValue.Type)
		{
			case LuaValueType.Nil:
				break;
			case LuaValueType.Boolean:
				writer.Write(luaValue.Read<bool>());
				break;
			case LuaValueType.Number:
				writer.Write(luaValue.Read<double>());
				break;
			case LuaValueType.String:
				writer.Write(luaValue.Read<string>());
				break;
			case LuaValueType.Table:
				WriteTable(writer, luaValue.Read<LuaTable>(), depth + 1);
				break;
			case LuaValueType.UserData:
				WriteUserData(writer, luaValue.Read<ILuaUserData>());
				break;
			default:
				WriteUnknown(writer, luaValue.Read<object>());
				break;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteTable(IStringBufferWriter writer, LuaTable table, int depth)
	{
		if (depth > _options.MaxDepth)
		{
			OnMaxDepthExceed();
		}

		writer.Write((byte)LuaValueType.Table);
		writer.Write(table.GetArrayCapacity());
		writer.Write(table.HashMapCount);
		foreach (var (key, value) in table.Pairs())
		{
			WriteValue(writer, key, depth);
			WriteValue(writer, value, depth);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void WriteUserData(IStringBufferWriter writer, ILuaUserData userData)
	{
		writer.Write((byte)LuaValueType.UserData);
		var fullName = userData.GetType().FullName;
		if (fullName != null && _globalUserData.TryGetValue(fullName, out var functions))
		{
			writer.Write(fullName);
			functions.write(writer, userData);
		}
		else if (_options.UserDataWriter != null)
		{
			writer.Write(string.Empty);
			_options.UserDataWriter(writer, userData);
		}
		else if (!_options.SuppressError)
		{
			throw new Exception("Unsupported user data");
		}
	}

	private void WriteUnknown(IStringBufferWriter _1, object _2)
	{
		if (!_options.SuppressError)
		{
			throw new Exception("Not support");
		}
	}

	[LuaMember("decode")]
	public LuaValue Decode(StringBufferData data)
	{
		using var baseStream = new MemoryStream(data.Bytes);
		using Stream stream = _options.Compress ? new DeflateStream(baseStream, CompressionMode.Decompress) : baseStream;
		using var reader = new StringBufferBinaryReader(stream);
		return ReadValue(reader, 0);
	}

	[LuaMember("decodeString")]
	public LuaValue DecodeString(string str) => Decode(new StringBufferData(Convert.FromBase64String(str)));

	public LuaValue Deserialize(IStringBufferReader reader)
	{
		return ReadValue(reader, 1);
	}

	private LuaValue ReadValue(IStringBufferReader reader, int depth)
	{
		if (depth > _options.MaxDepth)
		{
			return OnMaxDepthExceed();
		}

		var type = (LuaValueType)reader.ReadByte();
		return type switch
		{
			LuaValueType.Nil => LuaValue.Nil,
			LuaValueType.Boolean => (LuaValue)reader.ReadBoolean(),
			LuaValueType.Number => (LuaValue)reader.ReadDouble(),
			LuaValueType.String => (LuaValue)reader.ReadString(),
			LuaValueType.Table => (LuaValue)ReadTable(reader, depth + 1),
			LuaValueType.UserData => ReadUserData(reader),
			_ => ReadUnknown(reader),
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private LuaTable ReadTable(IStringBufferReader reader, int depth)
	{
		var arrayLength = reader.ReadInt();
		var hashMapCount = reader.ReadInt();
		var table = new LuaTable(arrayLength, hashMapCount);
		for (int _ = 0; _ < arrayLength + hashMapCount; _++)
		{
			var key = ReadValue(reader, depth);
			var value = ReadValue(reader, depth);
			table[key] = value;
		}
		return table;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private LuaValue ReadUserData(IStringBufferReader reader)
	{
		var fullName = reader.ReadString();
		if (fullName != string.Empty && _globalUserData.TryGetValue(fullName, out var functions))
		{
			return functions.read(reader);
		}
		if (_options.UserDataReader != null)
		{
			return _options.UserDataReader.Invoke(reader);
		}
		if (_options.SuppressError)
		{
			return LuaValue.Nil;
		}
		throw new Exception("Unsupported userData");
	}

	private LuaValue ReadUnknown(IStringBufferReader _)
	{
		if (!_options.SuppressError)
		{
			throw new Exception("Unsupported value type");
		}
		return LuaValue.Nil;
	}

	private LuaValue OnMaxDepthExceed()
	{
		if (!_options.SuppressError)
		{
			throw new Exception("Max depth exceeded");
		}

		return LuaValue.Nil;
	}
}