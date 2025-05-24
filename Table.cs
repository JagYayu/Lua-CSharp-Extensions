using Lua;

namespace LuaExt;

[LuaObject("Table")]
public sealed partial class Table
{
	[LuaMember("arrayCopy")]
	public static LuaTable ArrayCopy(LuaTable table, double begin, double length) => table.ArrayCopy((int)begin, (int)length);

	[LuaMember("getArrayCapacity")]
	public static double GetArrayCapacity(LuaTable table) => table.GetArrayCapacity();

	[LuaMember("getHashMapCount")]
	public static double GetHashMapCount(LuaTable table) => table.HashMapCount;

	[LuaMember("newTable")]
	public static LuaTable NewTable(double arrayCapacity, double hashMapCapacity) => new((int)arrayCapacity, (int)hashMapCapacity);
}
