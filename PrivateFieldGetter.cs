using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Lua;

namespace LuaExt;

internal static class PrivateFieldGetter
{
	private static readonly Dictionary<(Type, string), Delegate> _getterCache = [];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static TField Get<TClass, TField>(TClass @object, string fieldName)
	{
		var key = (typeof(TClass), fieldName);

		if (!_getterCache.TryGetValue(key, out var getter))
		{
			var field = typeof(TClass).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Debug.Assert(field != null);

			var param = Expression.Parameter(typeof(TClass), "instance");
			var lambda = Expression.Lambda<Func<TClass, TField>>(Expression.Field(param, field), param);
			getter = lambda.Compile();
			_getterCache[key] = getter;
		}

		return ((Func<TClass, TField>)getter)(@object);
	}

	internal static LuaValue[] GetPrivateFieldArray(this LuaTable table) => Get<LuaTable, LuaValue[]>(table, "array");
}
