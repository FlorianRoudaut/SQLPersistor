using SQLPersistor.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLPersistor.DataModel
{
    [PersistedClass(TableName="Batch_Parameters")]
    [Serializable]
    public class BatchParameter
    {
        [PersistedField(PrimaryKey = true, DbFieldName = "Batch_Parameter_ID")]
        public long BatchParameterId { get; set; }
        [PersistedField(DbFieldName = "Batch_ID")]
        public long BatchId { get; set; }
        [PersistedField(DbFieldName = "Parameter_Name")]
        public string ParameterName { get; set; }
        [PersistedField(DbFieldName = "Parameter_Value")]
        public string Value { get; set; }
        public BatchParameter(){ }

        public BatchParameter(string name) { ParameterName = name; }
        public BatchParameter(string name, string value) { ParameterName = name; Value = value; }
    }
}
