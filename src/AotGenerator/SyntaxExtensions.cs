using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AotGenerator;

public static class SyntaxExtensions
{
    private static readonly string _fullName;
    private static readonly Version _version;

    static SyntaxExtensions()
    {
        var type = typeof(AotServerGenerator);
        var assembly = type.Assembly;
        _fullName = type.FullName;
        _version = assembly.GetName().Version;
    }
    public static T AddGeneratedCodeAttribute<T>(this T syntax)
    where T : MemberDeclarationSyntax
    {
        return (T)syntax.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(
            new List<AttributeSyntax>
            {
                SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("global::System.CodeDom.Compiler.GeneratedCodeAttribute"),
                    SyntaxFactory.ParseAttributeArgumentList($"(\"global::{_fullName}\", \"{_version}\")"))
            })));
    }
}