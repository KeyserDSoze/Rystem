using System.Diagnostics;
using System.Reflection;

namespace Rystem.Reflection
{
    public static class ReflectionHelper
    {
        public static string NameOfCallingClass(int deep = 1, bool full = false)
        {
            string name;
            Type declaringType;
            int skipFrames = 1 + deep;
            do
            {
                MethodBase method = new StackFrame(skipFrames, false).GetMethod()!;
                declaringType = method.DeclaringType!;
                if (declaringType == default)
                    return method.Name;
                skipFrames++;
                name = full ? declaringType.FullName! : declaringType.Name!;
            }
            while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));
            return name;
        }
    }
}
