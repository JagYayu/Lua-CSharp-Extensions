#define TEST

using Lua;

namespace LuaExt;

public static class Extensions
{
	public static LuaTable ToLuaTable<T>(this IList<T> list) where T : ILuaUserData
	{
		var table = new LuaTable(list.Count, 0);
		for (int i = 0; i < list.Count; i++)
		{
			table[i + 1] = new(list[i]);
		}
		return table;
	}

	public static LuaTable ToLuaTable(this IList<string> list)
	{
		var table = new LuaTable(list.Count, 0);
		for (int i = 0; i < list.Count; i++)
		{
			table[i + 1] = new(list[i]);
		}
		return table;
	}
}