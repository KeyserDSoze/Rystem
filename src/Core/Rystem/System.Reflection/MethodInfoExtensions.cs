namespace System.Reflection
{
    public static class MethodInfoExtensions
    {
        public static string GetBodyAsString(this MethodInfo methodInfo)
        {
            MethodBodyReader mr = new(methodInfo);
            return mr.GetBodyCode();
        }
        public static List<ILInstruction> GetInstructions(this MethodInfo methodInfo)
        {
            MethodBodyReader mr = new(methodInfo);
            return mr.Instructions;
        }
        public static string GetSignature(this MethodInfo methodInfo)
            => $"{methodInfo.Name}_{methodInfo.ReturnParameter?.ParameterType?.FullName}-{string.Join('-', methodInfo.GetParameters().Select(x => x.ParameterType.FullName))}";
    }
}
