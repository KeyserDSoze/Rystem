using System.Reflection.Emit;
using System.Text;

namespace System.Reflection
{
    public sealed class ILInstruction
    {
        public OpCode Code { get; internal set; }
        public object? Operand { get; internal  set; }
        public byte[]? OperandData { get; internal set; }
        public int Offset { get; internal set; }

        /// <summary>
        /// Returns a friendly strign representation of this instruction
        /// </summary>
        /// <returns></returns>
        public string GetCode()
        {
            StringBuilder result = new();
            result.Append($"{GetExpandedOffset(Offset)} : {Code}");
            if (Operand != null)
            {
                switch (Code.OperandType)
                {
                    case OperandType.InlineField:
                        FieldInfo fOperand = ((FieldInfo)Operand);
                        result.Append($" {Globals.ProcessSpecialTypes(fOperand.FieldType.ToString())} {Globals.ProcessSpecialTypes(fOperand.ReflectedType?.ToString())}::{fOperand.Name}");
                        break;
                    case OperandType.InlineMethod:
                        if (Operand is MethodInfo mOperand)
                        {
                            result.Append(' ');
                            if (!mOperand.IsStatic)
                                result.Append("instance ");
                            result.Append($"{Globals.ProcessSpecialTypes(mOperand.ReturnType.ToString())} {Globals.ProcessSpecialTypes(mOperand.ReflectedType!.ToString())} :: {mOperand.Name}()");
                        }
                        else if (Operand is ConstructorInfo constructorInfo)
                        {
                            result.Append(' ');
                            if (!constructorInfo.IsStatic)
                                result.Append("instance ");
                            result.Append($"void {Globals.ProcessSpecialTypes(constructorInfo.ReflectedType!.ToString())} :: {constructorInfo.Name}()");
                        }
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.InlineBrTarget:
                        result.Append($" {GetExpandedOffset((int)Operand)}");
                        break;
                    case OperandType.InlineType:
                        result.Append($" {Globals.ProcessSpecialTypes(Operand.ToString())}");
                        break;
                    case OperandType.InlineString:
                        if (Operand.ToString() == "\r\n")
                            result.Append(" \"\\r\\n\"");
                        else result.Append($" \"{Operand}\"");
                        break;
                    case OperandType.ShortInlineVar:
                        result.Append(Operand);
                        break;
                    case OperandType.InlineI:
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineR:
                        result.Append(Operand);
                        break;
                    case OperandType.InlineTok:
                        if (Operand is Type type)
                            result.Append(type.FullName);
                        else
                            result.Append("not supported");
                        break;
                    default:
                        result.Append("not supported");
                        break;
                }
            }
            return result.ToString();
        }

        private static string GetExpandedOffset(long offset)
        {
            string result = offset.ToString();
            return new string('0', 4 - result.Length) + result;
        }
    }
}
