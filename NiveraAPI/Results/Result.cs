namespace NiveraAPI.Results;

public class Result : IResult
{
    public virtual bool IsSuccess { get; }

    public virtual object Value { get; }

    public Result(object value)
    {
        Value = value;
    }

    public static IResult Success(object value = null)
    {
        return new SuccessResult(value);
    }

    public static IResult Error(string reason = "None", Exception exception = null)
    {
        return new ErrorResult(reason, exception);
    }
}