namespace NiveraAPI.Results;

public class ErrorResult : Result
{
    public override bool IsSuccess => false;

    public virtual string Reason { get; }

    public new virtual Exception Error { get; }

    public ErrorResult(string reason, Exception exception)
        : base(null)
    {
        Reason = reason;
        Error = exception;
    }
}