namespace DomainAssembly;

public interface IExampleQuery
{
    string GetDataById(int id);
}

public class ExampleQuery : IExampleQuery
{
    public string GetDataById(int id)
    {
        throw new NotImplementedException();
    }
}