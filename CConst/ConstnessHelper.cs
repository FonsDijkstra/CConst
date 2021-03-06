﻿using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
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

        public static bool ContainedIn(this ISymbol symbol, INamedTypeSymbol type)
        {
            return symbol.IsStatic ? symbol.ContainingType == type : type.BaseTypes().Contains(symbol.ContainingType);
        }

        static IEnumerable<INamedTypeSymbol> BaseTypes(this INamedTypeSymbol type)
        {
            return type == null ? new INamedTypeSymbol[0] : BaseTypes(type.BaseType).Concat(new[] { type });
        }

        public static MethodDeclarationSyntax GetContainingMethod(this SyntaxNode node)
        {
            return node?.AncestorsAndSelf()?.FirstOrDefault(ancestor => ancestor.Kind() == SyntaxKind.MethodDeclaration) as MethodDeclarationSyntax;
        }

        public static ConstructorDeclarationSyntax GetContainingConstructor(this SyntaxNode node)
        {
            return node?.AncestorsAndSelf()?.FirstOrDefault(ancestor => ancestor.Kind() == SyntaxKind.ConstructorDeclaration) as ConstructorDeclarationSyntax;
        }

        public static SyntaxModelPair<MethodDeclarationSyntax> GetInvokedMethod(this InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol == null)
            {
                return new SyntaxModelPair<MethodDeclarationSyntax>();
            }

            return GetMethodDeclaration(symbol, model);
        }

        public static SyntaxModelPair<MethodDeclarationSyntax> GetOverriddenMethod(this MethodDeclarationSyntax declaration, SemanticModel model)
        {
            var symbol = model.GetDeclaredSymbol(declaration) as IMethodSymbol;
            if (symbol.OverriddenMethod == null)
            {
                return new SyntaxModelPair<MethodDeclarationSyntax>();
            }

            return GetMethodDeclaration(symbol.OverriddenMethod, model);
        }

        public static IEnumerable<SyntaxModelPair<MethodDeclarationSyntax>> GetImplementedInterfaceMethods(this MethodDeclarationSyntax declaration, SemanticModel model)
        {
            var symbol = model.GetDeclaredSymbol(declaration) as IMethodSymbol;
            var type = symbol.ContainingType;
            return type
                .AllInterfaces
                .SelectMany(@interface => @interface.GetMembers())
                .Select(interfaceMember => new {
                    InterfaceMember = interfaceMember,
                    Implementation = type.FindImplementationForInterfaceMember(interfaceMember)
                })
                .Where(interfaceMemberImplementation => interfaceMemberImplementation.Implementation.Equals(symbol))
                .Select(interfaceMemberImplementation => interfaceMemberImplementation.InterfaceMember as IMethodSymbol)
                .Select(interfaceMethod => GetMethodDeclaration(interfaceMethod, model))
                .ToArray();
        }

        private static SyntaxModelPair<MethodDeclarationSyntax> GetMethodDeclaration(IMethodSymbol symbol, SemanticModel model)
        {
            var syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            var method = syntax?.GetSyntax() as MethodDeclarationSyntax;
            return new SyntaxModelPair<MethodDeclarationSyntax>(method, model.Compilation.GetSemanticModel(syntax?.SyntaxTree));
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
