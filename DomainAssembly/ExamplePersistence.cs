namespace DomainAssembly;

public interface IExamplePersistence
{
    void SaveDataForId(int id, string data);
}

public class ExamplePersistence : IExamplePersistence
{
    public void SaveDataForId(int id, string data)
    {
        throw new NotImplementedException();
    }
}