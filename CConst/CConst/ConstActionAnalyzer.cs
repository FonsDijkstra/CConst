using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace FonsDijkstra.CConst
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConstActionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CConst4";
        internal static readonly LocalizableString Title = "Const action";
        internal static readonly LocalizableString MessageFormat = "Action {0} is declared const";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, ConstnessHelper.Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            AnalyzeMethod(context, (MethodDeclarationSyntax)context.Node);
        }

        void AnalyzeMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(method);
            if (symbol.ReturnsVoid && method.HasConstAttribute(context.SemanticModel))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, method.Identifier.GetLocation(), symbol.Name));
            }
        }
    }
}
