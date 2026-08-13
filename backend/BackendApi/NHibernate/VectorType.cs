using NHibernate;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using NHibernate.Engine;
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace BackendApi.NHibernate
{
    /// <summary>
    /// Custom NHibernate type for storing float[] embeddings as a string
    /// in a PostgreSQL text column. This avoids dependency on the pgvector
    /// extension and the Npgsql vector type handler, which is not natively
    /// supported in Npgsql 7.0.x.
    /// </summary>
    public class VectorType : IUserType
    {
        public bool IsMutable => false;
        public Type ReturnedType => typeof(float[]);
        public SqlType[] SqlTypes => new[] { new SqlType(DbType.String) };

        public object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
        {
            if (rs.IsDBNull(rs.GetOrdinal(names[0])))
                return Array.Empty<float>();

            try
            {
                var value = rs.GetValue(rs.GetOrdinal(names[0]));

                if (value == null || value == DBNull.Value)
                    return Array.Empty<float>();

                if (value is float[] floatArray)
                    return floatArray;

                var stringValue = value.ToString();

                if (string.IsNullOrEmpty(stringValue))
                    return Array.Empty<float>();

                stringValue = stringValue.Trim();

                if (stringValue.StartsWith("[") && stringValue.EndsWith("]"))
                    stringValue = stringValue.Substring(1, stringValue.Length - 2);
                else if (stringValue.StartsWith("{") && stringValue.EndsWith("}"))
                    stringValue = stringValue.Substring(1, stringValue.Length - 2);

                var parts = stringValue.Split(',');
                var result = new float[parts.Length];

                for (int i = 0; i < parts.Length; i++)
                {
                    result[i] = float.Parse(parts[i].Trim());
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing vector: {ex.Message}");
                return Array.Empty<float>();
            }
        }

        public void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
        {
            if (value == null || value is float[] array && array.Length == 0)
            {
                cmd.Parameters[index].Value = DBNull.Value;
                return;
            }

            var floatArray = (float[])value;
            var vectorString = "[" + string.Join(",", floatArray) + "]";
            cmd.Parameters[index].Value = vectorString;
        }

        public object DeepCopy(object value)
        {
            if (value == null)
                return null!;

            var array = (float[])value;
            var copy = new float[array.Length];
            Array.Copy(array, copy, array.Length);
            return copy;
        }

        public object Replace(object original, object target, object owner)
        {
            return DeepCopy(original);
        }

        public object Assemble(object cached, object owner)
        {
            return DeepCopy(cached);
        }

        public object Disassemble(object value)
        {
            return DeepCopy(value);
        }

        public int GetHashCode(object x)
        {
            if (x == null)
                return 0;

            var array = (float[])x;
            unchecked
            {
                int hash = 17;
                foreach (var f in array)
                {
                    hash = hash * 31 + f.GetHashCode();
                }
                return hash;
            }
        }

        public new bool Equals(object x, object y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            var array1 = (float[])x;
            var array2 = (float[])y;

            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                    return false;
            }

            return true;
        }
    }
}
