using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AotGenerator;

[Generator(LanguageNames.CSharp)]
public class AotServerGenerator : IIncrementalGenerator
{
    private static readonly string[] _attributeNames;

    static AotServerGenerator()
    {
        _attributeNames = new[] { "Group", "Get", "Post", "Delete", "Put" };
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, (sourceProductionContext, compilation) =>
        {
            var mainMethod = compilation.GetEntryPoint(CancellationToken.None);
            var @namespace = mainMethod!.ContainingNamespace.ToDisplayString();
            //GenerateAutoApi(@namespace, sourceProductionContext);
            GenerateAttributes(@namespace, sourceProductionContext, _attributeNames);
            GenerateWebApplicationExtensions(@namespace, sourceProductionContext, compilation);
        });
    }

    private void GenerateAttributes(string @namespace, SourceProductionContext sourceProductionContext,
        params string[] attributeNames)
    {
        var namespaceDeclarationSyntax = SyntaxFactory
            .NamespaceDeclaration(SyntaxFactory.ParseName(@namespace));
        foreach (var attributeName in attributeNames)
        {
            var name = attributeName;
            if (!name.EndsWith("Attribute"))
            {
                name = $"{name}Attribute";
            }

            namespaceDeclarationSyntax = namespaceDeclarationSyntax.AddMembers(SyntaxFactory
                .ClassDeclaration(name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                .AddBaseListTypes(
                    SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("global::System.Attribute")))
                .AddMembers(SyntaxFactory.ParseMemberDeclaration("private readonly string _pattern;")!,
                    SyntaxFactory.ParseMemberDeclaration($"public {name}(string pattern){{ _pattern = pattern;}}")!));
        }

        var compilationUnitSyntax = SyntaxFactory.CompilationUnit().AddMembers(namespaceDeclarationSyntax);
        sourceProductionContext.AddSource("Attributes.g.cs",
            compilationUnitSyntax.NormalizeWhitespace().ToFullString());
    }

    private static void GenerateWebApplicationExtensions(string @namespace,
        SourceProductionContext sourceProductionContext, Compilation compilation)
    {
        //if (!Debugger.IsAttached)
        //{
        //    Debugger.Launch();
        //}

        var className = "WebApplicationExtensions";
        var methodDeclarationSyntax =
            SyntaxFactory
                .MethodDeclaration(SyntaxFactory.ParseTypeName("global::Microsoft.AspNetCore.Builder.WebApplication"),
                    "SetMap")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.List<AttributeListSyntax>(),
                    SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ThisKeyword)),
                    SyntaxFactory.ParseTypeName("global::Microsoft.AspNetCore.Builder.WebApplication"),
                    SyntaxFactory.Identifier("app"), null))
                .WithBody(SyntaxFactory.Block());

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            methodDeclarationSyntax = AddMap(methodDeclarationSyntax, syntaxTree, sourceProductionContext, @namespace,
                compilation);
        }

        methodDeclarationSyntax =
            methodDeclarationSyntax.AddBodyStatements(SyntaxFactory.ParseStatement("return app;"));
        var classDeclarationSyntax = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            .AddMembers(methodDeclarationSyntax);

        var compilationUnitSyntax = SyntaxFactory.CompilationUnit()
            .AddMembers(SyntaxFactory
                .NamespaceDeclaration(SyntaxFactory.ParseName(@namespace)).AddMembers(classDeclarationSyntax));
        sourceProductionContext.AddSource($"{className}.g.cs",
            compilationUnitSyntax.NormalizeWhitespace().ToFullString());
    }

    private static MethodDeclarationSyntax AddMap(MethodDeclarationSyntax methodDeclarationSyntax,
        SyntaxTree syntaxTree, SourceProductionContext sourceProductionContext, string @namespace,
        Compilation compilation)
    {
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        foreach (var classDeclarationSyntax in syntaxTree.GetCompilationUnitRoot().DescendantNodes()
                     .OfType<ClassDeclarationSyntax>())
        {
            var groupAttributeSyntax = GetAttributeSyntax(classDeclarationSyntax, "Group");

            if (groupAttributeSyntax != null)
            {
                var className = classDeclarationSyntax.Identifier.Text;
                var declaredSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax)!;
                //GenerateServiceExtensions(sourceProductionContext, classDeclarationSyntax, groupAttributeSyntax, @namespace, compilation); methodDeclarationSyntax = methodDeclarationSyntax.AddBodyStatements(
                //    SyntaxFactory.ParseStatement(
                //        $"app.Map{className}();"));
                var group = groupAttributeSyntax.ArgumentList!.Arguments.ToFullString().Trim('/');
                methodDeclarationSyntax = methodDeclarationSyntax.AddBodyStatements(
                    SyntaxFactory.ParseStatement(
                        $"var api{className} = app.MapGroup({group});"));
                foreach (var declarationSyntax in classDeclarationSyntax.DescendantNodes()
                             .OfType<MethodDeclarationSyntax>())
                {
                    var methodAttributeSyntax = GetAttributeSyntax(declarationSyntax, _attributeNames);
                    if (methodAttributeSyntax != null)
                    {
                        var args = string.Join(", ",
                            declarationSyntax.ParameterList.Parameters.Select(x => x.Identifier.Text));
                        methodDeclarationSyntax =
                            methodDeclarationSyntax.AddBodyStatements(
                                SyntaxFactory.ParseStatement(
                                    $"api{className}.Map{methodAttributeSyntax.Name.ToFullString().Replace("Attribute", "")}({methodAttributeSyntax.ArgumentList!.Arguments.ToFullString()}, ({declarationSyntax.ParameterList.Parameters.ToFullString()}) => new global::{declaredSymbol.ContainingNamespace.ToDisplayString()}.{className}().{declarationSyntax.Identifier.Text}({args}));"));
                    }
                }
            }
        }

        return methodDeclarationSyntax;
    }

    private static void GenerateServiceExtensions(SourceProductionContext sourceProductionContext,
        ClassDeclarationSyntax serviceClassDeclarationSyntax, AttributeSyntax groupAttributeSyntax, string @namespace,
        Compilation compilation)
    {
        var semanticModel = compilation.GetSemanticModel(serviceClassDeclarationSyntax.SyntaxTree);
        var symbol = semanticModel.GetDeclaredSymbol(serviceClassDeclarationSyntax)!;
        var className = $"{serviceClassDeclarationSyntax.Identifier.Text}Extensions";
        var methodDeclarationSyntax = (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
                $@"public static void Map{serviceClassDeclarationSyntax.Identifier.Text}(this global::Microsoft.AspNetCore.Builder.WebApplication app){{}}")
            !;
        methodDeclarationSyntax = methodDeclarationSyntax.AddBodyStatements(
            SyntaxFactory.ParseStatement(
                $"var api = app.MapGroup({groupAttributeSyntax.ArgumentList!.Arguments.ToFullString().Trim('/')});"));
        foreach (var declarationSyntax in serviceClassDeclarationSyntax.DescendantNodes()
                     .OfType<MethodDeclarationSyntax>())
        {
            var methodAttributeSyntax = GetAttributeSyntax(declarationSyntax, _attributeNames);
            if (methodAttributeSyntax != null)
            {
                var args = string.Join(", ",
                    declarationSyntax.ParameterList.Parameters.Select(x => x.Identifier.Text));
                methodDeclarationSyntax =
                    methodDeclarationSyntax.AddBodyStatements(
                        SyntaxFactory.ParseStatement(
                            $"api.Map{methodAttributeSyntax.Name.ToFullString().Replace("Attribute", "")}({methodAttributeSyntax.ArgumentList!.Arguments.ToFullString()}, ({declarationSyntax.ParameterList.Parameters.ToFullString()}) => new {serviceClassDeclarationSyntax.Identifier.Text}().{declarationSyntax.Identifier.Text}({args}));"));
            }
        }

        var classDeclarationSyntax = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)).AddMembers(methodDeclarationSyntax!);
        sourceProductionContext.AddSource($"{className}.g.cs",
            SyntaxFactory.CompilationUnit()
                .AddUsings(SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName(symbol.ContainingNamespace.ToDisplayString())))
                .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(@namespace))
                    .AddMembers(classDeclarationSyntax)).NormalizeWhitespace().ToFullString());
    }

    private static AttributeSyntax GetAttributeSyntax(MemberDeclarationSyntax classDeclarationSyntax,
        params string[] attributeNames)
    {
        AttributeSyntax attributeSyntax = null;

        if (classDeclarationSyntax.AttributeLists.Any())
        {
            foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
            {
                if (attributeSyntax != null)
                {
                    break;
                }

                if (attributeListSyntax.Attributes.Any())
                {
                    foreach (var attribute in attributeListSyntax.Attributes)
                    {
                        if (attributeSyntax != null)
                        {
                            break;
                        }

                        foreach (var attributeName in attributeNames)
                        {
                            if (attribute.Name.ToFullString().Equals($"{attributeName}") ||
                                attribute.Name.ToFullString().Equals($"{attributeName}Attribute"))
                            {
                                attributeSyntax = attribute;
                                break;
                            }
                        }
                    }
                }
            }
        }

        return attributeSyntax;
    }
}