using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using QuickClassMap.Domain;

namespace QuickClassMap.Roslyn
{
    internal class RoslynRelationshipParser
    {
        private readonly Compilation _compilation;
        private readonly Dictionary<INamedTypeSymbol, ClassInfo> _symbolToClassInfoMap;
        private readonly Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>> _inheritanceHierarchy;

        public RoslynRelationshipParser(Compilation compilation, Dictionary<INamedTypeSymbol, ClassInfo> symbolToClassInfoMap)
        {
            _compilation = compilation;
            _symbolToClassInfoMap = symbolToClassInfoMap;
            _inheritanceHierarchy = new Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
        }

        public void ProcessRelationships()
        {
            BuildInheritanceHierarchy();

            foreach (var classSymbol in _symbolToClassInfoMap.Keys)
            {
                var classInfo = _symbolToClassInfoMap[classSymbol];
                ExtractRelationships(classSymbol, classInfo);
            }
        }

        private void BuildInheritanceHierarchy()
        {
            foreach (var classSymbol in _symbolToClassInfoMap.Keys)
            {
                var hierarchy = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                var currentType = classSymbol;
                while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
                {
                    hierarchy.Add(currentType);
                    currentType = currentType.BaseType;
                }
                _inheritanceHierarchy[classSymbol] = hierarchy;
            }
        }

        private void ExtractRelationships(INamedTypeSymbol classSymbol, ClassInfo classInfo)
        {
            if (classSymbol.BaseType != null && classSymbol.BaseType.SpecialType != SpecialType.System_Object)
            {
                AddRelationship(classInfo, classSymbol.BaseType, RelationshipType.Inherits);
            }

            foreach (var @interface in classSymbol.Interfaces)
            {
                AddRelationship(classInfo, @interface, RelationshipType.Implements);
            }

            foreach (var member in classSymbol.GetMembers())
            {
                ProcessMember(classInfo, member, classSymbol);
            }
        }

        private void ProcessMember(ClassInfo classInfo, ISymbol member, INamedTypeSymbol containingType)
        {
            if (member is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.MethodKind == MethodKind.Constructor)
                {
                    ProcessConstructor(classInfo, methodSymbol);
                }
                else
                {
                    ProcessMethod(classInfo, methodSymbol, containingType);
                }
            }
            else
            {
                ITypeSymbol typeSymbol = GetTypeSymbol(member);
                if (typeSymbol != null && !SymbolEqualityComparer.Default.Equals(typeSymbol, containingType))
                {
                    RelationshipType relationshipType = DetermineRelationshipType(member, typeSymbol, containingType);
                    AddRelationship(classInfo, typeSymbol, relationshipType);
                }
            }
        }

        private void ProcessConstructor(ClassInfo classInfo, IMethodSymbol constructor)
        {
            foreach (var parameter in constructor.Parameters)
            {
                AddRelationship(classInfo, parameter.Type, RelationshipType.Aggregates);
            }

            ProcessMethodBody(classInfo, constructor, constructor.ContainingType);
        }

        private void ProcessMethod(ClassInfo classInfo, IMethodSymbol method, INamedTypeSymbol containingType)
        {
            foreach (var parameter in method.Parameters)
            {
                AddRelationship(classInfo, parameter.Type, RelationshipType.Uses);
            }

            if (method.ReturnType != null && !SymbolEqualityComparer.Default.Equals(method.ReturnType, containingType))
            {
                AddRelationship(classInfo, method.ReturnType, RelationshipType.Uses);
            }

            ProcessMethodBody(classInfo, method, containingType);
        }

