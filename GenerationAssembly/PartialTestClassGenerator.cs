using System;
using System.Collections.Generic;
using System.Linq;
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
            var semanticModel = GetSemanticModel(context, field);

            var testSubjectTypeSymbol = GetTestSubjectTypeSymbol(semanticModel, field);
            var testSubjectVariableDeclaration = GetTestSubjectVariableDeclaration(semanticModel, field);

            var testSubjectConstructorParameters = GetTestSubjectConstructorParameters(testSubjectTypeSymbol).ToList();

            // Generate text components required for partial class
            var namespaceName = GetNamespaceName(context);
            var usingStatements = GetUsingStatements(testSubjectConstructorParameters);
            var className = GetClassName(testSubjectVariableDeclaration);
            var fieldDeclarations = GetFieldDeclarations(testSubjectConstructorParameters);
            var constructorInstantiation =
                GetConstructorInstantiation(testSubjectVariableDeclaration, testSubjectTypeSymbol, testSubjectConstructorParameters);
            var helperMethods = GetHelperMethods(testSubjectConstructorParameters);

            // Generate partial class
            var sourceText = GenerateSourceText(
                usingStatements,
                namespaceName,
                className,
                fieldDeclarations,
                constructorInstantiation,
                helperMethods
            );

            context.AddSource($"{className}.generated.cs", sourceText);
        }

        private IEnumerable<IParameterSymbol> GetTestSubjectConstructorParameters(
            INamedTypeSymbol testSubjectVariableDeclaration)
        {
            var constructors = testSubjectVariableDeclaration.Constructors;

            if (constructors.Length > 1)
                throw new Exception("Encountered more than one constructor for test subject");

            return constructors.First().Parameters.ToList();
        }

        private SemanticModel GetSemanticModel(GeneratorExecutionContext context, FieldDeclarationSyntax field)
            => context.Compilation.GetSemanticModel(field.Declaration.Type.SyntaxTree);

        private INamedTypeSymbol GetTestSubjectTypeSymbol(SemanticModel semanticModel, FieldDeclarationSyntax field)
            => semanticModel.GetTypeInfo(field.Declaration.Type).Type as INamedTypeSymbol;

        private ISymbol GetTestSubjectVariableDeclaration(SemanticModel semanticModel, FieldDeclarationSyntax field)
        {
            if (field.Declaration.Variables.Count > 1)
                throw new Exception("Encountered more than one variable for field declaration");

            var result = semanticModel.GetDeclaredSymbol(field.Declaration.Variables.First());

            if (result is null)
                throw new Exception("Unable to obtain test subject variable declaration");

            return result;
        }

        private string GetUsingStatements(List<IParameterSymbol> testSubjectConstructorParameters)
        {
            var assemblies = testSubjectConstructorParameters.Select(GetAssemblyForParameter).Distinct();
            
            return string.Join("\n", assemblies);
        }

        private string GetAssemblyForParameter(IParameterSymbol parameter)
        {
            var assembly = parameter.ContainingAssembly;
                
            return $"using {assembly.Name};";
        }

        private string GetNamespaceName(GeneratorExecutionContext context)
            => context.Compilation.AssemblyName;

        private string GetClassName(ISymbol testSubjectVariableDeclaration)
            => testSubjectVariableDeclaration.ContainingType.Name;

        private string GetFieldDeclarations(IEnumerable<IParameterSymbol> testSubjectConstructorParameters)
        {
            var fields =
                testSubjectConstructorParameters.Select(x =>
                    $"private Mock<{x.Type.Name}> {GetFieldName(x.Type.Name)} = new();");

            return string.Join("\n\t", fields);
        }

        private string GetFieldName(string parameterName)
            => "_" + parameterName[1].ToString().ToLower() + parameterName.Substring(2);

        private string GetConstructorInstantiation(ISymbol testSubjectVariableDeclaration,
            INamedTypeSymbol testSubjectTypeSymbol,
            IEnumerable<IParameterSymbol> testSubjectConstructorParameters)
        {
            var parameters = testSubjectConstructorParameters.Select(x => $"{GetFieldName(x.Type.Name)}.Object");
            var parameterList = string.Join(", ", parameters);
            
            return $"{testSubjectVariableDeclaration.Name} = new {testSubjectTypeSymbol.Name}({parameterList});";
        }

        private string GetHelperMethods(IEnumerable<IParameterSymbol> testSubjectConstructorParameters)
        {
            var setupMethods = testSubjectConstructorParameters.Select(GenerateHelperMethodsForParameter);

            return string.Join("\n\n\t", setupMethods);
        }

        private string GenerateHelperMethodsForParameter(IParameterSymbol parameter)
        {
            var param = parameter.Type as INamedTypeSymbol;
            var availableMethods = param?.GetMembers().Select(x => x as IMethodSymbol);

            var setupMethods = availableMethods?.Where(x => !x.ReturnType.Name.Equals("Void")).Select(x => GenerateSetupMethod(parameter, x));
            var verifyMethods = availableMethods?.Select(x => GenerateVerifyMethod(parameter, x));
            
            var setupText = string.Join("\n\n\t", setupMethods);
            var verifyText = string.Join("\n\n\t", verifyMethods);

            return setupText + "\n\t" + verifyText;
        }

        private string GenerateSetupMethod(IParameterSymbol parameter, IMethodSymbol method)
        {
            var methodParameters = method.Parameters.Select(x => $"It.IsAny<{x.Type.Name}>()");
            var parametersText = string.Join(", ", methodParameters);

            var fieldName = GetFieldName(parameter.Type.Name);
            return $@"private void Setup{method.Name}({method.ReturnType.Name} returnValue)
    {{
       {fieldName}.Setup(x => x.{method.Name}({parametersText})).Returns(returnValue); 
    }}";
        }
        
        private string GenerateVerifyMethod(IParameterSymbol parameter, IMethodSymbol method)
        {
            var methodParameters = method.Parameters.Select(x => $"It.IsAny<{x.Type.Name}>()");
            var parametersText = string.Join(", ", methodParameters);

            var fieldName = GetFieldName(parameter.Type.Name);
            return $@"private void Verify{method.Name}(int timesCalled)
    {{  
       {fieldName}.Verify(x => x.{method.Name}({parametersText}), Times.Exactly(timesCalled));
    }}";
        }

        private SourceText GenerateSourceText(
            string usingStatements,
            string namespaceName,
            string className,
            string fieldDeclarations,
            string constructorInstantiation,
            string helperMethods
        )
        {
            return SourceText.From($@"// <Auto-generated>
using System;
using Moq;

{usingStatements}

namespace {namespaceName};

public partial class {className}
{{
    {fieldDeclarations}

    public {className}()
    {{
        {constructorInstantiation}
    }}

    {helperMethods}
}}
", Encoding.UTF8);
        }
    }
}