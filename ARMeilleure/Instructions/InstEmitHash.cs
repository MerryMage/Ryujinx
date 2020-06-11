// https://www.intel.com/content/dam/www/public/us/en/documents/white-papers/fast-crc-computation-generic-polynomials-pclmulqdq-paper.pdf

using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        public static void Crc32b(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Pclmulqdq(context, false, 8);
            }
            else
            {
                EmitCrc32Call(context, new _U32_U32_U8(SoftFallback.Crc32b));
            }
        }

        public static void Crc32h(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Pclmulqdq(context, false, 16);
            }
            else
            {
                EmitCrc32Call(context, new _U32_U32_U16(SoftFallback.Crc32h));
            }
        }

        public static void Crc32w(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Pclmulqdq(context, false, 32);
            }
            else
            {
                EmitCrc32Call(context, new _U32_U32_U32(SoftFallback.Crc32w));
            }
        }

        public static void Crc32x(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Pclmulqdq(context, false, 64);
            }
            else
            {
                EmitCrc32Call(context, new _U32_U32_U64(SoftFallback.Crc32x));
            }
        }

        public static void Crc32cb(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Pclmulqdq(context, true, 8);
            }
            else
            {
                EmitCrc32Call(context, new _U32_U32_U8(SoftFallback.Crc32cb));
            }
        }

        public static void Crc32ch(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Pclmulqdq(context, true, 16);
            }
            else
            {
                EmitCrc32Call(context, new _U32_U32_U16(SoftFallback.Crc32ch));
            }
        }

        public static void Crc32cw(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Pclmulqdq(context, true, 32);
            }
            else
            {
                EmitCrc32Call(context, new _U32_U32_U32(SoftFallback.Crc32cw));
            }
        }

        public static void Crc32cx(ArmEmitterContext context)
        {
            if (Optimizations.UsePclmulqdq)
            {
                EmitCrc32Pclmulqdq(context, true, 64);
            }
            else
            {
                EmitCrc32Call(context, new _U32_U32_U64(SoftFallback.Crc32cx));
            }
        }

        private static void EmitCrc32Pclmulqdq(ArmEmitterContext context, bool castagnoli, int bitsize)
        {
            OpCodeAluBinary op = (OpCodeAluBinary)context.CurrOp;

            ulong mu = castagnoli ? 0x4869EC38DEA713F1u : 0xB4E5B025F7011641u; // mu' = floor(x^96/P(x))'
            ulong polynomial = castagnoli ? 0x105EC76F0u : 0x1DB710641u; // P'(x) << 1

            Operand crc = GetIntOrZR(context, op.Rn);
            Operand data = GetIntOrZR(context, op.Rm);

            crc = context.VectorInsert(context.VectorZero(), crc, 0);

            switch (bitsize)
            {
                case 8: data = context.VectorInsert8(context.VectorZero(), data, 0); break;
                case 16: data = context.VectorInsert16(context.VectorZero(), data, 0); break;
                case 32: data = context.VectorInsert(context.VectorZero(), data, 0); break;
                case 64: data = context.VectorInsert(context.VectorZero(), data, 0); break;
            }

            Operand tmp = context.AddIntrinsic(Intrinsic.X86Pxor, crc, data);
            Operand res = tmp;

            if (bitsize < 64)
            {
                res = context.AddIntrinsic(Intrinsic.X86Pslldq, res, Const((64 - bitsize) / 8));
            }

            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, res, X86GetScalar(context, mu), Const(0));
            tmp = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, tmp, X86GetScalar(context, polynomial), Const(0));

            if (bitsize < 32)
            {
                tmp = context.AddIntrinsic(Intrinsic.X86Pxor, tmp, res);
            }

            SetIntOrZR(context, op.Rd, context.VectorExtract(OperandType.I32, tmp, 2));
        }

        private static void EmitCrc32Call(ArmEmitterContext context, Delegate dlg)
        {
            OpCodeAluBinary op = (OpCodeAluBinary)context.CurrOp;

            Operand n = GetIntOrZR(context, op.Rn);
            Operand m = GetIntOrZR(context, op.Rm);

            Operand d = context.Call(dlg, n, m);

            SetIntOrZR(context, op.Rd, d);
        }
    }
}
