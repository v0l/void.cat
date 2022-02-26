namespace VoidCat.Model;

public static class Roles
{
    public const string User = "User";
    public const string Admin = "Admin";
}

public static class Policies
{
    public const string RequireAdmin = "RequireAdmin";
}

public static class CorsPolicy
{
    public const string Default = "default";
    public const string Upload = "upload";
}