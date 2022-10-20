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

            // LOOK FOR 'DeclaringSyntaxReferences' 'GetMembers'

            foreach (var field in fields)
            {
                var fieldType = field.Declaration.Type;
                var semanticModel = context.Compilation.GetSemanticModel(fieldType.SyntaxTree);
                var typeInfo = semanticModel.GetTypeInfo(fieldType);

                var assemblyName = typeInfo.Type?.ContainingAssembly.Name;
                var namespaceName = typeInfo.Type?.ContainingNamespace.Name;
                var namedTypeSymbol = typeInfo.Type as INamedTypeSymbol;

                var constructors = namedTypeSymbol.Constructors;
                var firstConstructor = constructors.First();
                var parameters = firstConstructor.Parameters;

                var firstParam = parameters.First().Type;

                var testClass = SourceText.From($@"

using DomainAssembly;

namespace UnitTestAssembly;

public partial class ExampleServiceTests
{{
    private Mock<{firstParam}> _exampleQuery = new();

    public ExampleServiceTests()
    {{
        _testSubject = new {namedTypeSymbol.Name}(_exampleQuery.Object)
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