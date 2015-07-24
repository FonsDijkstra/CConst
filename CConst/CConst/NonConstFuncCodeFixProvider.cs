using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace FonsDijkstra.CConst
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonConstFuncCodeFixProvider)), Shared]
    public class NonConstFuncCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(NonConstFuncAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            var method = root.FindNode(context.Span) as MethodDeclarationSyntax;
            if (method != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Add const declaration",
                        c => method.AddConstAttributeAsync(context.Document, c),
                        NonConstFuncAnalyzer.DiagnosticId + "_add"),
                    context.Diagnostics.Single());
            }
        }
    }
}