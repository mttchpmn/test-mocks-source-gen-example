namespace DomainAssembly;

public interface IExamplePersistence
{
    void SaveDataForId(int id, string data);

    bool DeleteDataForId(int id);
}

public class ExamplePersistence : IExamplePersistence
{
    public void SaveDataForId(int id, string data)
    {
        throw new NotImplementedException();
    }

    public bool DeleteDataForId(int id)
    {
        throw new NotImplementedException();
    }
}