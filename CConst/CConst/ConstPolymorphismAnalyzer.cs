using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FonsDijkstra.CConst
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConstPolymorphismAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "Const5";
        internal static readonly LocalizableString Title = "Constness polymorphism";
        internal static readonly LocalizableString MessageFormat = "Overridden method {0} is declared const";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, ConstnessHelper.Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            AnalyzeMethodDeclaration(context, context.Node as MethodDeclarationSyntax);
        }

        private void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
        {
            if (!methodDeclaration.HasConstAttribute(context.SemanticModel))
            {
                SemanticModel model;
                var overridenMethod = methodDeclaration.GetOverriddenMethod(context.SemanticModel, out model);
                if (overridenMethod.HasConstAttribute(model))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), model.GetDeclaredSymbol(overridenMethod).ToDisplayString()));
                }
            }
        }
    }
}