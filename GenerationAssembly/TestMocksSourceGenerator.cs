using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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
                var fieldType = field.Declaration.Type;
                var semanticModel = context.Compilation.GetSemanticModel(fieldType.SyntaxTree);
                var typeInfo = semanticModel.GetTypeInfo(fieldType);
                var namedTypeSymbol = typeInfo.Type as INamedTypeSymbol;

                var variableDeclaration =
                    semanticModel.GetDeclaredSymbol(field.Declaration.Variables.First()); // TODO - Handle enumeration

                var variableName = variableDeclaration.Name;
                var className = variableDeclaration.ContainingType.Name;
                var assemblyName = context.Compilation.AssemblyName;

                var constructors = namedTypeSymbol.Constructors;
                var firstConstructor = constructors.First(); // TODO - Handle enumeration
                var parameters = firstConstructor.Parameters;

                var firstParam = parameters.First().Type;

                // TODO:
                // - Using statements
                // - Variable name of field declaration

                var testClass = SourceText.From(
                    $@"using DomainAssembly;

namespace {assemblyName};

public partial class {className}
{{
    private Mock<{firstParam}> _exampleQuery = new();

    public ExampleServiceTests()
    {{
        {variableName} = new {namedTypeSymbol.Name}(_exampleQuery.Object)
    }}
}}
", Encoding.UTF8);

                context.AddSource("PartialClass.g.cs", testClass);


                var sourceText = SourceText.From($@"
/*
NAMED TYPE SYMBOL: {namedTypeSymbol.Name}
CONSTR: {firstConstructor}
PARAMS: {parameters.First()}

var testSubject = new {namedTypeSymbol.Name}()


PARAM TYPE: {firstParam}
*/
", Encoding.UTF8);

                context.AddSource("Diagnostics.g.cs", sourceText);
            }
        }
    }
}