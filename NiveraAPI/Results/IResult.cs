namespace NiveraAPI.Results;

public interface IResult
{
    bool IsSuccess { get; }

    object Value { get; }
}
