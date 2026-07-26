namespace NiveraAPI.Results;

public static class ResultExtensions
{
    public static string ReadError(this IResult result)
    {
        if (result.IsSuccess)
        {
            return null;
        }
        if (!(result is ErrorResult errorResult))
        {
            return null;
        }
        return errorResult.Reason;
    }

    public static Exception ReadException(this IResult result)
    {
        if (result.IsSuccess)
        {
            return null;
        }
        if (!(result is ErrorResult errorResult))
        {
            return null;
        }
        return errorResult.Error;
    }

    public static TResult ReadResult<TResult>(this IResult result, bool failOnError = true)
    {
        TResult value;
        return result.TryReadResult<TResult>(failOnError, out value) ? value : default(TResult);
    }

    public static bool TryReadResult<TResult>(this IResult result, bool failOnError, out TResult value)
    {
        value = default(TResult);
        if (!result.IsSuccess && failOnError)
        {
            return false;
        }
        if (result.Value != null)
        {
            object value2 = result.Value;
            TResult val = default(TResult);
            int num;
            if (value2 is TResult)
            {
                val = (TResult)value2;
                num = 1;
            }
            else
            {
                num = 0;
            }
            if (num != 0)
            {
                value = val;
                return true;
            }
        }
        return false;
    }

    public static IResult CopyError(this IResult result)
    {
        return Result.Error(result.ReadError(), result.ReadException());
    }
}