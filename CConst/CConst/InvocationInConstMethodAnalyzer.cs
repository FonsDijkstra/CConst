using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FonsDijkstra.CConst
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InvocationInConstMethodAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CConst2";
        internal static readonly LocalizableString Title = "Invocation in const declared method.";
        internal static readonly LocalizableString MessageFormat = "Const declared method {0} may not invoke non-const declared method {1}.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, ConstnessHelper.Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            AnalyzeInvocation(context, (InvocationExpressionSyntax)context.Node);
        }

        void AnalyzeInvocation(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
        {
            var containingMethod = invocation.GetContainingMethod();
            if (containingMethod.HasConstAttribute(context.SemanticModel))
            {
                var invokedMethod = invocation.GetInvokedMethod(context.SemanticModel);
                if (!invokedMethod.Syntax.HasConstAttribute(invokedMethod.Model))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule,
                        invocation.GetLocation(),
                        context.SemanticModel.GetDeclaredSymbol(containingMethod)?.Name,
                        context.SemanticModel.GetSymbolInfo(invocation).Symbol?.Name));
                }
            }
        }
    }
}