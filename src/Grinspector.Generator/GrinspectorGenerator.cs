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
        int ctorIndex = 0;
        foreach (var constructor in constructors)
        {
            staticMembersBuilder.Append(GenerateConstructorWrapper(constructor, ctorIndex++));
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
        var fullTypeName = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeFullName = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isVoid = method.ReturnsVoid;
        
        var parameters = string.Join(", ", method.Parameters.Select(p => 
            $"{p.Type.ToDisplayString()} {p.Name}"));
        
        var bindingFlags = isStatic 
            ? "System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic"
            : "System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic";

        // Generate Expression tree components
        var paramExpressions = new StringBuilder();
        var paramArgs = new StringBuilder();
        var lambdaParams = new StringBuilder();
        var delegateInvoke = new StringBuilder();
        
        // Build the delegate type signature
        var delegateTypeParams = new List<string>();
        
        if (!isStatic)
        {
            delegateTypeParams.Add(fullTypeName);
            lambdaParams.Append("instanceParam");
            delegateInvoke.Append("_instance");
        }
        
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            var param = method.Parameters[i];
            var paramType = param.Type.ToDisplayString();
            var paramName = param.Name;
            
            delegateTypeParams.Add(paramType);
            paramExpressions.AppendLine($"            var param{i} = System.Linq.Expressions.Expression.Parameter(typeof({paramType}), \"{paramName}\");");
            paramArgs.Append($", param{i}");
            
            if (lambdaParams.Length > 0)
                lambdaParams.Append(", ");
            lambdaParams.Append($"param{i}");
            
            if (delegateInvoke.Length > 0)
                delegateInvoke.Append(", ");
            delegateInvoke.Append(paramName);
        }

        // Handle type parameters for overload resolution
        var typeParams = "";
        if (method.Parameters.Length > 0)
        {
            typeParams = $",\n                new System.Type[] {{ {string.Join(", ", method.Parameters.Select(p => $"typeof({p.Type.ToDisplayString()})"))} }}";
        }

        // For void methods, use Action; for non-void, use Func
        string delegateType;
        string invokeResult;
        
        if (isVoid)
        {
            if (delegateTypeParams.Count == 0)
                delegateType = "System.Action";
            else
                delegateType = $"System.Action<{string.Join(", ", delegateTypeParams)}>";
            invokeResult = "_" + methodName + "_delegate(" + delegateInvoke.ToString() + ");";
        }
        else
        {
            delegateTypeParams.Add(returnType);
            delegateType = $"System.Func<{string.Join(", ", delegateTypeParams)}>";
            invokeResult = "return _" + methodName + "_delegate(" + delegateInvoke.ToString() + ");";
        }

        var template = isStatic ? Templates.StaticMethod : Templates.Method;
        var result = template
            .Replace("{{DELEGATE_TYPE}}", delegateType)
            .Replace("{{RETURN_TYPE}}", returnType)
            .Replace("{{METHOD_NAME}}", methodName)
            .Replace("{{PARAMETERS}}", parameters)
            .Replace("{{TYPE_FULL_NAME}}", typeFullName)
            .Replace("{{FULL_TYPE_NAME}}", fullTypeName)
            .Replace("{{BINDING_FLAGS}}", bindingFlags)
            .Replace("{{PARAM_EXPRESSIONS}}", paramExpressions.ToString())
            .Replace("{{PARAM_ARGS}}", paramArgs.ToString())
            .Replace("{{LAMBDA_PARAMS}}", lambdaParams.Length > 0 ? ", " + lambdaParams.ToString() : "")
            .Replace("{{DELEGATE_INVOKE}}", invokeResult)
            .Replace("{{TYPE_PARAMS}}", typeParams);
            
        return result;
    }

    private static string GeneratePropertyWrapper(IPropertySymbol property, bool isStatic)
    {
        var propertyName = property.Name;
        var propertyType = property.Type.ToDisplayString();
        var fullTypeName = property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeFullName = property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var bindingFlags = isStatic 
            ? "System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic"
            : "System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic";

        var delegates = new StringBuilder();
        var getter = string.Empty;
        var setter = string.Empty;

        if (property.GetMethod != null)
        {
            var delegateTemplate = isStatic ? Templates.StaticPropertyGetterDelegate : Templates.PropertyGetterDelegate;
            delegates.Append(delegateTemplate
                .Replace("{{TYPE_FULL_NAME}}", typeFullName)
                .Replace("{{FULL_TYPE_NAME}}", fullTypeName)
                .Replace("{{PROPERTY_NAME}}", propertyName)
                .Replace("{{PROPERTY_TYPE}}", propertyType)
                .Replace("{{BINDING_FLAGS}}", bindingFlags));
            
            var template = isStatic ? Templates.StaticPropertyGetter : Templates.PropertyGetter;
            getter = template
                .Replace("{{PROPERTY_NAME}}", propertyName);
        }

        if (property.SetMethod != null)
        {
            var delegateTemplate = isStatic ? Templates.StaticPropertySetterDelegate : Templates.PropertySetterDelegate;
            delegates.Append(delegateTemplate
                .Replace("{{TYPE_FULL_NAME}}", typeFullName)
                .Replace("{{FULL_TYPE_NAME}}", fullTypeName)
                .Replace("{{PROPERTY_NAME}}", propertyName)
                .Replace("{{PROPERTY_TYPE}}", propertyType)
                .Replace("{{BINDING_FLAGS}}", bindingFlags));
            
            var template = isStatic ? Templates.StaticPropertySetter : Templates.PropertySetter;
            setter = template
                .Replace("{{PROPERTY_NAME}}", propertyName);
        }

        var propertyTemplate = isStatic ? Templates.StaticProperty : Templates.Property;
        return delegates.ToString() + propertyTemplate
            .Replace("{{PROPERTY_TYPE}}", propertyType)
            .Replace("{{PROPERTY_NAME}}", propertyName)
            .Replace("{{GETTER}}", getter)
            .Replace("{{SETTER}}", setter);
    }

    private static string GenerateFieldWrapper(IFieldSymbol field, bool isStatic)
    {
        var fieldName = field.Name;
        var fieldType = field.Type.ToDisplayString();
        var fullTypeName = field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeFullName = field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var bindingFlags = isStatic 
            ? "System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic"
            : "System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic";

        var delegates = new StringBuilder();
        
        var getterDelegateTemplate = isStatic ? Templates.StaticFieldGetterDelegate : Templates.FieldGetterDelegate;
        delegates.Append(getterDelegateTemplate
            .Replace("{{TYPE_FULL_NAME}}", typeFullName)
            .Replace("{{FULL_TYPE_NAME}}", fullTypeName)
            .Replace("{{FIELD_NAME}}", fieldName)
            .Replace("{{FIELD_TYPE}}", fieldType)
            .Replace("{{BINDING_FLAGS}}", bindingFlags));

        var template = isStatic ? Templates.StaticFieldGetter : Templates.FieldGetter;
        var getter = template
            .Replace("{{FIELD_NAME}}", fieldName);

        var setter = string.Empty;
        if (!field.IsReadOnly)
        {
            var setterDelegateTemplate = isStatic ? Templates.StaticFieldSetterDelegate : Templates.FieldSetterDelegate;
            delegates.Append(setterDelegateTemplate
                .Replace("{{TYPE_FULL_NAME}}", typeFullName)
                .Replace("{{FULL_TYPE_NAME}}", fullTypeName)
                .Replace("{{FIELD_NAME}}", fieldName)
                .Replace("{{FIELD_TYPE}}", fieldType)
                .Replace("{{BINDING_FLAGS}}", bindingFlags));
            
            var setterTemplate = isStatic ? Templates.StaticFieldSetter : Templates.FieldSetter;
            setter = setterTemplate
                .Replace("{{FIELD_NAME}}", fieldName);
        }

        var fieldTemplate = isStatic ? Templates.StaticField : Templates.Field;
        return delegates.ToString() + fieldTemplate
            .Replace("{{FIELD_TYPE}}", fieldType)
            .Replace("{{FIELD_NAME}}", fieldName)
            .Replace("{{GETTER}}", getter)
            .Replace("{{SETTER}}", setter);
    }

    private static string GenerateConstructorWrapper(IMethodSymbol constructor, int index)
    {
        var parameters = string.Join(", ", constructor.Parameters.Select(p => 
            $"{p.Type.ToDisplayString()} {p.Name}"));
        var typeFullName = constructor.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var simpleTypeName = constructor.ContainingType.Name;

        var paramTypes = string.Join(", ", constructor.Parameters.Select(p => 
            $"typeof({p.Type.ToDisplayString()})"));

        // Generate Expression tree components
        var paramExpressions = new StringBuilder();
        var paramArgs = new StringBuilder();
        var lambdaParams = new StringBuilder();
        var delegateInvoke = new StringBuilder();
        
        // Build delegate type params
        var delegateTypeParams = new List<string>();
        
        for (int i = 0; i < constructor.Parameters.Length; i++)
        {
            var param = constructor.Parameters[i];
            var paramType = param.Type.ToDisplayString();
            var paramName = param.Name;
            
            delegateTypeParams.Add(paramType);
            paramExpressions.AppendLine($"            var param{i} = System.Linq.Expressions.Expression.Parameter(typeof({paramType}), \"{paramName}\");");
            paramArgs.Append($", param{i}");
            
            if (lambdaParams.Length > 0)
                lambdaParams.Append(", ");
            lambdaParams.Append($"param{i}");
            
            if (delegateInvoke.Length > 0)
                delegateInvoke.Append(", ");
            delegateInvoke.Append(paramName);
        }

        // Use unique delegate field names based on parameter signature to avoid conflicts,
        // but keep method name as "CreateInstance" for all (C# overloading handles it)
        var delegateSuffix = index > 0 ? $"_{index}" : "";
        var compileSuffix = index > 0 ? $"_{index}" : "";
        
        delegateTypeParams.Add(typeFullName);
        string delegateType = $"System.Func<{string.Join(", ", delegateTypeParams)}>";
        string invokeResult = $"return _CreateInstance{delegateSuffix}_delegate(" + delegateInvoke.ToString() + ");";

        var result = Templates.Constructor
            .Replace("{{TYPE_FULL_NAME}}", typeFullName)
            .Replace("{{TYPE_NAME}}", simpleTypeName)
            .Replace("{{PARAMETERS}}", parameters)
            .Replace("{{PARAM_TYPES}}", paramTypes)
            .Replace("{{PARAM_EXPRESSIONS}}", paramExpressions.ToString())
            .Replace("{{PARAM_ARGS}}", paramArgs.ToString())
            .Replace("{{LAMBDA_PARAMS}}", lambdaParams.Length > 0 ? ", " + lambdaParams.ToString() : "")
            .Replace("{{FUNC_PARAMS}}", delegateTypeParams.Count > 1 ? string.Join(", ", delegateTypeParams.Take(delegateTypeParams.Count - 1)) + ", " : "")
            .Replace("{{DELEGATE_INVOKE}}", invokeResult);
        
        // Add suffix to delegate field names and compile method names to make them unique
        result = result.Replace("_CreateInstance_delegate", $"_CreateInstance{delegateSuffix}_delegate");
        result = result.Replace("CompileConstructor()", $"CompileConstructor{compileSuffix}()");
        
        return result;
    }
}
