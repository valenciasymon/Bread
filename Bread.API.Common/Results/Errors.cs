namespace Bread.API.Common.Results;

public readonly record struct Errors (string Code, string Description)
{
    public static readonly Errors None = new ("", "");

    public bool IsNone => string.IsNullOrWhiteSpace(Code);
}
