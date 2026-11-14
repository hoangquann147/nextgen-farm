using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class SyntaxCheck
{
    public static int Main(string[] args)
    {
        int count = 0, errors = 0;
        foreach (string file in Directory.GetFiles(args[0], "*.cs", SearchOption.AllDirectories))
        {
            foreach (string[] symbols in new[] { new string[0], new[] { "UNITY_EDITOR", "ENABLE_INPUT_SYSTEM" },
                new[] { "UNITY_EDITOR", "ENABLE_LEGACY_INPUT_MANAGER" } })
            {
                var options = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: symbols);
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), options, file);
                foreach (var diagnostic in tree.GetDiagnostics())
                    if (diagnostic.Severity == DiagnosticSeverity.Error) { Console.WriteLine(diagnostic); errors++; }
            }
            count++;
        }
        Console.WriteLine(count + " C# files parsed in 3 preprocessor configurations; " + errors + " syntax errors. Unity type/API checking requires Unity Editor.");
        return errors == 0 ? 0 : 1;
    }
}
