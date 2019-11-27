using System;
using System.Linq;
using System.Data;


namespace SQLPersistor.Database
{
    public interface IDatabaseConnection
    {
        void ConnectToDatabase(string sqlString, bool log = true);
        void CloseConnection();
        void BeginTransaction();
        void Commit();
        void Rollback();
        SelectQueryResults SelectQuery(string serverCall, string queryType, string sqlQuery, bool isRetry = false);
        int ExecuteQuery(string serverCall, string queryType, string sqlQuery, bool throwException = false, bool isRetry = false);
    }
}


