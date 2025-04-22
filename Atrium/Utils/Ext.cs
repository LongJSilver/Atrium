using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Atrium.Utils
{
    public static class Ext
    {
        public static IEnumerable<string> RowsToFixedLengthStrings(this DataTable table, string separator = "  |  ", int maxColumnLen = int.MaxValue)
        {
            int[] maxLengths = new int[table.Columns.Count];
            for (int row = 0; row < table.Rows.Count; row++)
            {
                DataRow dr = table.Rows[row];
                for (int col = 0; col < maxLengths.Length; col++)
                {
                    maxLengths[col] = Math.Max(maxLengths[col], dr[col].ToString().Length);
                }
            }

            int totLen = 0;
            for (int i = 0; i < maxLengths.Length; i++)
            {
                totLen += maxLengths[i] = Math.Min(maxColumnLen, maxLengths[i]);
            }
            totLen += separator.Length * (maxLengths.Length - 1);

            List<string> result = new();

            StringBuilder sb = new(totLen);

            for (int row = 0; row < table.Rows.Count; row++)
            {
                sb.Clear();
                DataRow dr = table.Rows[row];

                for (int col = 0; col < maxLengths.Length; col++)
                {
                    if (col > 0) sb.Append(separator);

                    string val = dr[col]?.ToString() ?? "";
                    int diff = maxLengths[col] - val.Length;
                    if (diff < 0) //too long
                        sb.Append(val.Substring(0, maxColumnLen));
                    else if (diff > 0) //too short
                    {
                        sb.Append(val).Append(' ', diff);
                    }
                    else
                    {
                        sb.Append(val);
                    }
                }
                result.Add(sb.ToString());
            }
            return result;
        }

        public static string? DecryptSecureString(SecureString secureString)
        {
            if (secureString == null) return null;
            IntPtr stringPtr = IntPtr.Zero;
            string? Decrypted = null;
            GCHandle gcHandler = GCHandle.Alloc(Decrypted, GCHandleType.Pinned);

            try
            {
                stringPtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                Decrypted = Marshal.PtrToStringUni(stringPtr);
                return Decrypted;
            }
            finally
            {
                gcHandler.Free();
                Marshal.ZeroFreeGlobalAllocUnicode(stringPtr);
            }
        }
        public static SecureString EncryptSecureString(string unsafeString)
        {
            SecureString ss = new();
            foreach (char item in unsafeString)
            {
                ss.AppendChar(item);
            }
            ss.MakeReadOnly();
            return ss;
        }

        public static SecureString ToSecureString(this string unsafeString) => EncryptSecureString(unsafeString);
        public static string? ToClearString(this SecureString safeString) => DecryptSecureString(safeString);


        public static IEnumerable<ResultType> ForEach<EnumerableType, ResultType>(
                    this IEnumerable<EnumerableType> list,
                    Func<EnumerableType, ResultType> function)
        {
            List<ResultType> _result = new();
            list.ForEach(l => _result.Add(function(l)));
            return _result;
        }

        public static void ForEach<EnumerableType>
            (this IEnumerable<EnumerableType> list, Action<EnumerableType> action)
        {
            foreach (EnumerableType item in list)
            {
                action(item);
            }
        }
        public static void AddRange<V>(this ISet<V> me, IEnumerable<V> all) => all.ForEach(v => me.Add(v));

        #region Tuples

        public static V[] AllEntries<V>(this Tuple<V, V, V, V, V, V, V> t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5,t.Item6,t.Item7};
        public static V[] AllEntries<V>(this Tuple<V, V, V, V, V, V> t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5,t.Item6};
        public static V[] AllEntries<V>(this Tuple<V, V, V, V, V> t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5};
        public static V[] AllEntries<V>(this Tuple<V, V, V, V> t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4};
        public static V[] AllEntries<V>(this Tuple<V, V, V> t) => new V[] {
                                t.Item1,t.Item2,t.Item3};
        public static V[] AllEntries<V>(Tuple<V, V> t) => new V[] {
                                t.Item1,t.Item2};

        public static V[] AllEntries<V>(this (V, V, V, V, V, V, V) t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5,t.Item6,t.Item7};
        public static V[] AllEntries<V>(this (V, V, V, V, V, V) t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5,t.Item6};
        public static V[] AllEntries<V>(this (V, V, V, V, V) t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4,t.Item5};
        public static V[] AllEntries<V>(this (V, V, V, V) t) => new V[] {
                                t.Item1,t.Item2,t.Item3,t.Item4};
        public static V[] AllEntries<V>(this (V, V, V) t) => new V[] {
                                t.Item1,t.Item2,t.Item3};
        public static V[] AllEntries<V>((V, V) t) => new V[] {
                                t.Item1,t.Item2};
        #endregion


    }
}
