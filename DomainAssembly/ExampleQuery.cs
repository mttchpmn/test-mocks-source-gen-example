namespace DomainAssembly;

public interface IExampleQuery
{
    string GetDataById(int id);

    int GetIdForKey(string key);
}

public class ExampleQuery : IExampleQuery
{
    public string GetDataById(int id)
    {
        throw new NotImplementedException();
    }

    public int GetIdForKey(string key)
    {
        throw new NotImplementedException();
    }
}