using System.Text;
using System.Text.RegularExpressions;

namespace UnrealCppMigrator
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var sourceRoot = args.Length < 1 ? Directory.GetCurrentDirectory() : args[0];
            VerifyContextRoot(sourceRoot, out var sourceDir, out var iniFile);
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
            
            var detectedMigrations = new List<CppMigrationRecord>();

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
                    var typeMatch = Regex.Match(headerText, @"\b(UCLASS|UINTERFACE|USTRUCT|UENUM)\b");
                    var record = new CppMigrationRecord()
                    {
                        destinationModule = moduleName,
                        sourceModule = apiMacros2Module[match.Value],
                        sourceName = Path.GetFileNameWithoutExtension(header.Name),
                        sourceType = Utils.FindSourceType(typeMatch.Value)
                    };
                    detectedMigrations.Add(record);
                    var replaced = headerText.Replace(match.Value, moduleName.ToUpperInvariant() + "_API");
                    File.WriteAllText(header.FullName, replaced);
                    // add redirect
                    redirects.AppendLine(record.GenerateCoreRedirect());
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
                Console.WriteLine(detectedMigration.ToString());
            }
        }

        public static void VerifyContextRoot(string contextRoot, out DirectoryInfo sourceDir, out FileInfo iniFile)
        {
            var projectDir = new DirectoryInfo(contextRoot);
            if (!projectDir.Exists)
            {
                throw new InvalidOperationException(
                    "The specified directory does not exist. Please check your command line argument.");
            }

            sourceDir = projectDir.GetChildDirectory("Source");
            if (!sourceDir.Exists)
            {
                throw new InvalidOperationException(
                    "The specified directory does not contain a Source folder, check your command line argument or where this program is placed if you have specified none.");
            }

            iniFile = projectDir.GetChildFile("Config/DefaultEngine.ini"); 
            if (!iniFile.Exists)
            {
                throw new FileNotFoundException($"Cannot find DefaultEngine.ini at {iniFile.FullName}");
            }
        }
    }
}