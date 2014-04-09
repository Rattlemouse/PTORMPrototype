using System.Data;
using System.Text;

namespace PTORMPrototype.Mapping.Configuration
{
    public class SqlType
    {
        public SqlDbType Type { get; set; }
        public int[] Limits { get; set; }
        public bool Nullable { get; set; }

        public SqlType(SqlDbType sType, bool nullable, params int[] limits)
        {
            Type = sType;
            Limits = limits;
            Nullable = nullable;
        }

        public override string ToString()
        {
            var limits = "";
            if (Limits != null && Limits.Length > 0)
            {
                limits = string.Format("({0})", string.Join(",", Limits));
            }
            else if (Type == SqlDbType.NVarChar)
                limits = "(MAX)";
            return  Type + limits + (Nullable ? " NULL" : " NOT NULL");
        }
    }
}