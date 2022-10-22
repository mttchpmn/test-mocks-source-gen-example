using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GenerationAssembly
{
    [Generator]
    public class TestMocksSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new GenerateMocksAttributeSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = (GenerateMocksAttributeSyntaxReceiver) context.SyntaxReceiver;

            var fields = receiver?.FieldDeclarations;
            if (fields is null)
                return;

            foreach (var field in fields)
            {
                GeneratePartialClass(context, field);
            }
        }

        private void GeneratePartialClass(GeneratorExecutionContext context, FieldDeclarationSyntax field)
        {
            var fieldType = field.Declaration.Type;
            var semanticModel = context.Compilation.GetSemanticModel(fieldType.SyntaxTree);
            var typeInfo = semanticModel.GetTypeInfo(fieldType);
            var namedTypeSymbol = typeInfo.Type as INamedTypeSymbol;

            var variableDeclaration = semanticModel
                    .GetDeclaredSymbol(field.Declaration.Variables
                    .First()); // TODO - Will there ever be more than 1?

            var variableName = variableDeclaration?.Name;
            var className = variableDeclaration?.ContainingType.Name;
            var assemblyName = context.Compilation.AssemblyName;

            var constructors = namedTypeSymbol?.Constructors;
            var firstConstructor = constructors?.First(); // TODO - How do we handle multiple constructors?
            var parameters = firstConstructor?.Parameters;

            var firstParam = parameters?.First().Type; // DomainAssembly.IExampleQuery

            var mock = firstParam as INamedTypeSymbol;
            var firstMethod = mock.GetMembers().First(); // DomainAssembly.IExampleQuery.GetDataById


            // TODO:
            // - Using statements
            // - Field names (i.e. _exampleQuery)
            // - Ensure class is partial
            // - Throw error / emit diagnostic when too many constructors etc

            var testClass = SourceText.From(
                $@"{GetUsingStatements()}

namespace {assemblyName};

public partial class {className}
{{
    {GetFieldDeclarations()}
    private Mock<{firstParam.Name}> _exampleQuery = new();

    public ExampleServiceTests()
    {{
        {variableName} = new {namedTypeSymbol.Name}(_exampleQuery.Object)
    }}

    public void Setup{firstMethod.Name}()
    {{
        // _exampleQuery.Setup(x => x.{firstMethod.Name}().Returns(""brocolli""))
    }}

}}
", Encoding.UTF8);

            context.AddSource($"{className}.g.cs", testClass);
        }

        private string GetFieldDeclarations()
        {
            return ""; // TODO
        }

        private string GetUsingStatements()
        {
            return "using DomainAssembly;"; // TODO
        }
    }
}