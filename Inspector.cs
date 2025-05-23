using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Lua;

namespace LuaExt;

[LuaObject("LuaInspectorOptions")]
public sealed partial class InspectorOptions
{
	public int Depth { get; set; }
	public bool LuaIdentifiable { get; set; }
	public Func<ILuaUserData, LuaValue>? GetUserdataRepresentation { get; set; }
	public Regex IdentifierPattern { get; set; } = IdentifierRegex();
	public LuaState? State { get; set; }

	[GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$")]
	private static partial Regex IdentifierRegex();

	internal static InspectorOptions Duplicate(InspectorOptions options) => new()
	{
		Depth = options.Depth,
		GetUserdataRepresentation = options.GetUserdataRepresentation,
		IdentifierPattern = options.IdentifierPattern,
		State = options.State,
	};
}

[LuaObject("LuaInspector")]
public sealed partial class Inspector
{
	public static InspectorOptions DefaultOptions { get; private set; } = new()
	{
		Depth = 32,
	};

	private static Inspector? _instance;
	public static Inspector Instance => _instance ??= new(DefaultOptions);

	// public static LuaState? DefaultLuaState
	// {
	// 	get
	// 	{

	// 	}
	// }
	private readonly StringBuilder _buffer = new();
	private int _depth;
	private readonly Dictionary<LuaFunction, int> _functionIDs = [];
	private InspectorOptions _options;
	private readonly Dictionary<LuaTable, int> _tableIDs = [];
	private readonly Dictionary<LuaTable, bool> _tableRecurred = [];
	private readonly Dictionary<LuaThread, int> _threadIDs = [];
	private readonly Dictionary<ILuaUserData, int> _userDataIDs = [];

	private InspectorOptions ModifyOptions
	{
		get
		{
			if (_options == DefaultOptions)
			{
				_options = InspectorOptions.Duplicate(_options);
			}
			return _options;
		}
	}

	public int Depth
	{
		get => _options.Depth;
		set => ModifyOptions.Depth = value;
	}

	public Inspector()
	{
		_options = DefaultOptions;
	}

	public Inspector(InspectorOptions options)
	{
		_options = options;
	}

	[LuaMember("dump")]
	public string Dump(LuaValue value)
	{
		if (value.TryRead(out LuaTable table))
		{
			return Dump(table);
		}
		else
		{
			return value.ToString();
		}
	}

	public string Dump(LuaTable table)
	{
		EvaluateRecurredTables(table);
		AppendValue(table);
		var str = _buffer.ToString();
		Reset();
		return str;
	}

	private void Reset()
	{
		_buffer.Clear();
		_depth = 0;
		_functionIDs.Clear();
		_tableRecurred.Clear();
		_tableIDs.Clear();
		_userDataIDs.Clear();
	}

	private void AppendKey(LuaValue key)
	{
		if (key.TryRead(out string str) && _options.IdentifierPattern.Match(str).Success)
		{
			_buffer.Append(str);
		}
		else
		{
			_buffer.Append('[');
			AppendValue(key);
			_buffer.Append(']');
		}
	}

	private void AppendValue(LuaValue value)
	{
		switch (value.Type)
		{
			case LuaValueType.String:
				AppendValue(value.ToString());
				break;
			case LuaValueType.Number:
			case LuaValueType.Boolean:
			case LuaValueType.Nil:
				_buffer.Append(value.ToString());
				break;
			case LuaValueType.Table:
				AppendValue(value.Read<LuaTable>());
				break;
			case LuaValueType.Function:
				AppendValue(value.Read<LuaFunction>());
				break;
			case LuaValueType.Thread:
				AppendValue(value.Read<LuaThread>());
				break;
			case LuaValueType.UserData:
				AppendValue(value.Read<ILuaUserData>());
				break;
			default:
				_buffer.Append("<unknown>");
				break;
		}
	}

	private void AppendValue(LuaTable table)
	{
		var id = GetID(table, out var init);

		if (!init)
		{
			_buffer.Append($"<table:{id}>");
			return;
		}

		if (_depth > _options.Depth)
		{
			_buffer.Append($"{{ --[[ {table.ToString()} ]] }}");
			return;
		}

		if (_tableRecurred.TryGetValue(table, out var recurred) && recurred)
		{
			_buffer.Append($"<{GetID(table)}>");
		}

		_buffer.Append('{');
		_depth++;

		var comment = EvaluateToString(table);
		if (comment != null)
		{
			_buffer.Append(" -- ");
			_buffer.Append(comment);
		}

		ResolveTableKeys(table, out var arraySequenceLength, out var dictionaryKeys);

		var separate = false;

		for (var i = 1; i <= arraySequenceLength; i++)
		{
			if (separate)
			{
				_buffer.Append(',');
			}

			_buffer.Append(' ');
			AppendValue(table[i]);

			separate = true;
		}

		foreach (var key in dictionaryKeys)
		{
			if (separate)
			{
				_buffer.Append(',');
			}

			AppendNewEntry();
			AppendKey(key);
			_buffer.Append(" = ");
			AppendValue(table[key]);

			separate = true;
		}

		if (table.Metatable != null)
		{
			if (separate)
			{
				_buffer.Append(',');
			}

			AppendNewEntry();
			_buffer.Append("<metatable> = ");
			AppendValue(table.Metatable);
		}

		_depth--;

		if (dictionaryKeys.Count > 0 || table.Metatable != null)
		{
			AppendNewEntry();
		}
		else if (arraySequenceLength > 0)
		{
			_buffer.Append(' ');
		}

		_buffer.Append('}');
	}

