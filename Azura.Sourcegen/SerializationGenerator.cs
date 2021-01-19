using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Azura.Sourcegen
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
                            "Azura.Sourcegen", DiagnosticSeverity.Error, true), null));
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
                    string symbol = $"{sem.GetSymbolInfo(element.Type).Symbol}";
                    sb.Append(symbol.EndsWith("]")
                        ? @$"
                {element.Name} = {symbol.Substring(0, symbol.Length - 2)}Serialization.DeserializeArray(stream, intSerialization.Deserialize(stream)),"
                        : @$"
                {element.Name} = {symbol}Serialization.Deserialize(stream),");
                }

                sb.Append(@$"
            }};
        }}

        public static void Serialize(this {id} self, Stream stream)
        {{");
                foreach (var element in elements)
                {
                    string symbol = $"{sem.GetSymbolInfo(element.Type).Symbol}";
                    if (symbol.EndsWith("[]"))
                    {
                        sb.Append(@$"
            self.{element.Name}.Length.Serialize(stream);");
                        sb.Append(@$"
            self.{element.Name}.AsSpan().Serialize(stream);");
                    }
                    else
                        sb.Append(@$"
            self.{element.Name}.Serialize(stream);");
                }

                sb.Append(@$"
        }}

        public static {id}[] DeserializeArray(Stream stream, int count)
        {{
            {id}[] res = new {id}[count];
            for (int i = 0; i < count; i++) res[i] = Deserialize(stream);
            return res;
        }}

        public static void Serialize(this Span<{id}> self, Stream stream)
        {{
            for (int i = 0; i < self.Length; i++) self[i].Serialize(stream);
        }}

        public static void Serialize(this ReadOnlySpan<{id}> self, Stream stream)
        {{
            for (int i = 0; i < self.Length; i++) self[i].Serialize(stream);
        }}
    }}");
                if (namespaceName != null)
                    sb.Append(@"
}");
                context.AddSource($"{name}Serialization.cs", sb.ToString());
            }
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
