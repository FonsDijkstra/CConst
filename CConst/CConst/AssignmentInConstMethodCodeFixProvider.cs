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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AssignmentInConstMethodCodeFixProvider)), Shared]
    public class AssignmentInConstMethodCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AssignmentInConstMethodAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            var assignment = root.FindNode(context.Span) as AssignmentExpressionSyntax;
            var containingMethod = assignment.GetContainingMethod();
            if (containingMethod != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create("Remove const declaration", c => containingMethod.RemoveConstAttributeAsync(context.Document, c)),
                    context.Diagnostics.Single());
            }
        }
    }
}