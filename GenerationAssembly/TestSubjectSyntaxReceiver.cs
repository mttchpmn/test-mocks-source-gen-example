using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenerationAssembly
{
    public class TestSubjectSyntaxReceiver : ISyntaxReceiver
    {
        public List<FieldDeclarationSyntax> FieldDeclarations { get; private set; } =
            new List<FieldDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (!(syntaxNode is FieldDeclarationSyntax fds) || !fds.AttributeLists.Any()) return;

            // At this point we have a declared field with at least one attribute
            foreach (var attributeListSyntax in fds.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (attributeSyntax.Name.ToString().Equals("TestSubject"))
                        FieldDeclarations.Add(fds);
                }
            }
        }
    }
}