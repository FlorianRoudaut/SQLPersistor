using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLPersistor.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PersistedField : Attribute
    {
        public bool PrimaryKey { get; set; }
        public string ForeingKeyTable { get; set; }

        public string DbFieldName { get; set; }
        public string CodeFieldName { get; set; }

        public Type CodeFieldType { get; set; }
        public PersistedField()
        {
        }
    }
}
