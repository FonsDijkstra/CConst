using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace FonsDijkstra.CConst
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConstPolymorphismAnalyzer : DiagnosticAnalyzer
    {
        public const string OverrideDiagnosticId = "Const51";
        public const string ExplicitInterfaceDiagnosticId = "Const52";
        public const string InterfaceDiagnosticId = "Const53";

        static readonly LocalizableString OverrideTitle = "Constness override polymorphism";
        static readonly LocalizableString OverrideMessageFormat = "Overridden method {0} is declared const";
        static readonly DiagnosticDescriptor OverrideRule = new DiagnosticDescriptor(OverrideDiagnosticId, OverrideTitle, OverrideMessageFormat, ConstnessHelper.Category, DiagnosticSeverity.Warning, true);

        static readonly LocalizableString InterfaceTitle = "Constness interface implementation polymorphism";
        static readonly LocalizableString InterfaceMessageFormat = "Implemented interface method {0} is declared const";
        static readonly DiagnosticDescriptor InterfaceRule = new DiagnosticDescriptor(InterfaceDiagnosticId, InterfaceTitle, InterfaceMessageFormat, ConstnessHelper.Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(OverrideRule, InterfaceRule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            AnalyzeMethodDeclaration(context, context.Node as MethodDeclarationSyntax);
        }

        void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
        {
            if (!methodDeclaration.HasConstAttribute(context.SemanticModel))
            {
                AnalyzeOverrideMethod(context, methodDeclaration);
                AnalyzeInterfaceImplementation(context, methodDeclaration);
            }
        }

        void AnalyzeInterfaceImplementation(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
        {
            var interfaceMethods = methodDeclaration.GetImplementedInterfaceMethods(context.SemanticModel)
                .Where(interfaceMethod => interfaceMethod.Syntax.HasConstAttribute(interfaceMethod.Model))
                .ToArray();
            if (interfaceMethods.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(InterfaceRule, methodDeclaration.Identifier.GetLocation(), interfaceMethods.First().Model.GetDeclaredSymbol(interfaceMethods.First().Syntax).ToDisplayString()));
            }
        }

        static void AnalyzeOverrideMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
        {
            var overridenMethod = methodDeclaration.GetOverriddenMethod(context.SemanticModel);
            if (overridenMethod.Syntax.HasConstAttribute(overridenMethod.Model))
            {
                context.ReportDiagnostic(Diagnostic.Create(OverrideRule, methodDeclaration.Identifier.GetLocation(), overridenMethod.Model.GetDeclaredSymbol(overridenMethod.Syntax).ToDisplayString()));
            }
        }
    }
}