using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System;
using System.Linq;

namespace FonsDijkstra.CConst
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConstPolymorphismAnalyzer : DiagnosticAnalyzer
    {
        public const string OverrideDiagnosticId = "Const51";
        public const string ExplicitInterfaceDiagnosticId = "Const52";

        static readonly LocalizableString OverrideTitle = "Constness override polymorphism";
        static readonly LocalizableString OverrideMessageFormat = "Overridden method {0} is declared const";
        static DiagnosticDescriptor OverrideRule = new DiagnosticDescriptor(OverrideDiagnosticId, OverrideTitle, OverrideMessageFormat, ConstnessHelper.Category, DiagnosticSeverity.Warning, true);

        static readonly LocalizableString ExplicitInterfaceTitle = "Constness explicit interface implementation polymorphism";
        static readonly LocalizableString ExplicitInterfaceMessageFormat = "Explicitely implemented interface method {0} is declared const";
        static DiagnosticDescriptor ExplicitInterfaceRule = new DiagnosticDescriptor(ExplicitInterfaceDiagnosticId, ExplicitInterfaceTitle, ExplicitInterfaceMessageFormat, ConstnessHelper.Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(OverrideRule, ExplicitInterfaceRule);
            }
        }

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
            var interfaceMethods = methodDeclaration.GetExplicitlyImplementedInterfaceMethods(context.SemanticModel)
                .Where(eii => eii.Syntax.HasConstAttribute(eii.Model));
            if (interfaceMethods.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(ExplicitInterfaceRule, methodDeclaration.Identifier.GetLocation(), interfaceMethods.First().Model.GetDeclaredSymbol(interfaceMethods.First().Syntax).ToDisplayString()));
            }
        }

        private static void AnalyzeOverrideMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
        {
            var overridenMethod = methodDeclaration.GetOverriddenMethod(context.SemanticModel);
            if (overridenMethod.Syntax.HasConstAttribute(overridenMethod.Model))
            {
                context.ReportDiagnostic(Diagnostic.Create(OverrideRule, methodDeclaration.Identifier.GetLocation(), overridenMethod.Model.GetDeclaredSymbol(overridenMethod.Syntax).ToDisplayString()));
            }
        }
    }
}