	private void AppendValue(LuaFunction function)
	{
		_buffer.Append($"<function:{GetID(function)}>");
	}

	private void AppendValue(LuaThread thread)
	{
		_buffer.Append($"<thread:{GetID(thread)}>");
	}

	private void AppendValue(ILuaUserData userData)
	{
		if (_options.GetUserdataRepresentation == null)
		{
			_buffer.Append($"<userdata:{GetID(userData)}>");

			return;
		}

		var value = _options.GetUserdataRepresentation(userData);
		switch (value.Type)
		{
			case LuaValueType.String:
				AppendValue(value.ToString());
				break;
			case LuaValueType.Number:
			case LuaValueType.Boolean:
			case LuaValueType.Nil:
				_buffer.Append(value.ToString());
				break;
			case LuaValueType.Table:
				var table = value.Read<LuaTable>();
				EvaluateRecurredTables(table);
				AppendValue(table);
				break;
			case LuaValueType.UserData:
				_buffer.Append($"<userdata:{GetID(userData)}>");
				break;
			case LuaValueType.Function:
			default:
				_buffer.Append("<unknown>");
				break;
		}
	}

	private void AppendValue(string str)
	{
		_buffer.Append('"');
		_buffer.Append(str);
		_buffer.Append('"');
	}

	private void EvaluateRecurredTables(LuaTable table)
	{
		var seen = _tableRecurred.TryGetValue(table, out var recurred);
		if (seen)
		{
			if (!recurred)
			{
				_tableRecurred[table] = true;
			}

			return;
		}

		_tableRecurred.Add(table, false);

		LuaValue key = LuaValue.Nil;
		while (table.TryGetNext(key, out var pair))
		{
			key = pair.Key;
			if (key.TryRead(out LuaTable inner))
			{
				EvaluateRecurredTables(inner);
			}
			if (pair.Value.TryRead(out inner))
			{
				EvaluateRecurredTables(inner);
			}
		}
		if (table.Metatable != null)
		{
			EvaluateRecurredTables(table.Metatable);
		}
	}


	private void AppendNewEntry()
	{
		_buffer.Append('\n');
		_buffer.Append('\t', _depth);
	}

	private string? EvaluateToString(LuaTable table)
	{
		var state = _options.State;
		if (state == null)
		{
			return null;
		}

		var metatable = table.Metatable;
		if (metatable == null || !metatable.TryGetValue("__tostring", out LuaValue toString) || !toString.TryRead(out LuaFunction function))
		{
			return null;
		}

		var result = function.InvokeAsync(state, [table], CancellationToken.None).AsTask().Result;
		if (result.Length == 0 || !result[0].TryRead(out string value))
		{
			return null;
		}

		return value;
	}

	private static void ResolveTableKeys(LuaTable table, out int arraySequenceLength, out List<LuaValue> dictionaryKeys)
	{
		var arraySpan = table.GetArraySpan();
		arraySequenceLength = arraySpan.Length;
		for (int i = 0; i < arraySequenceLength; i++)
		{
			if (arraySpan[i].Type == LuaValueType.Nil)
			{
				arraySequenceLength = i;
				break;
			}
		}

		dictionaryKeys = new List<LuaValue>(table.HashMapCount);
		LuaValue key = LuaValue.Nil;
		while (table.TryGetNext(key, out var pair))
		{
			key = pair.Key;
			if (!key.TryRead(out double number) || !double.IsInteger(number) || number < 1 || number > arraySequenceLength)
			{
				dictionaryKeys.Add(key);
			}
		}
		dictionaryKeys.Sort(TableKeyComparer);
	}

	private static readonly Dictionary<LuaValueType, int> TypeOrders = new() {
		{ LuaValueType.Number, 1 },
		{ LuaValueType.Boolean, 2 },
		{ LuaValueType.String, 3 },
		{ LuaValueType.Table, 4 },
		{ LuaValueType.Function, 5 },
		{ LuaValueType.Thread, 6 },
		{ LuaValueType.UserData, 7 },
	};

	private static int TableKeyComparer(LuaValue l, LuaValue r)
	{
		var lt = l.Type;
		var rt = r.Type;
		if (lt == rt)
		{
			if (lt == LuaValueType.Number)
			{
				var res = l.Read<double>() - r.Read<double>();
				return res > 0 ? 1 : res < 0 ? -1 : 0;
			}
			if (lt == LuaValueType.String)
			{
				return string.CompareOrdinal(l.Read<string>(), r.Read<string>());
			}
		}

		var blo = TypeOrders.TryGetValue(lt, out var lto);
		var bro = TypeOrders.TryGetValue(rt, out var rto);
		if (blo && bro)
		{
			return lto - rto;
		}
		else if (blo)
		{
			return 1;
		}
		else if (bro)
		{
			return -1;
		}
		else
		{
			return lt - rt;
		}
	}

	private int GetID(LuaFunction function) => GetOrNewID(_functionIDs, function, out _);
	private int GetID(LuaTable table) => GetOrNewID(_tableIDs, table, out _);
	private int GetID(LuaTable table, out bool init) => GetOrNewID(_tableIDs, table, out init);
	private int GetID(LuaThread thread) => GetOrNewID(_threadIDs, thread, out _);
	private int GetID(ILuaUserData userData) => GetOrNewID(_userDataIDs, userData, out _);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int GetOrNewID<T>(Dictionary<T, int> idMap, T value, out bool init) where T : notnull
	{
		if (!idMap.TryGetValue(value, out var id))
		{
			init = true;
			id = idMap.Count + 1;
			idMap[value] = id;
		}
		else
		{
			init = false;
		}

		return id;
	}
}
