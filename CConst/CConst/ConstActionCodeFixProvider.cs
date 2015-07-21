using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace CConst
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstActionCodeFixProvider)), Shared]
    public class ConstActionCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ConstActionAnalyzer.DiagnosticId); }
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
                    CodeAction.Create("Remove const declaration", c => method.RemoveConstAttributeAsync(context.Document, c)),
                    context.Diagnostics.Single());
            }
        }
    }
}