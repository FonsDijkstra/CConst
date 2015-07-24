using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FonsDijkstra.CConst
{
    struct SyntaxModelPair<TSyntax>
        where TSyntax : SyntaxNode
    {
        public SyntaxModelPair(TSyntax syntax, SemanticModel model)
            : this()
        {
            Syntax = syntax;
            Model = model;
        }

        public TSyntax Syntax { get; }
        public SemanticModel Model { get; }
    }

    static class ConstnessHelper
    {
        public const string Category = "Constness";

        public static MethodDeclarationSyntax GetContainingMethod(this SyntaxNode node)
        {
            return node?.AncestorsAndSelf()?.FirstOrDefault(ancestor => ancestor.Kind() == SyntaxKind.MethodDeclaration) as MethodDeclarationSyntax;
        }

        public static SyntaxModelPair<MethodDeclarationSyntax> GetInvokedMethod(this InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null)
            {
                return new SyntaxModelPair<MethodDeclarationSyntax>();
            }

            var syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            var method = syntax?.GetSyntax() as MethodDeclarationSyntax;
            return new SyntaxModelPair<MethodDeclarationSyntax>(method, model.Compilation.GetSemanticModel(syntax?.SyntaxTree));
        }

        public static MethodDeclarationSyntax GetOverriddenMethod(this MethodDeclarationSyntax declaration, SemanticModel model, out SemanticModel overriddemMethodModel)
        {
            var symbol = model.GetDeclaredSymbol(declaration) as IMethodSymbol;
            if (symbol.OverriddenMethod == null)
            {
                overriddemMethodModel = null;
                return null;
            }

            var syntax = symbol.OverriddenMethod.DeclaringSyntaxReferences.FirstOrDefault();
            overriddemMethodModel = model.Compilation.GetSemanticModel(syntax?.SyntaxTree);
            return syntax?.GetSyntax() as MethodDeclarationSyntax;
        }

        public static AttributeListSyntax GetConstAttributeList(this MethodDeclarationSyntax methodDeclaration, SemanticModel model)
        {
            if (methodDeclaration == null)
            {
                return null;
            }

            return methodDeclaration.AttributeLists.Single(al => al.HasConstAttribute(model));
        }

        public static AttributeSyntax GetConstAttribute(this AttributeListSyntax attributeList, SemanticModel model)
        {
            return attributeList.Attributes.SingleOrDefault(a => a.IsConstAttribute(model));
        }

        public static bool HasConstAttribute(this MethodDeclarationSyntax methodDeclaration, SemanticModel model)
        {
            if (methodDeclaration == null)
            {
                return false;
            }

            return methodDeclaration.AttributeLists.Any(al => al.HasConstAttribute(model));
        }

        public static bool HasConstAttribute(this AttributeListSyntax attributeList, SemanticModel model)
        {
            return attributeList.Attributes.Any(a => a.IsConstAttribute(model));
        }

        public static bool IsConstAttribute(this AttributeSyntax attribute, SemanticModel model)
        {
            var symbol = model.GetSymbolInfo(attribute).Symbol;
            return symbol?.ContainingAssembly?.Name == typeof(ConstAttribute).Namespace &&
                symbol?.ContainingType?.Name == typeof(ConstAttribute).Name;
        }

        public static async Task<Document> RemoveConstAttributeAsync(this MethodDeclarationSyntax method, Document document, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken);
            var attributeList = method.GetConstAttributeList(model);
            var attribute = attributeList.GetConstAttribute(model);
            var newAttributeList = attributeList.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia);
            var newAttributeLists = newAttributeList.Attributes.Any()
                ? method.AttributeLists.Replace(attributeList, newAttributeList)
                : method.AttributeLists.Remove(attributeList);

            return await method.WithNewAttributeLists(newAttributeLists, document, cancellationToken);
        }

        public static async Task<Document> AddConstAttributeAsync(this MethodDeclarationSyntax method, Document document, CancellationToken cancellationToken)
        {
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Const"));
            var attributeList = method.AttributeLists.Count == 1
                ? method.AttributeLists.Single()
                : SyntaxFactory.AttributeList();
            var newAttributeList = SyntaxFactory.AttributeList(attributeList.Attributes.Add(attribute));
            var newAttributeLists = method.AttributeLists.Count == 1
                ? method.AttributeLists.Replace(attributeList, newAttributeList)
                : method.AttributeLists.Add(newAttributeList);

            var newDocument = await method.WithNewAttributeLists(newAttributeLists, document, cancellationToken);
            return await EnsureUsingDirective(newDocument, cancellationToken);
        }

        static async Task<Document> WithNewAttributeLists(this MethodDeclarationSyntax method, SyntaxList<AttributeListSyntax> newAttributeLists, Document document, CancellationToken cancellationToken)
        {
            var newMethod = method.WithAttributeLists(newAttributeLists);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(method, newMethod);
            return document.WithSyntaxRoot(newRoot);
        }

        static async Task<Document> EnsureUsingDirective(Document document, CancellationToken cancellationToken)
        {
            var usingName = SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("FonsDijkstra"), SyntaxFactory.IdentifierName("CConst"));
            var usingDirective = SyntaxFactory.UsingDirective(usingName);

            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            var unit = tree.GetCompilationUnitRoot(cancellationToken);
            if (unit.Usings.Any(ud => ud.IsEquivalentTo(usingDirective, true)))
            {
                return document;
            }
            var newUnit = unit.AddUsings(usingDirective);
            var newRoot = await newUnit.SyntaxTree.GetRootAsync(cancellationToken);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
