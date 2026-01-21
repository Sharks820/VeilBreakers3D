using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static int Main(string[] args)
    {
        var projectRoot = @"C:\Users\Conner\OneDrive\Documents\VeilBreakers3DCurrent";
        var scriptsPath = Path.Combine(projectRoot, "Assets", "Scripts");
        var unityPath = @"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data\Managed\UnityEngine";

        // Find all C# files
        var csFiles = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);
        Console.WriteLine($"Compiling {csFiles.Length} C# files with Unity references...\n");

        // Parse all files
        var syntaxTrees = new List<SyntaxTree>();
        foreach (var file in csFiles)
        {
            var code = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(code, path: file);
            syntaxTrees.Add(tree);
        }

        // Add Unity references
        var references = new List<MetadataReference>();

        // Core .NET
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location));

        // Get runtime directory for additional references
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (File.Exists(Path.Combine(runtimeDir, "System.Runtime.dll")))
            references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")));
        if (File.Exists(Path.Combine(runtimeDir, "netstandard.dll")))
            references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "netstandard.dll")));

        // Unity assemblies
        var unityDlls = new[]
        {
            "UnityEngine.dll",
            "UnityEngine.CoreModule.dll",
            "UnityEngine.UIElementsModule.dll",
            "UnityEngine.IMGUIModule.dll",
            "UnityEngine.TextRenderingModule.dll",
            "UnityEngine.AudioModule.dll",
            "UnityEngine.AnimationModule.dll",
            "UnityEngine.PhysicsModule.dll",
            "UnityEngine.InputLegacyModule.dll",
            "UnityEngine.JSONSerializeModule.dll"
        };

        foreach (var dll in unityDlls)
        {
            var dllPath = Path.Combine(unityPath, dll);
            if (File.Exists(dllPath))
            {
                references.Add(MetadataReference.CreateFromFile(dllPath));
            }
        }

        // Compile
        var compilation = CSharpCompilation.Create(
            "VeilBreakers",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Disable)
        );

        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Console.WriteLine("========================================");
        Console.WriteLine("SEMANTIC COMPILATION RESULTS");
        Console.WriteLine("========================================\n");

        if (!diagnostics.Any())
        {
            Console.WriteLine($"SUCCESS: All {csFiles.Length} files compiled without errors!");
            return 0;
        }

        // Group by file
        var errorsByFile = diagnostics
            .Where(d => d.Location.SourceTree != null)
            .GroupBy(d => d.Location.SourceTree.FilePath)
            .OrderBy(g => g.Key);

        int totalErrors = 0;
        foreach (var group in errorsByFile)
        {
            var relativePath = group.Key.Replace(projectRoot + "\\", "");
            Console.WriteLine($"[ERROR] {relativePath}");

            foreach (var diag in group.Take(5)) // Limit to 5 errors per file
            {
                var line = diag.Location.GetLineSpan();
                Console.WriteLine($"  Line {line.StartLinePosition.Line + 1}: {diag.Id} - {diag.GetMessage()}");
                totalErrors++;
            }

            if (group.Count() > 5)
                Console.WriteLine($"  ... and {group.Count() - 5} more errors");

            Console.WriteLine();
        }

        Console.WriteLine($"FAILED: {diagnostics.Count} total errors");
        return 1;
    }
}
