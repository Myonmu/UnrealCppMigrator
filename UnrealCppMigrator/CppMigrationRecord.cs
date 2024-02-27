namespace UnrealCppMigrator;

public record CppMigrationRecord
{
    public string sourceModule;
    public string destinationModule;
    public string sourceName;
    public SourceType sourceType;

    public string GenerateCoreRedirect()
    {
        var typeName = sourceType switch
        {
            SourceType.Class => "Class",
            SourceType.Struct => "Struct",
            SourceType.Enum => "Enum",
            _ => throw new ArgumentOutOfRangeException()
        };

        return $"+{typeName}Redirects=(OldName=\"/Script/{sourceModule}.{sourceName}\",NewName=\"/Script/{destinationModule}.{sourceName}\")";
    }
}