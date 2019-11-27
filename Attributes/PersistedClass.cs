using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLPersistor.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PersistedClass : Attribute
    {
        public string TableName { get; set; }
    }
}
