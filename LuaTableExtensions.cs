using Lua;
using System.Runtime.CompilerServices;

namespace LuaExt;

public static partial class LuaTableExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static LuaTable ArrayCopy(this LuaTable table, int begin = 1, int length = int.MaxValue)
	{
		if (length < 0)
		{
			return new LuaTable(0, 0);
		}

		length = Math.Min(length, table.ArrayLength - begin + 1);
		var clone = new LuaTable(length, 0);
		return clone;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ClearArray(this LuaTable table) => System.Array.Clear(PrivateFieldGetter.Get<LuaTable, LuaValue[]>(table, "field"));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ClearTable(this LuaTable table)
	{
		table.ClearArray();
		table.Clear();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void DeepCopy(this LuaTable table)
	{
		throw new NotImplementedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetArrayCapacity(this LuaTable table) => table.GetPrivateFieldArray().Length;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T? GetValueOrDefault<T>(this LuaTable table, LuaValue key, T? @default = default)
	{
		if (table.TryGetValue(key, out LuaValue value) && value.TryRead(out T? result))
		{
			return result;
		}

		return @default;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T GetValueOrDefault<T>(this LuaTable table, LuaValue key, Func<T> @default)
	{
		if (table.TryGetValue(key, out LuaValue value) && value.TryRead(out T result))
		{
			return result;
		}

		return @default();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ShallowCopy(this LuaTable table)
	{
		var clone = new LuaTable(table.GetPrivateFieldArray().Length, table.HashMapCount);
		foreach (var (key, value) in table.Pairs())
		{
			clone[key] = value;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T[] ToArray<T>(this LuaTable table)
	{
		var array = new T[table.ArrayLength];

		foreach (var (k, v) in table.IPairs())
		{
			if (v.TryRead<T>(out var value))
			{
				array[(int)k - 1] = value;
			}
		}

		return array;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IEnumerable<(double key, LuaValue value)> IPairs(this LuaTable table)
	{
		double index = 1;
		while (table.TryGetValue(index, out var value))
		{
			yield return (index, value);
			index++;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IEnumerable<(double key, TValue value)> IPairs<TValue>(this LuaTable table)
	{
		double index = 1;
		while (table.TryGetValue(index, out var value))
		{
			yield return (index, value.Read<TValue>());
			index++;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IEnumerable<(LuaValue key, LuaValue value)> Pairs(this LuaTable table)
	{
		LuaValue key = LuaValue.Nil;
		while (table.TryGetNext(key, out var pair))
		{
			key = pair.Key;
			yield return (key, pair.Value);
		}
	}
}