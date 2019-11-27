using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLPersistor.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PersistedForeignField : Attribute
    {
        public string CodeForeignFieldName { get; set; }

        public string DbForeignFieldName { get; set; }

        public string ForeignTableName { get; set; }

        public string DbForeignKey { get; set; }

        public string CodeForeignKey { get; set; }

        public PersistedForeignField()
        {
        }
    }
}
