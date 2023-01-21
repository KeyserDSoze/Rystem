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
    }
}
