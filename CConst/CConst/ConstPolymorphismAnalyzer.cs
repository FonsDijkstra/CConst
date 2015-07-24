using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System;

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
                AnalyzeOverrideMethod(context, methodDeclaration);
                AnalyzeExplicitInterfaceImplementation(context, methodDeclaration);
            }
        }

        private void AnalyzeExplicitInterfaceImplementation(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
        }

        private static void AnalyzeOverrideMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
        {
            var overridenMethod = methodDeclaration.GetOverriddenMethod(context.SemanticModel);
            if (overridenMethod.Syntax.HasConstAttribute(overridenMethod.Model))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), overridenMethod.Model.GetDeclaredSymbol(overridenMethod.Syntax).ToDisplayString()));
            }
        }
    }
}