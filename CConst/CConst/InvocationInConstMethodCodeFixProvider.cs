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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InvocationInConstMethodCodeFixProvider)), Shared]
    public class InvocationInConstMethodCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(InvocationInConstMethodAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken);

            var invocation = root.FindNode(context.Span) as InvocationExpressionSyntax;
            var invokedMethod = invocation.GetInvokedMethod(model);
            if (invokedMethod != null)
            {
                var document = context.Document.Project.Solution.GetDocument(invokedMethod.SyntaxTree);
                if (document != null)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create("Add const declaration", c => invokedMethod.AddConstAttributeAsync(document, c)),
                        context.Diagnostics.Single());
                }
            }
        }
    }
}