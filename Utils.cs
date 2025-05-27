using System.Runtime.CompilerServices;
using Lua;

namespace LuaExt;

public static class Utils
{
	public static T1? GetContextArguments<T1>(LuaFunctionExecutionContext context) => GetContextArgument<T1>(context, 1);

	public static (T1?, T2?) GetContextArguments<T1, T2>(LuaFunctionExecutionContext context) => (GetContextArgument<T1>(context, 1), GetContextArgument<T2>(context, 2));

	public static (T1?, T2?, T3?) GetContextArguments<T1, T2, T3>(LuaFunctionExecutionContext context) => (GetContextArgument<T1>(context, 1), GetContextArgument<T2>(context, 2), GetContextArgument<T3>(context, 3));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T? GetContextArgument<T>(LuaFunctionExecutionContext context, int index)
	{
		if (context.ArgumentCount >= index)
		{
			var arg = context.Arguments[index - 1];
			if (arg is T v1)
			{
				return v1;
			}
			else if (arg.TryRead(out T v2))
			{
				return v2;
			}
		}

		return default;
	}
}