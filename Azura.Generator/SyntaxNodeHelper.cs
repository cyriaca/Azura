using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Azura.Generator
{
    // https://stackoverflow.com/a/23249021 + nullable + convenience
    internal static class SyntaxNodeHelper
    {
        public static string GetFullyQualifiedName(this BaseTypeDeclarationSyntax cds)
        {
            string? pName = GetParentName(cds);
            return pName != null ? $"{pName}.{cds.Identifier}" : cds.Identifier.ToString();
        }

        public static string? GetParentName(this MemberDeclarationSyntax mds)
        {
            if (!mds.TryGetParentSyntax(out MemberDeclarationSyntax? p)) return null;
            string pName = p switch
            {
                FileScopedNamespaceDeclarationSyntax fsNds => fsNds.Name.ToString(),
                NamespaceDeclarationSyntax nds => nds.Name.ToString(),
                BaseTypeDeclarationSyntax cds => cds.Identifier.ToString(),
                _ => throw new IOException($"Unexpected parent type {p?.GetType()}")
            };
            string? ppName = GetParentName(p);
            return ppName != null ? $"{ppName}.{pName}" : pName;
        }

        public static string GetFullyQualifiedName(this MethodDeclarationSyntax mds)
        {
            return $"{(mds.Parent as BaseTypeDeclarationSyntax)!.GetFullyQualifiedName()}.{mds.Identifier}";
        }

        public static bool TryGetParentSyntax<T>(this SyntaxNode? syntaxNode, out T? result)
            where T : SyntaxNode
        {
            // set defaults
            result = null;
            if (syntaxNode == null) return false;
            try
            {
                syntaxNode = syntaxNode.Parent;
                if (syntaxNode == null) return false;
                if (syntaxNode is T t)
                {
                    result = t;
                    return true;
                }

                return TryGetParentSyntax(syntaxNode, out result);
            }
            catch
            {
                return false;
            }
        }
    }
}
