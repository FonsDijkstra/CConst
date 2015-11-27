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
    public class ConstConstructorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CConst9";
        internal static readonly LocalizableString Title = "Const constructor";
        internal static readonly LocalizableString MessageFormat = "Constructor {0} may not have side-effects.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, ConstnessHelper.Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeAssignment,
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.AddAssignmentExpression,
                SyntaxKind.AndAssignmentExpression,
                SyntaxKind.DivideAssignmentExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression,
                SyntaxKind.LeftShiftAssignmentExpression,
                SyntaxKind.ModuloAssignmentExpression,
                SyntaxKind.MultiplyAssignmentExpression,
                SyntaxKind.OrAssignmentExpression,
                SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression);
        }

        private void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            AnalyzeConstructor(context, context.Node as ConstructorDeclarationSyntax);
        }

        private void AnalyzeConstructor(SyntaxNodeAnalysisContext context, ConstructorDeclarationSyntax constructor)
        {
            constructor.Accept(new Visitor(context, constructor));
        }

        private class Visitor : CSharpSyntaxWalker
        {
            readonly SyntaxNodeAnalysisContext context;
            readonly ConstructorDeclarationSyntax constructor;
            readonly IMethodSymbol constructorSymbol;

            public Visitor(SyntaxNodeAnalysisContext context, ConstructorDeclarationSyntax constructor)
            {
                this.context = context;
                this.constructor = constructor;
                constructorSymbol = context.SemanticModel.GetDeclaredSymbol(constructor);
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax assignment)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
                if (symbol == null) {
                    return;
                }

                if (symbol.Kind == SymbolKind.Local || 
                    (symbol.Kind == SymbolKind.Parameter && ((IParameterSymbol)symbol).RefKind != RefKind.Ref)) {
                    return;
                }

                if ((symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Property) &&
                    symbol.ContainedIn(constructorSymbol.ContainingType) &&
                    symbol.IsStatic == constructorSymbol.IsStatic) {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.GetLocation(), constructorSymbol.Name));
            }
        }

        void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            //AnalyzeAssignment(context, (AssignmentExpressionSyntax)context.Node);
        }

        void AnalyzeAssignment(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignment)
        {
            var constructor = assignment.GetContainingConstructor();
            if (constructor == null)
            {
                return;
            }

            var symbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
            if (symbol?.Kind == SymbolKind.Local)
            {
                return;
            }

            var constructorSymbol = context.SemanticModel.GetDeclaredSymbol(constructor);
            if (symbol?.ContainingType == constructorSymbol.ContainingType)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.GetLocation(), constructorSymbol.Name));
        }
    }
}