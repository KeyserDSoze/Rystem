using System.Reflection.Emit;
using System.Text;

namespace System.Reflection
{
    internal sealed class MethodBodyReader
    {
        public List<ILInstruction> Instructions { get; } = new();
        private readonly byte[] _ilCode;
        private readonly MethodInfo _methodInfo;
        public MethodBodyReader(MethodInfo mi)
        {
            _methodInfo = mi;
            if (mi.GetMethodBody() is not null)
            {
                _ilCode = mi.GetMethodBody()!.GetILAsByteArray()!;
                ConstructInstructions(mi.Module);
            }
            else
                _ilCode = Array.Empty<byte>();
        }
        #region il read methods
        private ushort ReadUInt16(ref int position)
            => (ushort)(_ilCode[position++] | (_ilCode[position++] << 8));
        private int ReadInt32(ref int position)
            => _ilCode[position++] | (_ilCode[position++] << 8) | (_ilCode[position++] << 0x10) | (_ilCode[position++] << 0x18);
        private ulong ReadInt64(ref int position)
            => (ulong)(_ilCode[position++] | (_ilCode[position++] << 8) | (_ilCode[position++] << 0x10) | (_ilCode[position++] << 0x18) | (_ilCode[position++] << 0x20) | (_ilCode[position++] << 0x28) | (_ilCode[position++] << 0x30) | (_ilCode[position++] << 0x38));
        private double ReadDouble(ref int position)
            => _ilCode[position++] | (_ilCode[position++] << 8) | (_ilCode[position++] << 0x10) | (_ilCode[position++] << 0x18) | (_ilCode[position++] << 0x20) | (_ilCode[position++] << 0x28) | (_ilCode[position++] << 0x30) | (_ilCode[position++] << 0x38);
        private sbyte ReadSByte(ref int position)
            => (sbyte)_ilCode[position++];
        private byte ReadByte(ref int position)
            => _ilCode[position++];
        private float ReadSingle(ref int position)
            => (_ilCode[position++] | (_ilCode[position++] << 8) | (_ilCode[position++] << 0x10)) | (_ilCode[position++] << 0x18);
        #endregion

        /// <summary>
        /// Constructs the array of ILInstructions according to the IL byte code.
        /// </summary>
        /// <param name="module"></param>
        private void ConstructInstructions(Module module)
        {
            int position = 0;
            while (position < _ilCode.Length)
            {
                ILInstruction instruction = new();
                ushort value = _ilCode[position++];

                // get the operation code of the current instruction
                OpCode code;
                if (value != 0xfe)
                {
                    code = Globals.SingleByteOpCodes[(int)value];
                }
                else
                {
                    value = _ilCode[position++];
                    code = Globals.MultiByteOpCodes[(int)value];
                }
                instruction.Code = code;
                instruction.Offset = position - 1;
                int metadataToken = 0;
                // get the operand of the current operation
                switch (code.OperandType)
                {
                    case OperandType.InlineBrTarget:
                        metadataToken = ReadInt32(ref position);
                        metadataToken += position;
                        instruction.Operand = metadataToken;
                        break;
                    case OperandType.InlineField:
                        metadataToken = ReadInt32(ref position);
                        instruction.Operand = module.ResolveField(metadataToken);
                        break;
                    case OperandType.InlineMethod:
                        metadataToken = ReadInt32(ref position);
                        try
                        {
                            instruction.Operand = module.ResolveMethod(metadataToken);
                        }
                        catch
                        {
                            try
                            {
                                instruction.Operand = module.ResolveMember(metadataToken);
                            }
                            catch
                            {

                            }
                        }
                        break;
                    case OperandType.InlineSig:
                        metadataToken = ReadInt32(ref position);
                        instruction.Operand = module.ResolveSignature(metadataToken);
                        break;
                    case OperandType.InlineTok:
                        metadataToken = ReadInt32(ref position);
                        Try.WithDefaultOnCatch(() =>
                        {
                            instruction.Operand = module.ResolveType(metadataToken);
                        });
                        break;
                    case OperandType.InlineType:
                        metadataToken = ReadInt32(ref position);
                        instruction.Operand = module.ResolveType(metadataToken, _methodInfo.DeclaringType!.GetGenericArguments(), _methodInfo.GetGenericArguments());
                        break;
                    case OperandType.InlineI:
                        {
                            instruction.Operand = ReadInt32(ref position);
                            break;
                        }
                    case OperandType.InlineI8:
                        {
                            instruction.Operand = ReadInt64(ref position);
                            break;
                        }
                    case OperandType.InlineNone:
                        {
                            instruction.Operand = null;
                            break;
                        }
                    case OperandType.InlineR:
                        {
                            instruction.Operand = ReadDouble(ref position);
                            break;
                        }
                    case OperandType.InlineString:
                        {
                            metadataToken = ReadInt32(ref position);
                            instruction.Operand = module.ResolveString(metadataToken);
                            break;
                        }
                    case OperandType.InlineSwitch:
                        {
                            int count = ReadInt32(ref position);
                            int[] casesAddresses = new int[count];
                            for (int i = 0; i < count; i++)
                            {
                                casesAddresses[i] = ReadInt32(ref position);
                            }
                            int[] cases = new int[count];
                            for (int i = 0; i < count; i++)
                            {
                                cases[i] = position + casesAddresses[i];
                            }
                            break;
                        }
                    case OperandType.InlineVar:
                        {
                            instruction.Operand = ReadUInt16(ref position);
                            break;
                        }
                    case OperandType.ShortInlineBrTarget:
                        {
                            instruction.Operand = ReadSByte(ref position) + position;
                            break;
                        }
                    case OperandType.ShortInlineI:
                        {
                            instruction.Operand = ReadSByte(ref position);
                            break;
                        }
                    case OperandType.ShortInlineR:
                        {
                            instruction.Operand = ReadSingle(ref position);
                            break;
                        }
                    case OperandType.ShortInlineVar:
                        {
                            instruction.Operand = ReadByte(ref position);
                            break;
                        }
                    default:
                        break;
                }
                Instructions.Add(instruction);
            }
        }

        public string GetBodyCode()
        {
            StringBuilder result = new();
            foreach (var instruction in Instructions)
                result.AppendLine(instruction.GetCode());
            return result.ToString();

        }
    }
}
