using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Azura.Generator
{
    [Generator]
    public class SerializationGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ForSerializationSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            ForSerializationSyntaxReceiver? syntaxReceiver = (ForSerializationSyntaxReceiver?)context.SyntaxReceiver;
            if (syntaxReceiver == null) return;
            foreach (var item in syntaxReceiver.Requests)
            {
                var sem = context.Compilation.GetSemanticModel(item.Attribute.SyntaxTree);
                string attr = sem.GetSymbolInfo(item.Attribute).Symbol!.ContainingSymbol.ToString();
                if (attr != "Azura.AzuraAttribute") continue;
                string name = item.Type.GetFullyQualifiedName();
                if (item.Type.TryGetParentSyntax(out BaseTypeDeclarationSyntax _))
                {
                    // This type has a type in the parent hierarchy, which means this is a nested type
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("CT0001", "Serialized type cannot be a nested type",
                            $"The Azura library does not support generating serialization sources for nested types (failed on {name}). Please move the type to a namespace's scope.",
                            "Azura.Generator", DiagnosticSeverity.Error, true), null));
                    continue;
                }

                string id = item.Type.Identifier.ToString();
                string? namespaceName = item.Type.GetParentName();
                if (string.IsNullOrWhiteSpace(namespaceName)) namespaceName = null;
                var elements = new List<ForSerializationSyntaxReceiver.RequestElementItem>(item.Elements);
                elements.RemoveAll(e =>
                    sem.GetSymbolInfo(e.Attribute).Symbol?.ContainingSymbol.ToString() !=
                    "Azura.AzuraAttribute");
                var sb = new StringBuilder(@"
#pragma warning disable 1591
#nullable enable
using System;
using System.IO;");
                if (namespaceName != null)
                    sb.Append(@$"
namespace {namespaceName}
{{");
                sb.Append(@$"
    public static class {id}Serialization
    {{
        public static {name} Deserialize(Stream stream)
        {{
            return new {name} {{");
                foreach (var element in elements)
                {
                    sb.Append(@$"
                {element.Name} = ");
                    GetInfo(sem, element.Type, out bool selfNullable, out bool isArray, out bool elementNullable,
                        out bool elementValueType);
                    if (selfNullable)
                        sb.Append("byteSerialization.Deserialize(stream) == 0 ? null : ");
                    string symbol = $"{sem.GetSymbolInfo(element.Type).Symbol}";
                    if (isArray) symbol = symbol.TrimEnd('?').TrimEnd('[', ']');
                    symbol = symbol.TrimEnd('?');

                    if (isArray && !elementNullable && _primitives.Contains(symbol))
                        sb.Append(
                            $"{symbol}Serialization.DeserializeArray(stream, intSerialization.Deserialize(stream)),");
                    else
                        sb.Append(isArray
                            ? elementNullable
                                ? elementValueType
                                    ? $"SerializationBase.DeserializeArrayValueNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),"
                                    : $"SerializationBase.DeserializeArrayNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),"
                                : $"SerializationBase.DeserializeArray<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),"
                            : $"{symbol}Serialization.Deserialize(stream),");
                }

                sb.Append(@$"
            }};
        }}

        public static void Serialize(this {id} self, Stream stream)
        {{");
                foreach (var element in elements)
                {
                    GetInfo(sem, element.Type, out bool selfNullable, out bool isArray, out bool elementNullable,
                        out bool elementValueType);
                    if (selfNullable)
                        sb.Append($@"
            (self.{element.Name} != default ? (byte)1 : (byte)0).Serialize(stream);");
                    if (isArray)
                    {
                        if (selfNullable)
                            sb.Append(@$"
            if(self.{element.Name} != default)
            {{");
                        sb.Append(@$"
            self.{element.Name}.Length.Serialize(stream);");
                        string symbol = $"{sem.GetSymbolInfo(element.Type).Symbol}";
                        if (isArray) symbol = symbol.TrimEnd('?').TrimEnd('[', ']');
                        symbol = symbol.TrimEnd('?');
                        if (isArray && !elementNullable && _primitives.Contains(symbol))
                            sb.Append(@$"
            {symbol}Serialization.SerializeArray(self.{element.Name}, stream);");
                        else
                            sb.Append(elementNullable
                                ? elementValueType
                                    ? @$"
            SerializationBase.SerializeArrayValueNullable<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);"
                                    : @$"
            SerializationBase.SerializeArrayNullable<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);"
                                : @$"
            SerializationBase.SerializeArray<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);");
                        if (selfNullable)
                            sb.Append(@"
            }");
                    }
                    else
                        sb.Append(selfNullable
                            ? @$"
                self.{element.Name}?.Serialize(stream);"
                            : @$"
                self.{element.Name}.Serialize(stream);");
                }

                sb.Append(@"
        }
    }");
                if (namespaceName != null)
                    sb.Append(@"
}");
                context.AddSource($"{name}Serialization.cs", sb.ToString());
            }
        }

        private static readonly HashSet<string> _primitives = new()
        {
            "byte",
            "sbyte",
            "ushort",
            "short",
            "uint",
            "int",
            "ulong",
            "long",
            "float",
            "double"
        };

        private static void GetInfo(SemanticModel semanticModel, TypeSyntax typeSyntax,
            out bool selfNullable, out bool isArray, out bool elementNullable, out bool elementValueType)
        {
            var info = semanticModel.GetTypeInfo(typeSyntax);
            selfNullable = typeSyntax is NullableTypeSyntax ||
                           info.Type!.NullableAnnotation == NullableAnnotation.Annotated;
            isArray = info.Type!.TypeKind == TypeKind.Array;
            elementNullable = isArray && (info.Type as IArrayTypeSymbol)!.ElementType.NullableAnnotation ==
                NullableAnnotation.Annotated;
            elementValueType = isArray && (info.Type as IArrayTypeSymbol)!.ElementType.IsValueType;
        }

        private class ForSerializationSyntaxReceiver : ISyntaxReceiver
        {
            public record RequestItem(TypeDeclarationSyntax Type, AttributeSyntax Attribute,
                List<RequestElementItem> Elements);

            public record RequestElementItem(AttributeSyntax Attribute, TypeSyntax Type, SyntaxToken Name);

            public List<RequestItem> Requests { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is TypeDeclarationSyntax tds)
                    foreach (AttributeSyntax x in tds.AttributeLists.SelectMany(v => v.Attributes))
                        if (x.Name.ToString() == "Azura")
                        {
                            var items = new List<RequestElementItem>();
                            foreach (var y in tds.Members)
                            {
                                if (y is PropertyDeclarationSyntax pds)
                                {
                                    foreach (var z in y.AttributeLists.SelectMany(v => v.Attributes))
                                        if (z.Name.ToString() == "Azura")
                                        {
                                            items.Add(new RequestElementItem(z, pds.Type, pds.Identifier));
                                            break;
                                        }
                                }

                                if (y is FieldDeclarationSyntax fds)
                                {
                                    foreach (var z in y.AttributeLists.SelectMany(v => v.Attributes))
                                        if (z.Name.ToString() == "Azura")
                                        {
                                            foreach (VariableDeclaratorSyntax v in fds.Declaration.Variables)
                                                items.Add(new RequestElementItem(z, fds.Declaration.Type,
                                                    v.Identifier));
                                            break;
                                        }
                                }
                            }

                            Requests.Add(new RequestItem(tds, x, items));
                        }
            }
        }
    }
}
