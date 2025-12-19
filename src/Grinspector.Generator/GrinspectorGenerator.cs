using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
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
            
        // Find all [PrivatesAvailable(typeof(...))] attributes
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Name != "PrivatesAvailableAttribute" && 
                attribute.AttributeClass?.Name != "PrivatesAvailable")
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

        // Get all private static methods
        var privateStaticMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.DeclaredAccessibility == Accessibility.Private 
                     && m.IsStatic 
                     && m.MethodKind == MethodKind.Ordinary)
            .ToList();

        // Get all private static properties
        var privateStaticProperties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Private && p.IsStatic)
            .ToList();

        // Get all private static fields
        var privateStaticFields = classSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.DeclaredAccessibility == Accessibility.Private 
                     && f.IsStatic
                     && !f.IsImplicitlyDeclared)
            .ToList();

        // Get all private constructors
        var privateConstructors = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.DeclaredAccessibility == Accessibility.Private 
                     && m.MethodKind == MethodKind.Constructor)
            .ToList();

        if (privateMethods.Count == 0 && privateProperties.Count == 0 && privateFields.Count == 0 &&
            privateStaticMethods.Count == 0 && privateStaticProperties.Count == 0 && privateStaticFields.Count == 0 &&
            privateConstructors.Count == 0)
            return;

        var source = GenerateGrinspectorPartial(classSymbol, privateMethods, privateProperties, privateFields,
            privateStaticMethods, privateStaticProperties, privateStaticFields, privateConstructors);
        var fileName = $"{classSymbol.Name}_Privates_{classSymbol.ContainingNamespace?.ToDisplayString().Replace(".", "_")}.g.cs";
        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateGrinspectorPartial(INamedTypeSymbol targetType, List<IMethodSymbol> methods, 
        List<IPropertySymbol> properties, List<IFieldSymbol> fields,
        List<IMethodSymbol> staticMethods, List<IPropertySymbol> staticProperties, List<IFieldSymbol> staticFields,
        List<IMethodSymbol> constructors)
    {
        var fullTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var simpleTypeName = targetType.Name;
        var targetNamespace = targetType.ContainingNamespace?.ToDisplayString() ?? "Global";
        
        // Generate instance members
        var membersBuilder = new StringBuilder();
        
        foreach (var method in methods)
        {
            membersBuilder.Append(GenerateMethodWrapper(method, isStatic: false));
        }

        foreach (var property in properties)
        {
            membersBuilder.Append(GeneratePropertyWrapper(property, isStatic: false));
        }

        foreach (var field in fields)
        {
            membersBuilder.Append(GenerateFieldWrapper(field, isStatic: false));
        }

        // Generate static members
        var staticMembersBuilder = new StringBuilder();
        
        foreach (var method in staticMethods)
        {
            staticMembersBuilder.Append(GenerateMethodWrapper(method, isStatic: true));
        }

        foreach (var property in staticProperties)
        {
            staticMembersBuilder.Append(GeneratePropertyWrapper(property, isStatic: true));
        }

        foreach (var field in staticFields)
        {
            staticMembersBuilder.Append(GenerateFieldWrapper(field, isStatic: true));
        }

        // Generate constructor wrappers
        foreach (var constructor in constructors)
        {
            staticMembersBuilder.Append(GenerateConstructorWrapper(constructor));
        }

        // Replace template placeholders
        return Templates.Wrapper
            .Replace("{{NAMESPACE}}", targetNamespace)
            .Replace("{{TYPE_NAME}}", simpleTypeName)
            .Replace("{{FULL_TYPE_NAME}}", fullTypeName)
            .Replace("{{MEMBERS}}", membersBuilder.ToString())
            .Replace("{{STATIC_MEMBERS}}", staticMembersBuilder.ToString());
    }

    private static string GenerateMethodWrapper(IMethodSymbol method, bool isStatic)
    {
        var methodName = method.Name;
        var returnType = method.ReturnType.ToDisplayString();
        var parameters = string.Join(", ", method.Parameters.Select(p => 
            $"{p.Type.ToDisplayString()} {p.Name}"));
        var typeFullName = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Generate invoke code based on return type and parameters
        string invokeCode;
        var arguments = string.Join(", ", method.Parameters.Select(p => p.Name));
        var bindingFlags = isStatic 
            ? "System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic"
            : "System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic";
        var invokeTarget = isStatic ? "null" : "_instance";
        
        if (method.ReturnsVoid)
        {
            if (method.Parameters.Length == 0)
                invokeCode = $"methodInfo.Invoke({invokeTarget}, null);";
            else
                invokeCode = $"methodInfo.Invoke({invokeTarget}, new object?[] {{ {arguments} }});";
        }
        else
        {
            if (method.Parameters.Length == 0)
                invokeCode = $"return ({returnType})methodInfo.Invoke({invokeTarget}, null)!;";
            else
                invokeCode = $"return ({returnType})methodInfo.Invoke({invokeTarget}, new object?[] {{ {arguments} }})!;";
        }

        var template = isStatic ? Templates.StaticMethod : Templates.Method;
        return template
            .Replace("{{RETURN_TYPE}}", returnType)
            .Replace("{{METHOD_NAME}}", methodName)
            .Replace("{{PARAMETERS}}", parameters)
            .Replace("{{TYPE_FULL_NAME}}", typeFullName)
            .Replace("{{BINDING_FLAGS}}", bindingFlags)
            .Replace("{{INVOKE}}", invokeCode);
    }

    private static string GeneratePropertyWrapper(IPropertySymbol property, bool isStatic)
    {
        var propertyName = property.Name;
        var propertyType = property.Type.ToDisplayString();
        var typeFullName = property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var bindingFlags = isStatic 
            ? "System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic"
            : "System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic";

        var getter = string.Empty;
        var setter = string.Empty;

        if (property.GetMethod != null)
        {
            var template = isStatic ? Templates.StaticPropertyGetter : Templates.PropertyGetter;
            getter = template
                .Replace("{{TYPE_FULL_NAME}}", typeFullName)
                .Replace("{{PROPERTY_NAME}}", propertyName)
                .Replace("{{PROPERTY_TYPE}}", propertyType)
                .Replace("{{BINDING_FLAGS}}", bindingFlags);
        }

        if (property.SetMethod != null)
        {
            var template = isStatic ? Templates.StaticPropertySetter : Templates.PropertySetter;
            setter = template
                .Replace("{{TYPE_FULL_NAME}}", typeFullName)
                .Replace("{{PROPERTY_NAME}}", propertyName)
                .Replace("{{BINDING_FLAGS}}", bindingFlags);
        }

        var propertyTemplate = isStatic ? Templates.StaticProperty : Templates.Property;
        return propertyTemplate
            .Replace("{{PROPERTY_TYPE}}", propertyType)
            .Replace("{{PROPERTY_NAME}}", propertyName)
            .Replace("{{GETTER}}", getter)
            .Replace("{{SETTER}}", setter);
    }

    private static string GenerateFieldWrapper(IFieldSymbol field, bool isStatic)
    {
        var fieldName = field.Name;
        var fieldType = field.Type.ToDisplayString();
        var typeFullName = field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var bindingFlags = isStatic 
            ? "System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic"
            : "System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic";

        var template = isStatic ? Templates.StaticFieldGetter : Templates.FieldGetter;
        var getter = template
            .Replace("{{TYPE_FULL_NAME}}", typeFullName)
            .Replace("{{FIELD_NAME}}", fieldName)
            .Replace("{{FIELD_TYPE}}", fieldType)
            .Replace("{{BINDING_FLAGS}}", bindingFlags);

        var setter = string.Empty;
        if (!field.IsReadOnly)
        {
            var setterTemplate = isStatic ? Templates.StaticFieldSetter : Templates.FieldSetter;
            setter = setterTemplate
                .Replace("{{TYPE_FULL_NAME}}", typeFullName)
                .Replace("{{FIELD_NAME}}", fieldName)
                .Replace("{{BINDING_FLAGS}}", bindingFlags);
        }

        var fieldTemplate = isStatic ? Templates.StaticField : Templates.Field;
        return fieldTemplate
            .Replace("{{FIELD_TYPE}}", fieldType)
            .Replace("{{FIELD_NAME}}", fieldName)
            .Replace("{{GETTER}}", getter)
            .Replace("{{SETTER}}", setter);
    }

    private static string GenerateConstructorWrapper(IMethodSymbol constructor)
    {
        var parameters = string.Join(", ", constructor.Parameters.Select(p => 
            $"{p.Type.ToDisplayString()} {p.Name}"));
        var typeFullName = constructor.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var simpleTypeName = constructor.ContainingType.Name;
        var arguments = string.Join(", ", constructor.Parameters.Select(p => p.Name));

        var paramTypes = string.Join(", ", constructor.Parameters.Select(p => 
            $"typeof({p.Type.ToDisplayString()})"));

        string invokeCode;
        if (constructor.Parameters.Length == 0)
        {
            invokeCode = $"return ({typeFullName})ctorInfo.Invoke(null)!;";
        }
        else
        {
            invokeCode = $"return ({typeFullName})ctorInfo.Invoke(new object?[] {{ {arguments} }})!;";
        }

        return Templates.Constructor
            .Replace("{{TYPE_FULL_NAME}}", typeFullName)
            .Replace("{{TYPE_NAME}}", simpleTypeName)
            .Replace("{{PARAMETERS}}", parameters)
            .Replace("{{PARAM_TYPES}}", paramTypes)
            .Replace("{{INVOKE}}", invokeCode);
    }
}
