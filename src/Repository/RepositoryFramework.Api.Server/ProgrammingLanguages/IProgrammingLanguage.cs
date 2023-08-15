namespace RepositoryFramework
{
    internal interface IProgrammingLanguage
    {
        string Start(string name);
        string GetMimeType();
        string SetProperty(string name, string type);
        string GetPrimitiveType(Type type);
        string GetNonPrimitiveType(Type type);
        string End();
    }
}
