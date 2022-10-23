using DomainAssembly;
using Xunit;

namespace UnitTestAssembly;

public partial class ExampleServiceTests
{
   [TestSubject] // <-- This attribute triggers generation of partial test class
   private ExampleService _testSubject;

   public ExampleServiceTests()
   {
      
   }

   [Fact]
   public void ExampleTestMethod()
   {
   }
}