        private void ProcessMethodBody(ClassInfo classInfo, IMethodSymbol method, INamedTypeSymbol containingType)
        {
            var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            if (syntax is MethodDeclarationSyntax methodSyntax)
            {
                var semanticModel = _compilation.GetSemanticModel(methodSyntax.SyntaxTree);
                var descendantNodes = methodSyntax.DescendantNodes();

                foreach (var node in descendantNodes)
                {
                    if (node is InvocationExpressionSyntax invocation)
                    {
                        if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol invokedMethod)
                        {
                            if (invokedMethod.ContainingType != null &&
                                !SymbolEqualityComparer.Default.Equals(invokedMethod.ContainingType, containingType) &&
                                !IsInInheritanceHierarchy(containingType, invokedMethod.ContainingType))
                            {
                                AddRelationship(classInfo, invokedMethod.ContainingType, RelationshipType.Uses);
                            }
                        }

                        // Handle generic types of extension methods defined in other assemblies.
                        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                        {
                            if (memberAccess.Name is GenericNameSyntax genericName)
                            {
                                foreach (var typeArgSyntax in genericName.TypeArgumentList.Arguments)
                                {
                                    if (semanticModel.GetTypeInfo(typeArgSyntax).Type is INamedTypeSymbol typeSymbol)
                                    {
                                        AddRelationship(classInfo, typeSymbol, RelationshipType.Uses);
                                    }
                                }
                            }
                        }
                    }
                    else if (node is ObjectCreationExpressionSyntax objectCreation)
                    {
                        if (semanticModel.GetTypeInfo(objectCreation).Type is INamedTypeSymbol createdType &&
                            !SymbolEqualityComparer.Default.Equals(createdType, containingType) &&
                            !IsInInheritanceHierarchy(containingType, createdType) &&
                            !method.IsStatic)
                        {
                            AddRelationship(classInfo, createdType, RelationshipType.Composes);
                        }
                    }
                    else if (node is CastExpressionSyntax castExpression)
                    {
                        ProcessTypeConversion(classInfo, castExpression.Type, semanticModel, containingType);
                    }
                    else if (node is BinaryExpressionSyntax binaryExpression &&
                        (binaryExpression.OperatorToken.IsKind(SyntaxKind.AsKeyword) ||
                        binaryExpression.OperatorToken.IsKind(SyntaxKind.IsKeyword)))
                    {
                        ProcessTypeConversion(classInfo, binaryExpression.Right, semanticModel, containingType);
                    }
                    else if (node is SimpleLambdaExpressionSyntax simpleLambda)
                    {
                        var parameterSymbol = semanticModel.GetDeclaredSymbol(simpleLambda.Parameter);
                        if (parameterSymbol != null)
                        {
                            AddRelationship(classInfo, parameterSymbol.Type, RelationshipType.Uses);
                        }
                    }
                    else if (node is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
                    {
                        foreach (var parameter in parenthesizedLambda.ParameterList.Parameters)
                        {
                            var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter);
                            if (parameterSymbol != null)
                            {
                                AddRelationship(classInfo, parameterSymbol.Type, RelationshipType.Uses);
                            }
                        }
                    }
                }
            }
        }

        private bool IsInInheritanceHierarchy(INamedTypeSymbol derivedType, INamedTypeSymbol potentialBaseType)
        {
            if (_inheritanceHierarchy.TryGetValue(derivedType, out var hierarchy))
            {
                return hierarchy.Contains(potentialBaseType);
            }
            return false;
        }

        private void ProcessTypeConversion(ClassInfo classInfo, SyntaxNode typeNode, SemanticModel semanticModel, INamedTypeSymbol containingType)
        {
            var convertedType = semanticModel.GetTypeInfo(typeNode).Type;
            if (convertedType is INamedTypeSymbol namedType &&
                !SymbolEqualityComparer.Default.Equals(namedType, containingType) &&
                !IsInInheritanceHierarchy(containingType, namedType))
            {
                AddRelationship(classInfo, namedType, RelationshipType.Uses);
            }
        }

        private ITypeSymbol GetTypeSymbol(ISymbol symbol)
        {
            if (symbol is IFieldSymbol field)
                return field.Type;
            if (symbol is IPropertySymbol property)
                return property.Type;
            if (symbol is IParameterSymbol parameter)
                return parameter.Type;
            if (symbol is IMethodSymbol method)
                return method.ReturnType;
            return null;
        }

        private RelationshipType DetermineRelationshipType(ISymbol symbol, ITypeSymbol typeSymbol, INamedTypeSymbol containingType)
        {
            // Value types and strings are always composed
            if (typeSymbol.IsValueType || typeSymbol.SpecialType == SpecialType.System_String)
                return RelationshipType.Composes;

            // Collections are typically aggregated
            if (IsCollectionType(typeSymbol))
                return RelationshipType.Aggregates;

            // Parameters are always aggregated
            if (symbol is IParameterSymbol)
                return RelationshipType.Aggregates;

            // Check if the symbol is a property or field that matches a constructor parameter
            if (symbol is IPropertySymbol || symbol is IFieldSymbol)
            {
                if (IsConstructorParameter(typeSymbol, containingType))
                {
                    return RelationshipType.Aggregates;
                }
            }

            // For other cases, use accessibility to determine the relationship
            return symbol.DeclaredAccessibility == Accessibility.Private ? RelationshipType.Composes : RelationshipType.Aggregates;
        }

        private bool IsCollectionType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol namedType)
            {
                var enumerableType = _compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                return _compilation.ClassifyConversion(namedType, enumerableType).IsImplicit;
            }
            return false;
        }

        private bool IsConstructorParameter(ITypeSymbol typeSymbol, INamedTypeSymbol containingType)
        {
            foreach (var constructor in containingType.Constructors)
            {
                if (constructor.Parameters.Any(p => SymbolEqualityComparer.Default.Equals(p.Type, typeSymbol)))
                {
                    return true;
                }
            }
            return false;
        }

        private void AddRelationship(ClassInfo classInfo, ITypeSymbol relatedType, RelationshipType relationshipType)
        {
            // Determine the user-defined type
            INamedTypeSymbol typeToAdd = null;
            if (relatedType.SpecialType == SpecialType.None &&
                relatedType is INamedTypeSymbol namedTypeSymbol &&
                (namedTypeSymbol.IsDefinition || namedTypeSymbol.IsGenericType))
            {
                if (namedTypeSymbol.IsGenericType)
                {
                    foreach (var typeArg in namedTypeSymbol.TypeArguments)
                    {
                        if (typeArg is INamedTypeSymbol namedTypeArg)
                        {
                            AddRelationship(classInfo, typeArg, relationshipType);
                        }
                    }
                }
                else
                {
                    // For other types, use the type as is
                    typeToAdd = namedTypeSymbol;
                }
            }

            // Add the relationship if the type is valid and exists in our class map
            if (typeToAdd != null && _symbolToClassInfoMap.ContainsKey(typeToAdd))
            {
                var relatedClassName = typeToAdd.ToDisplayString();

                // Prevent self-references
                if (relatedClassName == classInfo.FullName)
                {
                    return;
                }

                var existingRelationship = classInfo.Relationships.FirstOrDefault(r => r.RelatedClassName == relatedClassName);

                if (existingRelationship == null)
                {
                    classInfo.Relationships.Add(new RelationshipInfo
                    {
                        RelatedClassName = relatedClassName,
                        Type = relationshipType
                    });
                }
                else if (relationshipType < existingRelationship.Type)
                {
                    // If a relationship exists and the new type is stronger (lower enum value),
                    // update the existing relationship
                    existingRelationship.Type = relationshipType;
                }
            }
        }
    }
}
