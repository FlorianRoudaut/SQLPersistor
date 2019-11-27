using NUnit.Framework;
using SQLPersistor.Database;
using SQLPersistor.DataModel;
using SQLPersistor.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLPersistor.Tests
{
    [TestFixture]
    public class PersistorTest
    {
        [Test]
        public void FindTest()
        {
            var mockDbConnection = new MockDataBaseConnection();
            var expectedResult = new SelectQueryResults();
            var dict = new Dictionary<int, object>();
            dict[0] = 1;
            dict[1] = "CheckService";
            expectedResult.AddRow(dict);
            mockDbConnection.SelectMappings["SELECT Batch_ID,Batch_Type,Name,Description FROM Batches WHERE Batch_ID=1;"] = expectedResult;

            var paramResults = new SelectQueryResults();
            var dict1 = new Dictionary<int, object>();
            dict1[0] = 1;
            dict1[1] = 1;
            dict1[2] = "SendMail";
            dict1[3] = "";
            paramResults.AddRow(dict1);
            var dict2 = new Dictionary<int, object>();
            dict2[0] = 2;
            dict2[1] = 1;
            dict2[2] = "MailSender";
            dict2[3] = "dev@google.fr";
            paramResults.AddRow(dict2);
            mockDbConnection.SelectMappings["SELECT Batch_Parameter_ID,Batch_ID,Parameter_Name,Parameter_Value " +
                "FROM Batch_Parameters WHERE Batch_ID IN (1);"] = paramResults;

            var batch = Persistor<Batch>.Find(mockDbConnection, 1);
            Assert.AreNotEqual(null, batch);
            Assert.AreEqual(1, batch.BatchId);
            Assert.AreEqual("CheckService", batch.BatchType);

            Assert.AreEqual(2, batch.Params.Count);
            var firstParam = batch.Params[0];
            Assert.AreEqual("SendMail", firstParam.ParameterName);

            var secondParam = batch.Params[1];
            Assert.AreEqual("MailSender", secondParam.ParameterName);
            Assert.AreEqual("dev@google.fr", secondParam.Value);
        }

        [Test]
        public void LoadAllTest()
        {
            var mockDbConnection = new MockDataBaseConnection();
            var expectedResult = new SelectQueryResults();
            var dict = new Dictionary<int, object>();
            dict[0] = 1;
            dict[1] = "CheckService";
            expectedResult.AddRow(dict);
            var dict2 = new Dictionary<int, object>();
            dict2[0] = 2;
            dict2[1] = "DeleteAllClients";
            expectedResult.AddRow(dict2);
            mockDbConnection.SelectMappings["SELECT Batch_ID,Batch_Type,Name,Description FROM Batches;"] = expectedResult;

            var batches = Persistor<Batch>.LoadAll(mockDbConnection, "");
            Assert.AreNotEqual(null, batches);
            Assert.AreEqual(2, batches.Count);
            Assert.AreEqual(1, batches[0].BatchId);
            Assert.AreEqual("DeleteAllClients", batches[1].BatchType);
        }

        [Test]
        public void SaveOrUpdateTest()
        {
            var mockDbConnection = new MockDataBaseConnection();

            var updateBatch = new Batch();
            updateBatch.BatchId = 2;
            updateBatch.BatchType = "DeleteAllClients";
            var batch = Persistor<Batch>.SaveOrUpdate(mockDbConnection, updateBatch);
            Assert.AreEqual(1, mockDbConnection.ExecutedQueries.Count);
            var updateQuery = "UPDATE Batches SET Batch_Type='DeleteAllClients',Name='',Description='' WHERE Batch_ID = 2;";
            Assert.AreEqual(updateQuery, mockDbConnection.ExecutedQueries[0]);

            var newBatch = new Batch();
            newBatch.BatchId = -1;
            newBatch.BatchType = "CheckService";

            var expectedResult = new SelectQueryResults();
            var dict = new Dictionary<int, object>();
            dict[0] = 5;
            expectedResult.AddRow(dict);
            mockDbConnection.SelectMappings["SELECT last_insert_id();"] = expectedResult;
            var expectedResult2 = new SelectQueryResults();
            var dict2 = new Dictionary<int, object>();
            dict2[0] = 5;
            dict2[1] = "CheckService";
            expectedResult2.AddRow(dict2);
            mockDbConnection.SelectMappings["SELECT Batch_ID,Batch_Type,Name,Description FROM Batches WHERE Batch_ID=5;"] = expectedResult2;

            newBatch = Persistor<Batch>.SaveOrUpdate(mockDbConnection, newBatch);
            Assert.AreEqual(2, mockDbConnection.ExecutedQueries.Count);
            var insertQuery = "INSERT INTO Batches (Batch_Type,Name,Description) VALUES ('CheckService','','');";
            Assert.AreEqual(insertQuery, mockDbConnection.ExecutedQueries[1]);
            Assert.AreNotEqual(null, newBatch);
            Assert.AreEqual(5, newBatch.BatchId);
        }

        [Test]
        public void SaveWithChildsTest()
        {
            var mockDbConnection = new MockDataBaseConnection();

            //Insert with child, the row should be inserted and every child shoudl be inserted too
            var newBatch = new Batch();
            newBatch.BatchId = -1;
            newBatch.BatchType = "CheckService";

            var parameter = new BatchParameter();
            parameter.BatchId = -1;
            parameter.BatchParameterId = -1;
            parameter.ParameterName = "MailSender";
            parameter.Value = "dev@google.fr";
            newBatch.Params.Add(parameter);

            var expectedResult = new SelectQueryResults();
            var dict = new Dictionary<int, object>();
            dict[0] = 5;
            expectedResult.AddRow(dict);
            mockDbConnection.SelectMappings["SELECT last_insert_id();"] = expectedResult;
            var expectedResult2 = new SelectQueryResults();
            var dict2 = new Dictionary<int, object>();
            dict2[0] = 5;
            dict2[1] = "CheckService";
            expectedResult2.AddRow(dict2);
            mockDbConnection.SelectMappings["SELECT Batch_ID,Batch_Type,Name,Description FROM Batches WHERE Batch_ID=5;"] = expectedResult2;

            var expectedResult3 = new SelectQueryResults();
            var dict3 = new Dictionary<int, object>();
            dict3[0] = 5;
            dict3[1] = 5;
            dict3[2] = "MailSender";
            dict3[3] = "dev@google.fr";
            expectedResult3.AddRow(dict3);
            mockDbConnection.SelectMappings["SELECT Batch_Parameter_ID,Batch_ID,Parameter_Name,Parameter_Value FROM Batch_Parameters WHERE Batch_Parameter_ID=5;"] = expectedResult3;

            newBatch = Persistor<Batch>.SaveOrUpdate(mockDbConnection, newBatch);
            Assert.AreEqual(2, mockDbConnection.ExecutedQueries.Count);
            var insertQuery = "INSERT INTO Batches (Batch_Type,Name,Description) VALUES ('CheckService','','');";
            Assert.AreEqual(insertQuery, mockDbConnection.ExecutedQueries[0]);
            Assert.AreNotEqual(null, newBatch);
            Assert.AreEqual(5, newBatch.BatchId);

            Assert.AreEqual(2, mockDbConnection.ExecutedQueries.Count);
            var insertParamQuery = "INSERT INTO Batch_Parameters (Batch_ID,Parameter_Name,Parameter_Value)" +
                " VALUES (5,'MailSender','dev@google.fr');";
            Assert.AreEqual(insertParamQuery, mockDbConnection.ExecutedQueries[1]);
            var savedParam = newBatch.Params[0];
            Assert.AreNotEqual(null, savedParam);
            Assert.AreEqual(5, savedParam.BatchId);
            Assert.AreEqual(5, savedParam.BatchParameterId);
        }

        [Test]
        public void UpdateWithChildsTest()
        {
            var mockDbConnection = new MockDataBaseConnection();
            //Update with childs. The row should be updated. FOr childs there are three cases :

            //Insert with child, the row should be inserted and every child shoudl be inserted 
            var updateBatch = new Batch();
            updateBatch.BatchId = 2;
            updateBatch.BatchType = "DeleteAllClients";

            //1. If the child has a negative id it should be inserted
            var newParameter = new BatchParameter();
            newParameter.BatchId = -1;
            newParameter.BatchParameterId = -1;
            newParameter.ParameterName = "MailSender";
            newParameter.Value = "dev@google.fr";
            updateBatch.Params.Add(newParameter);

            //2. If the child has a positive id it should be updated
            var updateParameter = new BatchParameter();
            updateParameter.BatchId = 2;
            updateParameter.BatchParameterId = 1;
            updateParameter.ParameterName = "MailReceiver";
            updateParameter.Value = "support@google.fr";
            updateBatch.Params.Add(updateParameter);

            //3. We take the list of all ids in the db, 
            //if there is an id that is not in the child list we should delete. 
            var expectedResult4 = new SelectQueryResults();
            var dict4 = new Dictionary<int, object>();
            dict4[0] = 1;
            expectedResult4.AddRow(dict4);
            var dict5 = new Dictionary<int, object>();
            dict5[0] = 4;
            expectedResult4.AddRow(dict5);
            mockDbConnection.SelectMappings["SELECT Batch_Parameter_ID FROM Batch_Parameters WHERE Batch_ID=2;"] = expectedResult4;


            var expectedResult = new SelectQueryResults();
            var dict = new Dictionary<int, object>();
            dict[0] = 2;
            expectedResult.AddRow(dict);
            mockDbConnection.SelectMappings["SELECT last_insert_id();"] = expectedResult;

            var expectedResult3 = new SelectQueryResults();
            var dict3 = new Dictionary<int, object>();
            dict3[0] = 2;
            dict3[1] = 2;
            dict3[2] = "MailSender";
            dict3[3] = "dev@google.fr";
            expectedResult3.AddRow(dict3);
            mockDbConnection.SelectMappings["SELECT Batch_Parameter_ID,Batch_ID,Parameter_Name,Parameter_Value FROM Batch_Parameters WHERE Batch_Parameter_ID=2;"] = expectedResult3;


            var newBatch = Persistor<Batch>.SaveOrUpdate(mockDbConnection, updateBatch);


            Assert.AreEqual(4, mockDbConnection.ExecutedQueries.Count);
            Assert.AreEqual(2, newBatch.Params.Count);

            //1
            var insertParamQuery = "INSERT INTO Batch_Parameters (Batch_ID,Parameter_Name,Parameter_Value) VALUES (2,'MailSender','dev@google.fr');";
            Assert.AreEqual(insertParamQuery, mockDbConnection.ExecutedQueries[1]);
            var savedParam = newBatch.Params[0];
            Assert.AreNotEqual(null, savedParam);
            Assert.AreEqual(2, savedParam.BatchId);
            Assert.AreEqual(2, savedParam.BatchParameterId);

            //2
            var updateParamQuery = "UPDATE Batch_Parameters SET Batch_ID=2,Parameter_Name='MailReceiver',Parameter_Value='support@google.fr' WHERE Batch_Parameter_ID = 1;";
            Assert.AreEqual(updateParamQuery, mockDbConnection.ExecutedQueries[2]);

            //3
            var deleteParamQuery = "DELETE FROM Batch_Parameters WHERE Batch_Parameter_ID IN (4);";
            Assert.AreEqual(deleteParamQuery, mockDbConnection.ExecutedQueries[3]);
        }

        [Test]
        public void DeleteTest()
        {
            var mockDbConnection = new MockDataBaseConnection();
            var batchList = new List<Batch>();

            var batch1 = new Batch();
            batch1.BatchId = 1;
            batch1.BatchType = "CheckService";

            var param1 = new BatchParameter();
            param1.BatchParameterId = 1;
            param1.BatchId = 1;
            param1.ParameterName = "SendMail";
            batch1.Params.Add(param1);
            var param2 = new BatchParameter();
            param2.BatchParameterId = 2;
            param2.BatchId = 1;
            param2.ParameterName = "MailSender";
            param2.Value = "deve@google.fr";
            batch1.Params.Add(param1);

            batchList.Add(batch1);
            var batch2 = new Batch();
            batch2.BatchId = 2;
            batch2.BatchType = "DeleteAllClients";
            batchList.Add(batch2);

            Persistor<Batch>.Delete(mockDbConnection, batchList);
            Assert.AreEqual(2, mockDbConnection.ExecutedQueries.Count);

            var deleteParametersQuery = "DELETE FROM Batch_Parameters WHERE Batch_ID IN (1,2);";
            Assert.AreEqual(deleteParametersQuery, mockDbConnection.ExecutedQueries[0]);

            var deleteBatchQuery = "DELETE FROM Batches WHERE Batch_ID IN (1,2);";
            Assert.AreEqual(deleteBatchQuery, mockDbConnection.ExecutedQueries[1]);
        }

        [Test]
        public void TransactionTest()
        {
            var dbConnection = new MockDataBaseConnection();
            var batch = new Batch();
            batch.Params = null;
            Persistor<Batch>.SaveOrUpdate(dbConnection, batch);
            Assert.AreEqual(true, dbConnection.HasBegunTransaction);
            Assert.AreEqual(false, dbConnection.LastTransactionCommit);
            Assert.AreEqual(true, dbConnection.LastTransactionRollback);

            dbConnection = new MockDataBaseConnection();
            batch = new Batch();
            Persistor<Batch>.SaveOrUpdate(dbConnection, batch);
            Assert.AreEqual(true, dbConnection.HasBegunTransaction);
            Assert.AreEqual(true, dbConnection.LastTransactionCommit);
            Assert.AreEqual(false, dbConnection.LastTransactionRollback);
        }
    }
}
