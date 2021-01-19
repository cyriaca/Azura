using System;
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
                var sbDe = new StringBuilder(@"
#pragma warning disable 1591
#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;");
                var sbSe = new StringBuilder();
                if (namespaceName != null)
                    sbDe.Append(@$"
namespace {namespaceName}
{{");
                sbDe.Append(@$"
    public static class {id}Serialization
    {{
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static {name} Deserialize(Stream stream)
        {{
            return new {name} {{");
                foreach (var element in elements)
                {
                    sbDe.Append(@$"
                {element.Name} = ");
                    GetInfo(sem, element.Type, out MemberKind kind, out bool nullable, out var elementInfo);
                    if (kind == MemberKind.Unsupported)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor("CT0002", "Unsupported generic type",
                                $"The Azura library does not support generating serialization sources for this type (failed on {name}:{element.Type} {sem.GetTypeInfo(element.Type).Type}).",
                                "Azura.Generator", DiagnosticSeverity.Error, true), null));
                        return;
                    }

                    if (nullable)
                        sbDe.Append("byteSerialization.Deserialize(stream) == 0 ? null : ");
                    string symbol = $"{sem.GetSymbolInfo(element.Type).Symbol}";
                    if (kind == MemberKind.Array) symbol = symbol.TrimEnd('?').TrimEnd('[', ']');
                    symbol = symbol.TrimEnd('?');
                    if (nullable)
                        sbSe.Append($@"
            (self.{element.Name} != default ? (byte)1 : (byte)0).Serialize(stream);");
                    switch (kind)
                    {
                        case MemberKind.Plain:
                        {
                            sbDe.Append($"{symbol}Serialization.Deserialize(stream),");
                            sbSe.Append(nullable
                                ? @$"
                self.{element.Name}?.Serialize(stream);"
                                : @$"
                self.{element.Name}.Serialize(stream);");
                            break;
                        }
                        case MemberKind.Array:
                        {
                            var info = elementInfo[0];
                            if (!info.Nullable && _primitives.Contains(symbol))
                                sbDe.Append(
                                    $"{symbol}Serialization.DeserializeArray(stream, intSerialization.Deserialize(stream)),");
                            else
                                sbDe.Append(info.Nullable
                                    ? info.ValueType
                                        ? $"SerializationBase.DeserializeArrayValueNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),"
                                        : $"SerializationBase.DeserializeArrayNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),"
                                    : $"SerializationBase.DeserializeArray<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),");
                            if (nullable)
                                sbSe.Append(@$"
            if(self.{element.Name} != default)
            {{");
                            sbSe.Append(@$"
            self.{element.Name}.Length.Serialize(stream);");

                            if (!info.Nullable && _primitives.Contains(symbol))
                                sbSe.Append(@$"
            {symbol}Serialization.SerializeArray(self.{element.Name}, stream);");
                            else
                                sbSe.Append(info.Nullable
                                    ? info.ValueType
                                        ? @$"
            SerializationBase.SerializeArrayValueNullable<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);"
                                        : @$"
            SerializationBase.SerializeArrayNullable<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);"
                                    : @$"
            SerializationBase.SerializeArray<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);");
                            if (nullable)
                                sbSe.Append(@"
            }");
                            break;
                        }
                        case MemberKind.HashSet:
                        {
                            var info = elementInfo[0];
                            symbol = info.Type.ToString().TrimEnd('?');
                            sbDe.Append(info.Nullable
                                ? info.ValueType
                                    ? $"SerializationBase.DeserializeHashSetValueNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),"
                                    : $"SerializationBase.DeserializeHashSetNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),"
                                : $"SerializationBase.DeserializeHashSet<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),");
                            if (nullable)
                                sbSe.Append(@$"
            if(self.{element.Name} != default)
            {{");
                            sbSe.Append(@$"
            self.{element.Name}.Count.Serialize(stream);");
                            sbSe.Append(info.Nullable
                                ? info.ValueType
                                    ? @$"
            SerializationBase.SerializeHashSetValueNullable<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);"
                                    : @$"
            SerializationBase.SerializeHashSetNullable<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);"
                                : @$"
            SerializationBase.SerializeHashSet<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);");
                            if (nullable)
                                sbSe.Append(@"
            }");
                            break;
                        }
                        case MemberKind.List:
                        {
                            var info = elementInfo[0];
                            symbol = info.Type.ToString().TrimEnd('?');
                            sbDe.Append(info.Nullable
                                ? info.ValueType
                                    ? $"SerializationBase.DeserializeListValueNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),"
                                    : $"SerializationBase.DeserializeListNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),"
                                : $"SerializationBase.DeserializeList<{symbol}>(stream, intSerialization.Deserialize(stream), {symbol}Serialization.Deserialize),");
                            if (nullable)
                                sbSe.Append(@$"
            if(self.{element.Name} != default)
            {{");
                            sbSe.Append(@$"
            self.{element.Name}.Count.Serialize(stream);");
                            sbSe.Append(info.Nullable
                                ? info.ValueType
                                    ? @$"
            SerializationBase.SerializeListValueNullable<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);"
                                    : @$"
            SerializationBase.SerializeListNullable<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);"
                                : @$"
            SerializationBase.SerializeList<{symbol}>(self.{element.Name}, stream, {symbol}Serialization.Serialize);");
                            if (nullable)
                                sbSe.Append(@"
            }");
                            break;
                        }
                        case MemberKind.Dictionary:
                        {
                            var info = elementInfo[1];
                            symbol = info.Type.ToString().TrimEnd('?');
                            var keyInfo = elementInfo[0];
                            string keySymbol = keyInfo.Type.ToString();
                            sbDe.Append(info.Nullable
                                ? info.ValueType
                                    ? $"SerializationBase.DeserializeDictionaryValueNullable<{keySymbol}, {symbol}>(stream, intSerialization.Deserialize(stream), {keySymbol}Serialization.Deserialize, {symbol}Serialization.Deserialize),"
                                    : $"SerializationBase.DeserializeDictionaryNullable<{keySymbol}, {symbol}>(stream, intSerialization.Deserialize(stream), {keySymbol}Serialization.Deserialize, {symbol}Serialization.Deserialize),"
                                : $"SerializationBase.DeserializeDictionary<{keySymbol}, {symbol}>(stream, intSerialization.Deserialize(stream), {keySymbol}Serialization.Deserialize, {symbol}Serialization.Deserialize),");
                            if (nullable)
                                sbSe.Append(@$"
            if(self.{element.Name} != default)
            {{");
                            sbSe.Append(@$"
            self.{element.Name}.Count.Serialize(stream);");
                            sbSe.Append(info.Nullable
                                ? info.ValueType
                                    ? @$"
            SerializationBase.SerializeDictionaryValueNullable<{keySymbol}, {symbol}>(self.{element.Name}, stream, {keySymbol}Serialization.Serialize, {symbol}Serialization.Serialize);"
                                    : @$"
            SerializationBase.SerializeDictionaryNullable<{keySymbol}, {symbol}>(self.{element.Name}, stream, {keySymbol}Serialization.Serialize, {symbol}Serialization.Serialize);"
                                : @$"
            SerializationBase.SerializeDictionary<{keySymbol}, {symbol}>(self.{element.Name}, stream, {keySymbol}Serialization.Serialize, {symbol}Serialization.Serialize);");
                            if (nullable)
                                sbSe.Append(@"
            }");
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                sbDe.Append(@$"
            }};
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize(this {id} self, Stream stream)
        {{{sbSe}
        }}
    }}");
                if (namespaceName != null)
                    sbDe.Append(@"
}");
                context.AddSource($"{name}Serialization.cs", sbDe.ToString());
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

        private enum MemberKind
        {
            Unsupported,
            Plain,
            Array,
            HashSet,
            List,
            Dictionary
        }

        private struct ElementInfo
        {
            public ITypeSymbol Type;
            public bool Nullable;
            public bool ValueType;
        }

        private static void GetInfo(SemanticModel semanticModel, TypeSyntax typeSyntax,
            out MemberKind kind, out bool nullable, out List<ElementInfo> elements)
        {
            var info = semanticModel.GetTypeInfo(typeSyntax);
            nullable = typeSyntax is NullableTypeSyntax ||
                       info.Type!.NullableAnnotation == NullableAnnotation.Annotated;
            ITypeSymbol type = nullable
                               && info.Nullability.Annotation != NullableAnnotation.Annotated
                               && info.Type is INamedTypeSymbol nts
                               && nts.Name == "Nullable"
                               && nts.ContainingNamespace.Name == "System"
                               && nts.TypeArguments.Length != 0
                ? nts.TypeArguments[0]
                : info.Type!;
            elements = new List<ElementInfo>();
            if (type.TypeKind == TypeKind.Array)
            {
                var elementType = (type as IArrayTypeSymbol)!.ElementType;
                elements.Add(new ElementInfo
                {
                    Type = elementType,
                    Nullable = elementType.NullableAnnotation == NullableAnnotation.Annotated,
                    ValueType = elementType.IsValueType
                });
                kind = MemberKind.Array;
                return;
            }

            if (type is INamedTypeSymbol named)
            {
                var args = named.TypeArguments;
                if (args.Length == 0)
                {
                    kind = MemberKind.Plain;
                    return;
                }

                foreach (var arg in args)
                {
                    elements.Add(new ElementInfo
                    {
                        Type = arg,
                        Nullable = arg.NullableAnnotation == NullableAnnotation.Annotated,
                        ValueType = arg.IsValueType
                    });
                }

                kind = named.Name switch
                {
                    "HashSet"
                        when named.ContainingNamespace.ToString() == "System.Collections.Generic"
                        => MemberKind.HashSet,
                    "List"
                        when named.ContainingNamespace.ToString() == "System.Collections.Generic"
                        => MemberKind.List,
                    "Dictionary"
                        when named.ContainingNamespace.ToString() == "System.Collections.Generic"
                        => MemberKind.Dictionary,
                    _ => MemberKind.Unsupported
                };
                return;
            }

            kind = MemberKind.Unsupported;
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
