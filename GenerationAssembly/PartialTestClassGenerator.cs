using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GenerationAssembly
{
    [Generator]
    public class PartialTestClassGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new TestSubjectSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = (TestSubjectSyntaxReceiver) context.SyntaxReceiver;

            var fields = receiver?.FieldDeclarations;
            if (fields is null)
                return;

            foreach (var field in fields)
            {
                GeneratePartialTestClass(context, field);
            }
        }

        /// <summary>
        /// Generates a partial test class, instantiating the test subject,
        /// and generating mocks for the required constructor parameters.
        /// Additionally generates convenience methods for setting up the test mocks.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="field"></param>
        private void GeneratePartialTestClass(GeneratorExecutionContext context, FieldDeclarationSyntax field)
        {
            var fieldType = field.Declaration.Type;
            var semanticModel = context.Compilation.GetSemanticModel(fieldType.SyntaxTree);
            
            var testSubjectVariableDeclaration = GetTestSubjectVariableDeclaration(semanticModel, field);
            
            // Generate text components required for partial class
            var usingStatements = GetUsingStatements();
            var namespaceName = GetNamespaceName(context);
            var className = GetClassName(testSubjectVariableDeclaration);
            var fieldDeclarations = GetFieldDeclarations();
            var constructorInstantiation = GetConstructorInstantiation();
            var setupMethods = GetSetupMethods();

            // Generate partial class
            var sourceText = GenerateSourceText(
                usingStatements,
                namespaceName,
                className,
                fieldDeclarations,
                constructorInstantiation,
                setupMethods
            );

            context.AddSource($"{className}.generated.cs", sourceText);
        }

        private ISymbol GetTestSubjectVariableDeclaration(SemanticModel semanticModel, FieldDeclarationSyntax field)
        {
            if (field.Declaration.Variables.Count > 1)
                throw new Exception("Encountered more than one variable for field declaration");
            
            var result = semanticModel.GetDeclaredSymbol(field.Declaration.Variables.First());

            if (result is null)
                throw new Exception("Unable to obtain test subject variable declaration");

            return result;
        }

        private string GetUsingStatements()
        {
            return "";
        }

        private string GetNamespaceName(GeneratorExecutionContext context)
            => context.Compilation.AssemblyName;

        private string GetClassName(ISymbol testSubjectVariableDeclaration)
            => testSubjectVariableDeclaration.ContainingType.Name;

        private string GetFieldDeclarations()
        {
            return "";
        }

        private string GetConstructorInstantiation()
        {
            return "";
        }

        private string GetSetupMethods()
        {
            return "";
        }

        private SourceText GenerateSourceText(
            string usingStatements,
            string namespaceName,
            string className,
            string fieldDeclarations,
            string constructorInstantiation,
            string setupMethods
        )
        {
            return SourceText.From($@"// This file is auto-generated
using System;
using Moq;

{usingStatements}

namespace {namespaceName};

{fieldDeclarations}

public partial class {className}
{{
    public {className}()
    {{
        {constructorInstantiation}
    }}

    {setupMethods}
}}
", Encoding.UTF8);
        }
    }
}