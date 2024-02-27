using System.Text;
using System.Text.RegularExpressions;

namespace UnrealCppMigrator
{
    internal class Program
    {
        public enum SourceType{
            Class,
            Struct,
            Enum
        }
        public static void Main(string[] args)
        {
            var sourceRoot = args.Length < 1 ? Directory.GetCurrentDirectory() : args[0];
            var projectDir = new DirectoryInfo(sourceRoot);
            if (!projectDir.Exists)
            {
                throw new InvalidOperationException(
                    "The specified directory does not exist. Please check your command line argument.");
            }

            var sourceDir = new DirectoryInfo(Path.Combine(sourceRoot, "Source"));
            if (!sourceDir.Exists)
            {
                throw new InvalidOperationException(
                    "The specified directory does not contain a Source folder, check your command line argument or where this program is placed if you have specified none.");
            }

            var iniFile = new FileInfo(Path.Combine(sourceRoot, "Config/DefaultEngine.ini"));
            if (!iniFile.Exists)
            {
                throw new FileNotFoundException($"Cannot find DefaultEngine.ini at {iniFile.FullName}");
            }

            var moduleDirs = sourceDir.GetDirectories();
            Console.WriteLine("============== Found Modules ===============");
            foreach (var dir in moduleDirs)
            {
                Console.WriteLine(dir.Name);
            }

            Console.WriteLine("============================================");
            var apiMacros2Module = moduleDirs.ToDictionary(
                x => x.Name.ToUpperInvariant() + "_API",
                x => x.Name);
            var detectedMigrations = new Dictionary<Tuple<string, string>,SourceType>();

            SourceType FindSourceType(string declaration)
            {
                switch (declaration)
                {
                    case "UENUM": return SourceType.Enum;
                    case "USTRUCT": return SourceType.Struct;
                    default: return SourceType.Class;
                }
            }
            var redirects = new StringBuilder();

            redirects.AppendLine("[CoreRedirects]");
            
            void DetectMigrationsInDirectory(string moduleName, DirectoryInfo directoryInfo)
            {
                var headers = directoryInfo.GetFiles("*.h");
                foreach (var header in headers)
                {
                    var headerText = File.ReadAllText(header.FullName);
                    var match = Regex.Match(headerText, $@"\b(?!{moduleName.ToUpperInvariant()})\w+_API\b");
                    if (!match.Success) continue;
                    //replace macro
                    var originModule = apiMacros2Module[match.Value];
                    var fileName = Path.GetFileNameWithoutExtension(header.Name);
                    var typeMatch = Regex.Match(headerText, @"\b(UCLASS|UINTERFACE|USTRUCT|UENUM)\b");
                    var type = FindSourceType(typeMatch.Value);
                    var from = $"{originModule}.{fileName}";
                    var to = $"{moduleName}.{fileName}";
                    detectedMigrations.Add(new Tuple<string,string>(from, to),type);
                    var replaced = headerText.Replace(match.Value, moduleName.ToUpperInvariant() + "_API");
                    File.WriteAllText(header.FullName, replaced);
                    // add redirect
                    string redirectText;
                    switch (type)
                    {
                        case SourceType.Class:
                            redirectText = $"+ClassRedirects=(OldName=\"/Script/{from}\",NewName=\"/Script/{to}\")";
                            break;
                        case SourceType.Struct:
                            redirectText = $"+StructRedirects=(OldName=\"/Script/{from}\",NewName=\"/Script/{to}\")";
                            break;
                        case SourceType.Enum:
                            redirectText = $"+EnumRedirects=(OldName=\"/Script/{from}\",NewName=\"/Script/{to}\")";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (!string.IsNullOrEmpty(redirectText))
                    {
                        redirects.AppendLine(redirectText);
                    }
                }
            }

            foreach (var module in moduleDirs)
            {
                var moduleName = module.Name;
                DetectMigrationsInDirectory(moduleName, module);
                DetectMigrationsInDirectory(moduleName, new DirectoryInfo(Path.Combine(module.FullName, "Public")));
                DetectMigrationsInDirectory(moduleName, new DirectoryInfo(Path.Combine(module.FullName, "Private")));
            }

            File.AppendAllText(iniFile.FullName, redirects.ToString());
            
            foreach (var detectedMigration in detectedMigrations)
            {
                Console.WriteLine($"[{detectedMigration.Value}] {detectedMigration.Key.Item1}  =>  {detectedMigration.Key.Item2}");
            }
        }
    }
}