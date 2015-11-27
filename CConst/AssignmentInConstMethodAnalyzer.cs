using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace FonsDijkstra.CConst
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AssignmentInConstMethodAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CConst1";
        internal static readonly LocalizableString Title = "Assignment in const declared method.";
        internal static readonly LocalizableString MessageFormat = "Const declared method {0} may not have side-effects.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, ConstnessHelper.Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
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

        void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            AnalyzeAssignment(context, (AssignmentExpressionSyntax)context.Node);
        }

        class Walker : CSharpSyntaxWalker
        {
            public readonly HashSet<string> UnsafeLocals = new HashSet<string>();

            readonly SyntaxNodeAnalysisContext context;

            public Walker(SyntaxNodeAnalysisContext context)
            {
                this.context = context;
            }

            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(node);
                if (symbol?.Kind == SymbolKind.Local) {
                    if (!(node.Initializer.Value is ObjectCreationExpressionSyntax || node.Identifier.Value is DefaultExpressionSyntax)) {
                        UnsafeLocals.Add(node.Identifier.Text);
                    }
                }
            }
        }

        void AnalyzeAssignment(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignment)
        {
            var containingMethod = assignment.GetContainingMethod();
            if (containingMethod.HasConstAttribute(context.SemanticModel)) {
                if (context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol?.Kind == SymbolKind.Local) {
                    return;
                }

                var left = assignment.Left as MemberAccessExpressionSyntax;
                if (left != null) {
                    var walker = new Walker(context);
                    containingMethod.Accept(walker);
                    var exprSymbol = context.SemanticModel.GetSymbolInfo(left.Expression).Symbol;
                    if (exprSymbol != null && exprSymbol.Kind == SymbolKind.Local && !walker.UnsafeLocals.Contains(exprSymbol.Name)) {
                        return;
                    }
                }
                context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.GetLocation(), context.SemanticModel.GetDeclaredSymbol(containingMethod)?.Name));
            }
        }
    }
}