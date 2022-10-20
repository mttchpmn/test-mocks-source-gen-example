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
                // var namespaceName = typeInfo.Type?.ContainingNamespace.Name;
                var namedTypeSymbol = typeInfo.Type as INamedTypeSymbol;

                var constructors = namedTypeSymbol.Constructors;
                var firstConstructor = constructors.First();
                var parameters = firstConstructor.Parameters;

                var sourceText = SourceText.From($@"
/*
NAMED TYPE SYMBOL: {namedTypeSymbol.Name}
CONSTR: {firstConstructor}
PARAMS: {parameters.First()}

var testSubject = new {namedTypeSymbol.Name}()
*/
", Encoding.UTF8);

                context.AddSource("FieldTest.g.cs", sourceText);
            }

            // var foo = fields?.Select(x => x.Span.ToString()).ToList();
            // var foo = fields?.Select(x => x.ToString());
//             var foo = fields?.Count.ToString();
//             var bar = String.Join(",", foo);
//
//             var fieldType = fields.First().Declaration.Type;
//
//             var semanticModel = context.Compilation.GetSemanticModel(fieldType.SyntaxTree);
//             var hmm = semanticModel.GetTypeInfo(fieldType);
//
//             var typeName = hmm.Type; // HelloSourceGenerator.MyCustomType
//             var namesp = hmm.Type.ContainingNamespace.Name;
//
//             var reflectionType = Type.GetType("HelloSourceGenerator.MyCustomType");
//
//             var st = SourceText.From($@"
// /*
// Count: {fields.Count}
// First: {fields.First()}
//
// Declaration: {fields.First().Declaration}
// Type: {fields.First().Declaration.Type}
// TypeInfo: {hmm.Type}
// TypeName: {hmm.Type.Name}
// Namespace: {namesp}
// ReflectionType: {reflectionType}
// */", Encoding.UTF8);


            // var sourceText = SourceText.From("/*" + fields.Count + fields.First().ToString() + "*/", Encoding.UTF8);
            //
            // context.AddSource("FieldTest.g.cs", st);
        }
    }
}