using Lua;

namespace LuaExt;

[LuaObject("Table")]
public sealed partial class Table
{
	[LuaMember("arrayCopy")]
	public static LuaTable ArrayCopy(LuaTable table, int begin, int length) => table.ArrayCopy(begin, length);

	[LuaMember("getArrayCapacity")]
	public static int GetArrayCapacity(LuaTable table) => table.GetArrayCapacity();

	[LuaMember("getHashMapCount")]
	public static int GetHashMapCount(LuaTable table) => table.HashMapCount;

	[LuaMember("newTable")]
	public static LuaTable NewTable(int arrayCapacity, int hashMapCapacity) => new(arrayCapacity, hashMapCapacity);
}
