using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Azura.Generator
{
    [Generator]
    public class SerializationGenerator : IIncrementalGenerator
    {
        const string AzuraAttributeName = "Azura.AzuraAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext incrementalContext)
        {
            var providerInfos = incrementalContext.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null);


            var compilationProviderInfos
                = incrementalContext
                    .CompilationProvider
                    .Combine(providerInfos.Collect());

            incrementalContext.RegisterSourceOutput(compilationProviderInfos, Execute);
        }

        static bool IsSyntaxTargetForGeneration(SyntaxNode node)
            => (node is TypeDeclarationSyntax s && s.AttributeLists.Count > 0);

        static RequestItem? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            if (context.Node is not TypeDeclarationSyntax tds)
            {
                return null;
            }

            foreach (AttributeSyntax x in tds.AttributeLists.SelectMany(v => v.Attributes))
            {
                if (x.Name.ToString() != nameof(Azura))
                {
                    continue;
                }
                var items = new List<RequestElementItem>();
                foreach (var y in tds.Members)
                {
                    if (y is PropertyDeclarationSyntax pds)
                    {
                        foreach (var z in y.AttributeLists.SelectMany(v => v.Attributes))
                        {
                            if (z.Name.ToString() == nameof(Azura))
                            {
                                items.Add(new RequestElementItem(y, z, pds.Type, pds.Identifier));
                                break;
                            }
                        }
                    }

                    if (y is not FieldDeclarationSyntax fds)
                    {
                        continue;
                    }
                    foreach (var z in y.AttributeLists.SelectMany(v => v.Attributes))
                    {
                        if (z.Name.ToString() == nameof(Azura))
                        {
                            foreach (VariableDeclaratorSyntax v in fds.Declaration.Variables)
                                items.Add(new RequestElementItem(y, z, fds.Declaration.Type,
                                    v.Identifier));
                            break;
                        }
                    }
                }

                return new RequestItem(tds, x, items);
            }

            return null;
        }


        public void Execute(SourceProductionContext sourceProductionContext, (Compilation Compilation, ImmutableArray<RequestItem> Requests) source)
        {
            var compilation = source.Compilation;

            foreach (var item in source.Requests)
            {
                var sem = compilation.GetSemanticModel(item.Attribute.SyntaxTree);
                string attr = ModelExtensions.GetSymbolInfo(sem, item.Attribute).Symbol!.ContainingSymbol.ToString();
                if (attr != AzuraAttributeName)
                    return;
                string name = item.Type.GetFullyQualifiedName();
                bool valueType = item.Type is StructDeclarationSyntax;
                bool refConstructor = item.Type.Modifiers.Any(SyntaxKind.PartialKeyword);
                if (item.Type.TryGetParentSyntax(out BaseTypeDeclarationSyntax _))
                {
                    // This type has a type in the parent hierarchy, which means this is a nested type
                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("CT0001", "Serialized type cannot be a nested type",
                            $"The Azura library does not support generating serialization sources for nested types (failed on {name}). Please move the type to a namespace's scope.",
                            "Azura.Generator", DiagnosticSeverity.Error, true), null));
                    continue;
                }

                string id = item.Type.Identifier.ToString();
                string? namespaceName = item.Type.GetParentName();
                if (string.IsNullOrWhiteSpace(namespaceName))
                    namespaceName = null;
                var elements = new List<RequestElementItem>(item.Elements);
                elements.RemoveAll(e =>
                    ModelExtensions.GetSymbolInfo(sem, e.Attribute).Symbol?.ContainingSymbol.ToString() !=
                    AzuraAttributeName);
                var sbDe = new StringBuilder();
                var sbSe = new StringBuilder();
                var sbEx = new StringBuilder();
                foreach (var element in elements)
                {
                    GetInfo(sem, element.Type, out MemberKind kind, out bool nullable, out var elementInfo);
                    if (kind == MemberKind.Unsupported)
                    {
                        sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor("CT0002", "Unsupported generic type",
                                $"The Azura library does not support generating serialization sources for this type (failed on {name}:{element.Type} {ModelExtensions.GetTypeInfo(sem, element.Type).Type}).",
                                "Azura.Generator", DiagnosticSeverity.Error, true), null));
                        return;
                    }

                    string symbol = $"{ModelExtensions.GetSymbolInfo(sem, element.Type).Symbol}";
                    if (kind == MemberKind.Array)
                        symbol = symbol.TrimEnd('?').TrimEnd('[', ']');
                    symbol = symbol.TrimEnd('?');
                    if (nullable)
                        sbSe.Append($@"
            boolSerialization.Serialize((self.{element.Name} != null), stream);");
                    bool useMemberRef = false;
                    string deStr;
                    switch (kind)
                    {
                        case MemberKind.Plain:
                            {
                                var elementTypeInfo = ModelExtensions.GetTypeInfo(sem, element.Type).Type;
                                bool elementValueType = elementTypeInfo is { IsValueType: true };
                                // Prims don't have much to gain, just ignore these
                                var (deser, ser, ex) =
                                    CreateSerializerPair(elementTypeInfo, symbol, name, out bool elementEnum);
                                useMemberRef = !_primitives.Contains(symbol) &&
                                               element.Member is not PropertyDeclarationSyntax && !elementEnum && !nullable;
                                if (ex != null)
                                    sbEx.Append(ex);
                                if (useMemberRef && refConstructor)
                                {
                                    deStr = $"{deser}(stream, out this.{element.Name})";
                                }
                                else
                                    deStr = $"{deser}(stream)";

                                if (useMemberRef)
                                    sbSe.Append(elementValueType
                                        ? nullable ? @$"
            if(self.{element.Name} != null) {ser}(self.{element.Name}.Value, stream);"
                                        : @$"
            {ser}(in self.{element.Name}, stream);"
                                        : nullable
                                            ? @$"
            if(self.{element.Name} != null) {ser}(in self.{element.Name}, stream);"
                                            : @$"
            {ser}(in self.{element.Name}, stream);");
                                else
                                    sbSe.Append(elementValueType
                                        ? nullable ? @$"
            if(self.{element.Name} != null) {ser}(self.{element.Name}.Value!, stream);"
                                        : @$"
            {ser}(self.{element.Name}, stream);"
                                        : nullable
                                            ? @$"
            if(self.{element.Name} != null) {ser}(self.{element.Name}, stream);"
                                            : @$"
            {ser}(self.{element.Name}, stream);");

                                break;
                            }
                        case MemberKind.Array:
                            {
                                var info = elementInfo[0];
                                var (deser, ser, ex) = CreateSerializerPair(info.Type, symbol, name, out _);
                                if (ex != null)
                                    sbEx.Append(ex);
                                if (!info.Nullable && _primitives.Contains(symbol))
                                    deStr =
                                        $"{symbol}Serialization.DeserializeArray(stream, intSerialization.Deserialize(stream))";
                                else
                                    deStr = info.Nullable
                                        ? info.ValueType
                                            ? $"SerializationBase.DeserializeArrayValueNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {deser})"
                                            : $"SerializationBase.DeserializeArrayNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {deser})"
                                        : $"SerializationBase.DeserializeArray<{symbol}>(stream, intSerialization.Deserialize(stream), {deser})";
                                if (nullable)
                                    sbSe.Append(@$"
            if(self.{element.Name} != default)
            {{");
                                sbSe.Append(@$"
            intSerialization.Serialize(self.{element.Name}.Length, stream);");

                                if (!info.Nullable && _primitives.Contains(symbol))
                                    sbSe.Append(@$"
            {symbol}Serialization.SerializeArray(self.{element.Name}, stream);");
                                else
                                    sbSe.Append(info.Nullable
                                        ? info.ValueType
                                            ? @$"
            SerializationBase.SerializeArrayValueNullable<{symbol}>(self.{element.Name}, stream, {ser});"
                                            : @$"
            SerializationBase.SerializeArrayNullable<{symbol}>(self.{element.Name}, stream, {ser});"
                                        : @$"
            SerializationBase.SerializeArray<{symbol}>(self.{element.Name}, stream, {ser});");
                                if (nullable)
                                    sbSe.Append(@"
            }");
                                break;
                            }
                        case MemberKind.HashSet:
                            {
                                var info = elementInfo[0];
                                symbol = info.Type.ToString().TrimEnd('?');
                                var (deser, ser, ex) = CreateSerializerPair(info.Type, symbol, name, out _);
                                if (ex != null)
                                    sbEx.Append(ex);
                                deStr = info.Nullable
                                    ? info.ValueType
                                        ? $"SerializationBase.DeserializeHashSetValueNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {deser})"
                                        : $"SerializationBase.DeserializeHashSetNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {deser})"
                                    : $"SerializationBase.DeserializeHashSet<{symbol}>(stream, intSerialization.Deserialize(stream), {deser})";
                                if (nullable)
                                    sbSe.Append(@$"
            if(self.{element.Name} != default)
            {{");
                                sbSe.Append(@$"
            intSerialization.Serialize(self.{element.Name}.Count, stream);");
                                sbSe.Append(info.Nullable
                                    ? info.ValueType
                                        ? @$"
            SerializationBase.SerializeHashSetValueNullable<{symbol}>(self.{element.Name}, stream, {ser});"
                                        : @$"
            SerializationBase.SerializeHashSetNullable<{symbol}>(self.{element.Name}, stream, {ser});"
                                    : @$"
            SerializationBase.SerializeHashSet<{symbol}>(self.{element.Name}, stream, {ser});");
                                if (nullable)
                                    sbSe.Append(@"
            }");
                                break;
                            }
                        case MemberKind.List:
                            {
                                var info = elementInfo[0];
                                symbol = info.Type.ToString().TrimEnd('?');
                                var (deser, ser, ex) = CreateSerializerPair(info.Type, symbol, name, out _);
                                if (ex != null)
                                    sbEx.Append(ex);
                                deStr = info.Nullable
                                    ? info.ValueType
                                        ? $"SerializationBase.DeserializeListValueNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {deser})"
                                        : $"SerializationBase.DeserializeListNullable<{symbol}>(stream, intSerialization.Deserialize(stream), {deser})"
                                    : $"SerializationBase.DeserializeList<{symbol}>(stream, intSerialization.Deserialize(stream), {deser})";
                                if (nullable)
                                    sbSe.Append(@$"
            if(self.{element.Name} != default)
            {{");
                                sbSe.Append(@$"
            intSerialization.Serialize(self.{element.Name}.Count, stream);");
                                sbSe.Append(info.Nullable
                                    ? info.ValueType
                                        ? @$"
            SerializationBase.SerializeListValueNullable<{symbol}>(self.{element.Name}, stream, {ser});"
                                        : @$"
            SerializationBase.SerializeListNullable<{symbol}>(self.{element.Name}, stream, {ser});"
                                    : @$"
            SerializationBase.SerializeList<{symbol}>(self.{element.Name}, stream, {ser});");
                                if (nullable)
                                    sbSe.Append(@"
            }");
                                break;
                            }
                        case MemberKind.Dictionary:
                            {
                                var info = elementInfo[1];
                                symbol = info.Type.ToString().TrimEnd('?');
                                var (deser, ser, ex) = CreateSerializerPair(info.Type, symbol, name, out _);
                                if (ex != null)
                                    sbEx.Append(ex);
                                var keyInfo = elementInfo[0];
                                string keySymbol = keyInfo.Type.ToString();
                                var (kdeser, kser, ex2) = CreateSerializerPair(keyInfo.Type, keySymbol, name, out _);
                                if (ex != null)
                                    sbEx.Append(ex2);
                                deStr = info.Nullable
                                    ? info.ValueType
                                        ? $"SerializationBase.DeserializeDictionaryValueNullable<{keySymbol}, {symbol}>(stream, intSerialization.Deserialize(stream), {kdeser}, {deser})"
                                        : $"SerializationBase.DeserializeDictionaryNullable<{keySymbol}, {symbol}>(stream, intSerialization.Deserialize(stream), {kdeser}, {deser})"
                                    : $"SerializationBase.DeserializeDictionary<{keySymbol}, {symbol}>(stream, intSerialization.Deserialize(stream), {kdeser}, {deser})";
                                if (nullable)
                                    sbSe.Append(@$"
            if(self.{element.Name} != default)
            {{");
                                sbSe.Append(@$"
            intSerialization.Serialize(self.{element.Name}.Count, stream);");
                                sbSe.Append(info.Nullable
                                    ? info.ValueType
                                        ? @$"
            SerializationBase.SerializeDictionaryValueNullable<{keySymbol}, {symbol}>(self.{element.Name}, stream, {kser}, {ser});"
                                        : @$"
            SerializationBase.SerializeDictionaryNullable<{keySymbol}, {symbol}>(self.{element.Name}, stream, {kser}, {ser});"
                                    : @$"
            SerializationBase.SerializeDictionary<{keySymbol}, {symbol}>(self.{element.Name}, stream, {kser}, {ser});");
                                if (nullable)
                                    sbSe.Append(@"
            }");
                                break;
                            }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (refConstructor)
                    {
                        sbDe.Append(useMemberRef
                            ? @$"
            {deStr};"
                            : nullable
                                ? @$"
            this.{element.Name} = boolSerialization.Deserialize(stream) ? {deStr} : null;"
                                : @$"
            this.{element.Name} = {deStr};");
                    }
                    else
                    {
                        sbDe.Append(@$"
                {element.Name} = ");
                        if (nullable)
                            sbDe.Append("!boolSerialization.Deserialize(stream) ? null : ");
                        sbDe.Append($"{deStr},");
                    }
                }

                var sbMain = new StringBuilder(@$"
#pragma warning disable 1591
#nullable enable
using {nameof(Azura)};
using System;
using System.IO;
using System.Runtime.CompilerServices;");
                if (namespaceName != null)
                    sbMain.Append(@$"
namespace {namespaceName}
{{");
                string fullDe = refConstructor
                    ? @$"new {name}(new {nameof(AzuraContext)}(stream))"
                    : @$"new {name} {{{sbDe}
            }}";
                sbMain.Append(@$"
    public static class {id}Serialization
    {{
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static {name} Deserialize(Stream stream)
        {{
            return {fullDe};
        }}");
                sbMain.Append(@$"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Deserialize(Stream stream, out {name} self)
        {{
            self = {fullDe};
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sbMain.Append(valueType
                    ? @$"
        public static void Serialize({id} self, Stream stream)
        {{{sbSe}
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static void Serialize(this in {id} self, Stream stream)
        {{{sbSe}
        }}

        {sbEx}
    }}"
                    : @$"
        public static void Serialize(this {id} self, Stream stream)
        {{{sbSe}
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Serialize(in {id} self, Stream stream)
        {{{sbSe}
        }}

        {sbEx}
    }}");
                if (namespaceName != null)
                    sbMain.Append(@"
}");
                sourceProductionContext.AddSource($"{name}Serialization.cs", sbMain.ToString());
                if (refConstructor)
                {
                    string type = item.Type.Kind() switch
                    {
                        SyntaxKind.ClassDeclaration => "class",
                        SyntaxKind.StructDeclaration => "struct",
                        SyntaxKind.RecordDeclaration => "record",
                        SyntaxKind.RecordStructDeclaration => "record struct",
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    var sbRef = new StringBuilder(@$"
#pragma warning disable 1591
#nullable enable
using {nameof(Azura)};
using System;
using System.IO;");
                    if (namespaceName != null)
                        sbRef.Append(@$"
namespace {namespaceName}
{{");
                    sbRef.Append(@$"
    public partial {type} {id}
    {{
        public {id}(AzuraContext context)
        {{
            Stream stream = context.Stream;{sbDe}
        }}
    }}");
                    if (namespaceName != null)
                        sbRef.Append(@"
}");
                    sourceProductionContext.AddSource($"{name}.Generated.cs", sbRef.ToString());
                }
            }
        }

        private (string deser, string ser, string? helperMethods) CreateSerializerPair(ITypeSymbol? symbol,
            string symbolText, string name, out bool isEnum)
        {
            if (symbol is INamedTypeSymbol rsymbol && rsymbol.Name == nameof(Nullable) &&
                rsymbol.ContainingNamespace.Name == nameof(System))
                symbol = rsymbol.TypeArguments[0]; // Get value type type from nullable struct
            if (symbol?.TypeKind == TypeKind.Enum)
            {
                isEnum = true;
                string ek = (symbol as INamedTypeSymbol)!.EnumUnderlyingType!.ToString();
                string gk = Guid.NewGuid().ToString().Replace('-', '_');
                return ($"{name}Serialization.Deserialize_{gk}",
                    $"{name}Serialization.Serialize_{gk}",
                    @$"
        internal static {symbol} Deserialize_{gk}(Stream stream) => ({symbolText}){ek}Serialization.Deserialize(stream);
        internal static {symbol} Deserialize_{gk}(Stream stream, out {symbolText} self) => self = Deserialize_{gk}(stream);
        internal static void Serialize_{gk}({symbolText} self, Stream stream) => Serialize_{gk}(in self, stream);
        internal static void Serialize_{gk}(in {symbolText} self, Stream stream) => {ek}Serialization.Serialize(({ek})self, stream);");
            }

            isEnum = false;
            return ($"{symbolText}Serialization.Deserialize", $"{symbolText}Serialization.Serialize", null);
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
            "double",
            "char",
            "bool",
            "System.Guid",
            "System.DateTime",
            "System.TimeSpan",
            "decimal"
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
            var info = ModelExtensions.GetTypeInfo(semanticModel, typeSyntax);
            nullable = typeSyntax is NullableTypeSyntax ||
                       info.Type!.NullableAnnotation == NullableAnnotation.Annotated;
            ITypeSymbol type = nullable
                               && info.Nullability.Annotation != NullableAnnotation.Annotated
                               && info.Type is INamedTypeSymbol nts
                               && nts.Name == nameof(Nullable)
                               && nts.ContainingNamespace.Name == nameof(System)
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

            if (type is not INamedTypeSymbol named)
            {
                kind = MemberKind.Unsupported;
            }
            else
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
        }

        public record RequestItem(TypeDeclarationSyntax Type, AttributeSyntax Attribute,
            List<RequestElementItem> Elements);

        public record RequestElementItem(MemberDeclarationSyntax Member, AttributeSyntax Attribute, TypeSyntax Type,
            SyntaxToken Name);
    }
}
