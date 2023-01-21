using System.Reflection.Emit;

namespace System.Reflection
{
    internal static class Globals
    {
        public static readonly Dictionary<int, object> Cache = new();

        public static readonly OpCode[] MultiByteOpCodes;
        public static readonly OpCode[] SingleByteOpCodes;

        static Globals()
        {
            MultiByteOpCodes = new OpCode[0x100];
            SingleByteOpCodes = new OpCode[0x100];
            FieldInfo[] infoArray1 = typeof(OpCodes).GetFields();
            for (int num1 = 0; num1 < infoArray1.Length; num1++)
            {
                FieldInfo info1 = infoArray1[num1];
                if (info1.FieldType == typeof(OpCode))
                {
                    OpCode code1 = (OpCode)info1.GetValue(null)!;
                    ushort num2 = (ushort)code1.Value;
                    if (num2 < 0x100)
                        SingleByteOpCodes[(int)num2] = code1;
                    else
                        MultiByteOpCodes[num2 & 0xff] = code1;
                }
            }
        }
        public static string? ProcessSpecialTypes(string? typeName)
        {
            switch (typeName)
            {
                case "System.string":
                case "System.String":
                case "String":
                    typeName = "string"; 
                    break;
                case "System.Int32":
                case "Int":
                case "Int32":
                    typeName = "int"; 
                    break;
            }
            return typeName;
        }
    }
}
