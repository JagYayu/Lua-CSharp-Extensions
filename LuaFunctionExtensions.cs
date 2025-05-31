using Lua;

namespace LuaExt;

public static class LuaFunctionExtensions
{
	public static LuaValue[] InvokeSync(this LuaFunction function, LuaState state, LuaValue[] arguments, CancellationToken cancellationToken = default)
	{
		var valueTask = function.InvokeAsync(state, arguments, cancellationToken);
		if (valueTask.IsCompletedSuccessfully)
		{
			return valueTask.Result;
		}
		return valueTask.AsTask().GetAwaiter().GetResult();
	}
}
