using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLPersistor.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PersistedForeignClass : Attribute
    {
        public Type ForeignType { get; set; }
        public string DbForeignKey { get; set; }
        public string CodeFieldName { get; set; }
        public string CodeForeignKey { get; set; }
        public PersistedForeignClass()
        {
        }
    }
}
