namespace System.Text.Minimization
{
    internal interface IMinimizationInterpreter
    {
        string Serialize(Type type, object value, int deep);
        dynamic Deserialize(Type type, string value, int deep);
        bool IsValid(Type type);
        int Priority { get; }
    }
}
