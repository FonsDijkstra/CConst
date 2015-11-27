using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FonsDijkstra.CConst;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CConst.Test2
{
    public static class DiagnosticHelper
    {
        public static Diagnostic[] GetDiagnostics(DiagnosticAnalyzer analyzer, string source)
        {
            var project = CreateProject(source);
            var compilation = project.GetCompilationAsync().Result;
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            return compilationWithAnalyzers.GetAllDiagnosticsAsync().Result.ToArray();
        }

        static Project CreateProject(string source)
        {
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            var projectId = ProjectId.CreateNewId();
            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")))
                .AddMetadataReference(projectId, MetadataReference.CreateFromFile(typeof(ConstAttribute).Assembly.Location));
            var documentId = DocumentId.CreateNewId(projectId);
            solution = solution.AddDocument(documentId, "Test.cs", SourceText.From(source));
            return solution.GetProject(projectId);
        }
    }
}
