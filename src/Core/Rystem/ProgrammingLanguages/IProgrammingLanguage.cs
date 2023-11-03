namespace System.ProgrammingLanguage
{
    internal interface IProgrammingLanguage
    {
        string Start(Type type, string name);
        string GetMimeType();
        string SetProperty(string name, string type);
        string GetPrimitiveType(Type type);
        string GetNonPrimitiveType(Type type);
        string End();
        string ConvertEnum(string name, Type type);
    }
}
