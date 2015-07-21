using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CConst
{
    static class ConstnessHelper
    {
        public const string Category = "Constness";

        public static MethodDeclarationSyntax GetContainingMethod(this SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            return node.AncestorsAndSelf().FirstOrDefault(ancestor => ancestor.Kind() == SyntaxKind.MethodDeclaration) as MethodDeclarationSyntax;
        }

        public static MethodDeclarationSyntax GetInvokedMethod(this InvocationExpressionSyntax invocation, SemanticModel model)
        {
            if (invocation == null)
            {
                return null;
            }

            var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null)
            {
                return null;
            }

            var syntax = symbol.DeclaringSyntaxReferences.SingleOrDefault()?.SyntaxTree;
            if (syntax == null)
            {
                return null;
            }

            return syntax.GetRoot().DescendantNodesAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .Where(md => model.GetDeclaredSymbol(md) == symbol)
                .SingleOrDefault();
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
            return symbol?.ContainingAssembly?.Name == "CConst" &&
                symbol?.ContainingType?.Name == "ConstAttribute";
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

            return await method.WithNewAttributeLists(newAttributeLists, document, cancellationToken);
        }

        static async Task<Document> WithNewAttributeLists(this MethodDeclarationSyntax method, SyntaxList<AttributeListSyntax> newAttributeLists, Document document, CancellationToken cancellationToken)
        {
            var newMethod = method.WithAttributeLists(newAttributeLists);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(method, newMethod);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
