using DomainAssembly;
using Moq;
using Xunit;

namespace UnitTestAssembly;

public partial class ExampleServiceTests
{
   [TestSubject] // <-- This attribute triggers generation of partial test class
   private ExampleService _testSubject;

   [Fact]
   public void ExampleTestMethod()
   {
      SetupDeleteDataForId(true);
   }
}