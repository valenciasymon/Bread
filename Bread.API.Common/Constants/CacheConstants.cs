namespace Bread.API.Common.Constants;

public static class CacheConstants
{
    public const string AuthenticatedUserCachingPolicy = "AuthenticatedUserCachingPolicy";
    public const string AuthenticatedUserCachingPolicyTag = "authpolicy-";

    public const int ShortDuration = 60;
    public const int MediumDuration = 300;
    public const int LongDuration = 900;
}
