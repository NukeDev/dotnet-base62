﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GuidToBase62 {
    public static class GuidToBase62Extension {
        private const uint radix = 62;
        private const ulong carry = 297528130221121800; // 2^64 / 62
        private const ulong carryRemainder = 16;        // 2^64 % 62
        private const string symbols = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string outputTemplate = "0000000000000000000000"; // 16 bytes takes up 22 base62 symbols

        private static void DivRem(ulong num, out ulong quot, out uint rem) {
            quot = num / radix;
            rem = (uint) (num - (radix * quot));
        }

        private static void DivRem(ulong numUpper64, ulong numLower64, out ulong quotUpper, out ulong quotLower, out uint rem) {
            uint remLower, remUpper;
            ulong remLowerQuot;
            
            DivRem(numUpper64, out quotUpper, out remUpper);
            DivRem(numLower64, out quotLower, out remLower);

            // take the upper remainder, and incorporate it into the other lower quotient/lower remainder/output remainder
            remLower += (uint)(remUpper * carryRemainder); // max value = 61 + 61*16 = 1037
            DivRem(remLower, out remLowerQuot, out rem);

            // at this point the max values are:
            //   quotientLower: 2^64-17 / 62, which is 297528130221121799 (any more than 2^64-17 and remainderLower will be under 61)
            //   remainderUpper * carry: 61 * 297528130221121800 which is 18149215943488429800
            //   remainderLowerQuotient = 1037 / 62 = 16 
            quotLower += remUpper * carry;  // max value is now 18446744073709551599
            quotLower += remLowerQuot;  // max value is now 18446744073709551615. So no overflow.
        }

        public static string ToBase62(this Guid guid) {
            return guid.ToByteArray().ToBase62();
        }

        public static string ToBase62(this byte[] bytes, bool zeroPadding = false) {
            if (bytes.Length != 16)
                throw new ArgumentOutOfRangeException("Input must be 16 bytes");

            if (!BitConverter.IsLittleEndian)
                throw new Exception("Untested on big endian architecture");
           
            ulong lower, upper;
            
            lower = BitConverter.ToUInt64(bytes, 0);
            upper = BitConverter.ToUInt64(bytes, 8);
         
            var sb = new StringBuilder(outputTemplate);
            int pos = outputTemplate.Length;
            uint remainder;
            while (upper != 0) {
                DivRem(upper, lower, out upper, out lower, out remainder);
                sb[--pos] = symbols[(int)remainder];
            }

            do {
                DivRem(lower, out lower, out remainder);
                sb[--pos] = symbols[(int)remainder];
            } while (lower != 0);

            if (zeroPadding) pos = 0;
            return sb.ToString(pos, outputTemplate.Length - pos);
        }
    }
}
