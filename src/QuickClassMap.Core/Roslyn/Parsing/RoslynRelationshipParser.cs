using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using QuickClassMap.Core.Domain;

namespace QuickClassMap.Core.Roslyn.Parsing
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

            foreach (var classSymbol in _symbolToClassInfoMap.Keys.ToList())
            {
                var classInfo = _symbolToClassInfoMap[classSymbol];
                foreach (var relationship in ExtractSymbolRelationships(classSymbol))
                {
                    AddRelationship(classInfo, relationship.Target, relationship.Type);
                }
            }
        }

        public IReadOnlyCollection<SymbolRelationship> ExtractSymbolRelationships(INamedTypeSymbol classSymbol)
        {
            var relationships = new List<SymbolRelationship>();
            classSymbol = classSymbol.OriginalDefinition;
            BuildInheritanceHierarchy(classSymbol);
            ExtractRelationships(classSymbol, relationships);
            return relationships;
        }

        private void BuildInheritanceHierarchy()
        {
            foreach (var classSymbol in _symbolToClassInfoMap.Keys)
            {
                BuildInheritanceHierarchy(classSymbol);
            }
        }

        private void BuildInheritanceHierarchy(INamedTypeSymbol classSymbol)
        {
            classSymbol = classSymbol.OriginalDefinition;
            var hierarchy = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var currentType = classSymbol;
            while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
            {
                hierarchy.Add(currentType.OriginalDefinition);
                currentType = currentType.BaseType;
            }
            _inheritanceHierarchy[classSymbol] = hierarchy;
        }

        private void ExtractRelationships(INamedTypeSymbol classSymbol, ICollection<SymbolRelationship> relationships)
        {
            if (classSymbol.BaseType != null && classSymbol.BaseType.SpecialType != SpecialType.System_Object)
            {
                AddSymbolRelationship(relationships, classSymbol, classSymbol.BaseType, RelationshipType.Inherits);
            }

            foreach (var @interface in classSymbol.Interfaces)
            {
                AddSymbolRelationship(relationships, classSymbol, @interface, RelationshipType.Implements);
            }

            foreach (var member in classSymbol.GetMembers())
            {
                ProcessMember(relationships, member, classSymbol);
            }
        }

        private void ProcessMember(ICollection<SymbolRelationship> relationships, ISymbol member, INamedTypeSymbol containingType)
        {
            if (member is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.MethodKind == MethodKind.Constructor)
                {
                    ProcessConstructor(relationships, methodSymbol);
                }
                else
                {
                    ProcessMethod(relationships, methodSymbol, containingType);
                }
            }
            else
            {
                ITypeSymbol typeSymbol = GetTypeSymbol(member);
                if (typeSymbol != null && !SymbolEqualityComparer.Default.Equals(typeSymbol, containingType))
                {
                    RelationshipType relationshipType = DetermineRelationshipType(member, typeSymbol, containingType);
                    AddSymbolRelationship(relationships, containingType, typeSymbol, relationshipType);
                }
            }
        }

        private void ProcessConstructor(ICollection<SymbolRelationship> relationships, IMethodSymbol constructor)
        {
            foreach (var parameter in constructor.Parameters)
            {
                AddSymbolRelationship(relationships, constructor.ContainingType, parameter.Type, RelationshipType.Aggregates);
            }

            ProcessMethodBody(relationships, constructor, constructor.ContainingType);
        }

        private void ProcessMethod(ICollection<SymbolRelationship> relationships, IMethodSymbol method, INamedTypeSymbol containingType)
        {
            foreach (var parameter in method.Parameters)
            {
                AddSymbolRelationship(relationships, containingType, parameter.Type, RelationshipType.Uses);
            }

            if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, containingType))
            {
                AddSymbolRelationship(relationships, containingType, method.ReturnType, RelationshipType.Uses);
            }

            ProcessMethodBody(relationships, method, containingType);
        }

        private void ProcessMethodBody(ICollection<SymbolRelationship> relationships, IMethodSymbol method, INamedTypeSymbol containingType)
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
                                AddSymbolRelationship(relationships, containingType, invokedMethod.ContainingType, RelationshipType.Uses);
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
                                        AddSymbolRelationship(relationships, containingType, typeSymbol, RelationshipType.Uses);
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
                            AddSymbolRelationship(relationships, containingType, createdType, RelationshipType.Composes);
                        }
                    }
                    else if (node is CastExpressionSyntax castExpression)
                    {
                        ProcessTypeConversion(relationships, castExpression.Type, semanticModel, containingType);
                    }
                    else if (node is BinaryExpressionSyntax binaryExpression &&
                        (binaryExpression.OperatorToken.IsKind(SyntaxKind.AsKeyword) ||
                        binaryExpression.OperatorToken.IsKind(SyntaxKind.IsKeyword)))
                    {
                        ProcessTypeConversion(relationships, binaryExpression.Right, semanticModel, containingType);
                    }
                    else if (node is SimpleLambdaExpressionSyntax simpleLambda)
                    {
                        var parameterSymbol = semanticModel.GetDeclaredSymbol(simpleLambda.Parameter);
                        if (parameterSymbol != null)
                        {
                            AddSymbolRelationship(relationships, containingType, parameterSymbol.Type, RelationshipType.Uses);
                        }
                    }
                    else if (node is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
                    {
                        foreach (var parameter in parenthesizedLambda.ParameterList.Parameters)
                        {
                            var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter);
                            if (parameterSymbol != null)
                            {
                                AddSymbolRelationship(relationships, containingType, parameterSymbol.Type, RelationshipType.Uses);
                            }
                        }
                    }
                }
            }
        }

        private bool IsInInheritanceHierarchy(INamedTypeSymbol derivedType, INamedTypeSymbol potentialBaseType)
        {
            derivedType = derivedType.OriginalDefinition;
            potentialBaseType = potentialBaseType.OriginalDefinition;
            if (_inheritanceHierarchy.TryGetValue(derivedType, out var hierarchy))
            {
                return hierarchy.Contains(potentialBaseType);
            }
            return false;
        }

        private void ProcessTypeConversion(ICollection<SymbolRelationship> relationships, SyntaxNode typeNode, SemanticModel semanticModel, INamedTypeSymbol containingType)
        {
            var convertedType = semanticModel.GetTypeInfo(typeNode).Type;
            if (convertedType is INamedTypeSymbol namedType &&
                !SymbolEqualityComparer.Default.Equals(namedType, containingType) &&
                !IsInInheritanceHierarchy(containingType, namedType))
            {
                AddSymbolRelationship(relationships, containingType, namedType, RelationshipType.Uses);
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
            if (!(relatedType is INamedTypeSymbol namedTypeSymbol))
            {
                return;
            }

            if (relationshipType != RelationshipType.Inherits &&
                relationshipType != RelationshipType.Implements)
            {
                foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                {
                    AddRelationship(classInfo, typeArgument, relationshipType);
                }
            }

            if (!_symbolToClassInfoMap.TryGetValue(namedTypeSymbol.OriginalDefinition, out var relatedClassInfo) ||
                relatedClassInfo.FullName == classInfo.FullName)
            {
                return;
            }

            var relationship = new RelationshipInfo
            {
                RelatedClassName = relatedClassInfo.FullName,
                Type = relationshipType
            };

            if (!classInfo.Relationships.TryGetValue(relationship, out var existingRelationship))
            {
                classInfo.Relationships.Add(relationship);
            }
            else if (relationshipType < existingRelationship.Type)
            {
                // If a relationship exists and the new type is stronger (lower enum value),
                // update the existing relationship
                existingRelationship.Type = relationshipType;
            }
        }

        private void AddSymbolRelationship(
            ICollection<SymbolRelationship> relationships,
            INamedTypeSymbol source,
            ITypeSymbol relatedType,
            RelationshipType relationshipType)
        {
            if (!(relatedType is INamedTypeSymbol namedTypeSymbol))
            {
                return;
            }

            if (relationshipType != RelationshipType.Inherits &&
                relationshipType != RelationshipType.Implements)
            {
                foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                {
                    AddSymbolRelationship(relationships, source, typeArgument, relationshipType);
                }
            }

            var target = namedTypeSymbol.OriginalDefinition;
            source = source.OriginalDefinition;
            if (SymbolEqualityComparer.Default.Equals(target, source))
            {
                return;
            }

            relationships.Add(new SymbolRelationship(source, target, relationshipType));
        }
    }
}
