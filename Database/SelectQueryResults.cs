using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLPersistor.Database
{
    public class SelectQueryResults : IEnumerable<Dictionary<int, object>>
    {
        private List<Dictionary<int, object>> Results = new List<Dictionary<int, object>>();

        public void AddRow(Dictionary<int, object> dict)
        {
            Results.Add(dict);
        }

        public IEnumerator<Dictionary<int, object>> GetEnumerator()
        {
            return Results.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Results.GetEnumerator();
        }

        public long GetFirstLong()
        {
            foreach (var result in Results)
            {
                if (result.Count == 0) continue;
                long longResult;
                if (result[0]!=null&&long.TryParse(result[0].ToString(), out longResult)) return longResult;
            }
            return 0;
        }
    }
}
