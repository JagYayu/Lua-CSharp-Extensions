using Lua;

namespace LuaExt;

public static class LuaFunctionExtensions
{
	public static LuaValue[] InvokeSync(this LuaFunction function, LuaState state, LuaValue[] arguments, CancellationToken cancellationToken = default)
	{
		try
		{
			var valueTask = function.InvokeAsync(state, arguments, cancellationToken);
			if (valueTask.IsCompletedSuccessfully)
			{
				return valueTask.Result;
			}
			return valueTask.AsTask().Result;
		}
		catch (AggregateException ex)
		{
			var luaEx = FindLuaException(ex);
			if (luaEx != null)
			{
				throw luaEx;
			}
			throw;
		}
		catch (Exception ex)
		{
			var luaEx = FindLuaException(ex);
			if (luaEx != null)
			{
				throw luaEx;
			}
			throw;
		}
	}

	private static LuaException? FindLuaException(Exception? ex)
	{
		while (ex != null)
		{
			if (ex is LuaException lua)
			{
				return lua;
			}
			ex = ex.InnerException;
		}
		return null;
	}
}
