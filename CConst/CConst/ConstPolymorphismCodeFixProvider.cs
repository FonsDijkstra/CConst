using System;
using System.Composition;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace FonsDijkstra.CConst
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstPolymorphismCodeFixProvider)), Shared]
    public class ConstPolymorphismCodeFixProvider : CodeFixProvider
    {
        public const string DiagnosticId = ConstPolymorphismAnalyzer.DiagnosticId;

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticId); }
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
                        DiagnosticId + "_add"),
                    context.Diagnostics.Single());
            }
        }
    }
}