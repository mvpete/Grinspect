using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Grinspector.Generator;

[Generator]
public class GrinspectorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Strategy: Find all [Grinspect(typeof(...))] attributes and generate for those types
        var targetTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is MethodDeclarationSyntax or ClassDeclarationSyntax,
                transform: static (ctx, _) => GetTargetTypesFromGrinspectAttribute(ctx))
            .Where(static types => types.Any())
            .SelectMany(static (types, _) => types)
            .Collect();

        context.RegisterSourceOutput(targetTypes, static (spc, types) => 
        {
            foreach (var targetType in types.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default))
            {
                if (targetType != null)
                {
                    GenerateForClass(targetType, spc);
                }
            }
        });
    }
    
    private static IEnumerable<INamedTypeSymbol> GetTargetTypesFromGrinspectAttribute(GeneratorSyntaxContext context)
    {
        ISymbol? symbol = context.Node switch
        {
            MethodDeclarationSyntax method => context.SemanticModel.GetDeclaredSymbol(method),
            ClassDeclarationSyntax cls => context.SemanticModel.GetDeclaredSymbol(cls),
            _ => null
        };
        
        if (symbol == null)
            yield break;
            
        // Find all [InternalsAvailable(typeof(...))] attributes
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Name != "InternalsAvailableAttribute" && 
                attribute.AttributeClass?.Name != "InternalsAvailable")
                continue;
                
            // Get the type from the first constructor argument
            if (attribute.ConstructorArguments.Length > 0)
            {
                var typeArg = attribute.ConstructorArguments[0];
                if (typeArg.Value is INamedTypeSymbol targetType)
                {
                    yield return targetType;
                }
            }
        }
    }
    
    private static void GenerateForClass(INamedTypeSymbol classSymbol, SourceProductionContext context)
    {
        // Skip the Grinspector class itself
        if (classSymbol.Name == "Grinspector" && classSymbol.ContainingNamespace?.ToDisplayString() == "Grinspector")
            return;

        // Get all private instance methods
        var privateMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.DeclaredAccessibility == Accessibility.Private 
                     && !m.IsStatic 
                     && m.MethodKind == MethodKind.Ordinary)
            .ToList();

        // Get all private instance properties
        var privateProperties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Private && !p.IsStatic)
            .ToList();

        // Get all private instance fields
        var privateFields = classSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.DeclaredAccessibility == Accessibility.Private 
                     && !f.IsStatic
                     && !f.IsImplicitlyDeclared) // Skip compiler-generated backing fields
            .ToList();

        if (privateMethods.Count == 0 && privateProperties.Count == 0 && privateFields.Count == 0)
            return;

        var source = GenerateGrinspectorPartial(classSymbol, privateMethods, privateProperties, privateFields);
        var fileName = $"Internals_{classSymbol.Name}_{classSymbol.ContainingNamespace?.ToDisplayString().Replace(".", "_")}.g.cs";
        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateGrinspectorPartial(INamedTypeSymbol targetType, List<IMethodSymbol> methods, 
        List<IPropertySymbol> properties, List<IFieldSymbol> fields)
    {
        var fullTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var simpleTypeName = targetType.Name;
        var targetNamespace = targetType.ContainingNamespace?.ToDisplayString() ?? "Global";
        
        // Generate all members
        var membersBuilder = new StringBuilder();
        
        foreach (var method in methods)
        {
            membersBuilder.Append(GenerateMethodWrapper(method));
        }

        foreach (var property in properties)
        {
            membersBuilder.Append(GeneratePropertyWrapper(property));
        }

        foreach (var field in fields)
        {
            membersBuilder.Append(GenerateFieldWrapper(field));
        }

        // Replace template placeholders
        return Templates.InternalsWrapper
            .Replace("{{NAMESPACE}}", targetNamespace)
            .Replace("{{TYPE_NAME}}", simpleTypeName)
            .Replace("{{FULL_TYPE_NAME}}", fullTypeName)
            .Replace("{{MEMBERS}}", membersBuilder.ToString());
    }

    private static string GenerateMethodWrapper(IMethodSymbol method)
    {
        var methodName = method.Name;
        var returnType = method.ReturnType.ToDisplayString();
        var parameters = string.Join(", ", method.Parameters.Select(p => 
            $"{p.Type.ToDisplayString()} {p.Name}"));
        var typeFullName = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Generate invoke code based on return type and parameters
        string invokeCode;
        var arguments = string.Join(", ", method.Parameters.Select(p => p.Name));
        
        if (method.ReturnsVoid)
        {
            if (method.Parameters.Length == 0)
                invokeCode = "methodInfo.Invoke(_instance, null);";
            else
                invokeCode = $"methodInfo.Invoke(_instance, new object?[] {{ {arguments} }});";
        }
        else
        {
            if (method.Parameters.Length == 0)
                invokeCode = $"return ({returnType})methodInfo.Invoke(_instance, null)!;";
            else
                invokeCode = $"return ({returnType})methodInfo.Invoke(_instance, new object?[] {{ {arguments} }})!;";
        }

        var template = Templates.MethodWrapper;
        return template
            .Replace("{{RETURN_TYPE}}", returnType)
            .Replace("{{METHOD_NAME}}", methodName)
            .Replace("{{PARAMETERS}}", parameters)
            .Replace("{{TYPE_FULL_NAME}}", typeFullName)
            .Replace("{{INVOKE}}", invokeCode);
    }

    private static string GeneratePropertyWrapper(IPropertySymbol property)
    {
        var propertyName = property.Name;
        var propertyType = property.Type.ToDisplayString();
        var typeFullName = property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var getter = string.Empty;
        var setter = string.Empty;

        if (property.GetMethod != null)
        {
            getter = Templates.PropertyGetter
                .Replace("{{TYPE_FULL_NAME}}", typeFullName)
                .Replace("{{PROPERTY_NAME}}", propertyName)
                .Replace("{{PROPERTY_TYPE}}", propertyType);
        }

        if (property.SetMethod != null)
        {
            setter = Templates.PropertySetter
                .Replace("{{TYPE_FULL_NAME}}", typeFullName)
                .Replace("{{PROPERTY_NAME}}", propertyName);
        }

        return Templates.PropertyWrapper
            .Replace("{{PROPERTY_TYPE}}", propertyType)
            .Replace("{{PROPERTY_NAME}}", propertyName)
            .Replace("{{GETTER}}", getter)
            .Replace("{{SETTER}}", setter);
    }

    private static string GenerateFieldWrapper(IFieldSymbol field)
    {
        var fieldName = field.Name;
        var fieldType = field.Type.ToDisplayString();
        var typeFullName = field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var getter = Templates.FieldGetter
            .Replace("{{TYPE_FULL_NAME}}", typeFullName)
            .Replace("{{FIELD_NAME}}", fieldName)
            .Replace("{{FIELD_TYPE}}", fieldType);

        var setter = string.Empty;
        if (!field.IsReadOnly)
        {
            setter = Templates.FieldSetter
                .Replace("{{TYPE_FULL_NAME}}", typeFullName)
                .Replace("{{FIELD_NAME}}", fieldName);
        }

        return Templates.FieldWrapper
            .Replace("{{FIELD_TYPE}}", fieldType)
            .Replace("{{FIELD_NAME}}", fieldName)
            .Replace("{{GETTER}}", getter)
            .Replace("{{SETTER}}", setter);
    }
}
