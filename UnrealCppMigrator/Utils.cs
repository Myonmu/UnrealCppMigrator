namespace UnrealCppMigrator;

public static class Utils
{
    public static DirectoryInfo GetChildDirectory(this DirectoryInfo root, string relativePath)
    {
        return new DirectoryInfo(Path.Combine(root.FullName, relativePath));
    }

    public static FileInfo GetChildFile(this DirectoryInfo root, string relativePath)
    {
        return new FileInfo(Path.Combine(root.FullName, relativePath));
    }
    
    public static SourceType FindSourceType(string declaration)
    {
        switch (declaration)
        {
            case "UENUM": return SourceType.Enum;
            case "USTRUCT": return SourceType.Struct;
            default: return SourceType.Class;
        }
    }
}