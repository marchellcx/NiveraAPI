namespace NiveraAPI.Results;

public class SuccessResult : Result
{
    public override bool IsSuccess => true;

    public SuccessResult(object value)
        : base(value)
    {
    }
}