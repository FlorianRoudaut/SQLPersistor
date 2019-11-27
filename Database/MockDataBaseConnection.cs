using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLPersistor.Database
{
    public class MockDataBaseConnection : IDatabaseConnection
    {
        public List<string> ExecutedQueries = new List<string>();
        public Dictionary<string, SelectQueryResults> SelectMappings = new Dictionary<string, SelectQueryResults>();
        public bool HasBegunTransaction;
        public bool LastTransactionCommit;
        public bool LastTransactionRollback;

        public void BeginTransaction() { HasBegunTransaction = true; }

        public void CloseConnection() { }

        public void Commit() { LastTransactionCommit = true; }

        public void ConnectToDatabase(string sqlString, bool log = true)
        {
            throw new NotImplementedException();
        }

        public int ExecuteQuery(string serverCall, string queryType, string sqlQuery, bool throwException = false, bool isRetry = false)
        {
            ExecutedQueries.Add(sqlQuery);
            return 0;
        }

        public void Rollback() { LastTransactionRollback = true; }

        public SelectQueryResults SelectQuery(string serverCall, string queryType, string sqlQuery, bool isRetry = false)
        {
            SelectQueryResults results;
            if (SelectMappings.TryGetValue(sqlQuery, out results)) return results;
            return new SelectQueryResults();
        }
    }
}
