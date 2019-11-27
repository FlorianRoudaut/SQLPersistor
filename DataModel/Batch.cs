using SQLPersistor.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SQLPersistor.DataModel
{
    [PersistedClass(TableName = "Batches")]
    [Serializable]
    public class Batch
    {
        public Batch() { Params = new List<BatchParameter>(); }

        [PersistedField(PrimaryKey = true, DbFieldName = "Batch_ID")]
        public long BatchId { get; set; }

        [PersistedField(DbFieldName = "Batch_Type")]
        public string BatchType { get; set; }

        [PersistedField(DbFieldName = "Name")]
        public string Name { get; set; }

        [PersistedField(DbFieldName = "Description")]
        public string Description { get; set; }

        [PersistedForeignList(ForeignType = typeof(BatchParameter),
            DbForeignKey = "Batch_ID", CodeForeignKey = "BatchId")]
        public List<BatchParameter> Params { get; set; }
    }
}
