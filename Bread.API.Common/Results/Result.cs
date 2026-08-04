
namespace Bread.API.Common.Results;

public readonly record struct Result
{
    public bool Succeeded { get;}
    public Errors[] Errors { get;}

    public Result (bool succeeded, Errors[] errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public static Result Success()
        => new Result(true, []);

    public static Result Failure(params Errors[] errors)
        => new Result(false, errors);

    public static Result NotFound(params Errors[] errors)
        => new Result(false, errors);

    public static Result BadRequest(params Errors[] errors)
        => new Result(false, errors);

    public static Result Conflict(params Errors[] errors)
        => new Result(false, errors);

    public static Result Forbidden(params Errors[] errors)
        => new Result(false, errors);

    public static Result Unauthorized(params Errors[] errors)
        => new Result(false, errors);

    public static Result Validation(params Errors[] errors)
        => new Result(false,errors);

    public static Result Combine(params Result[] result)
        => result.Any(r => !r.Succeeded) ?
        Failure(result.Where(r => !r.Succeeded).SelectMany(r => r.Errors).ToArray())
        : Success();
}

public readonly record struct Result<T>
{
    public bool Succeeded { get; }
    public T? Value { get; }
    public Errors[] Errors { get; }

    public Result(bool succeeded,T? value, Errors[] errors)
    {
        Succeeded = succeeded;
        Value = value;
        Errors = errors;
    }

    public static Result<T> Success(T value)
        => new Result<T>(true, value, []);

    public static Result<T> NotFound(params Errors[] errors)
        => new Result<T>(false, default, errors);

    public static Result<T> BadRequest(params Errors[] errors)
        => new Result<T>(false, default, errors);

    public static Result<T> Failure(params Errors[] errors)
        => new Result<T>(false, default, errors);

    public static Result<T> Conflict(params Errors[] errors)
        => new Result<T>(false, default, errors);

    public static Result<T> Forbidden(params Errors[] errors)
        => new Result<T>(false, default, errors);

    public static Result<T> Unauthorized(params Errors[] errors)
        => new Result<T>(false, default, errors);

    public static Result<T> Validation(params Errors[] errors)
        => new Result<T>(false, default, errors);
}
