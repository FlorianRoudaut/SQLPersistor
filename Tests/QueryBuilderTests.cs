using NUnit.Framework;
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
    public class QueryBuilderTests
    {
        [Test]
        public void TestLoadAllQuery()
        {
            //Cas simple
            var builtQuery = QueryBuilder.BuildLoadAllQuery(typeof(Batch));
            var expectedQuery = "SELECT Batch_ID,Batch_Type,Name,Description FROM Batches;";
            Assert.AreEqual(expectedQuery, builtQuery);
        }

        [Test]
        public void TestLoadQuery()
        {
            //Cas simple
            var builtQuery = QueryBuilder.BuildLoadQuery(typeof(Batch), 1);
            var expectedQuery = "SELECT Batch_ID,Batch_Type,Name,Description FROM Batches WHERE Batch_ID=1;";
            Assert.AreEqual(expectedQuery, builtQuery);
        }

        [Test]
        public void TestInsertQuery()
        {
            //Cas simple
            var batch = new Batch();
            batch.BatchType = "CheckService";
            var builtQuery = QueryBuilder.BuildInsertQuery(typeof(Batch), new List<object> { batch });
            var expectedQuery = "INSERT INTO Batches (Batch_Type,Name,Description) VALUES ('CheckService','','');";
            Assert.AreEqual(expectedQuery, builtQuery);

            //Cas multiple
            var batch2 = new Batch();
            batch2.BatchType = "UpdateParents";
            builtQuery = QueryBuilder.BuildInsertQuery(typeof(Batch), new List<object> { batch, batch2 });
            expectedQuery = "INSERT INTO Batches (Batch_Type,Name,Description) VALUES ('CheckService','',''),('UpdateParents','','');";
            Assert.AreEqual(expectedQuery, builtQuery);
        }

        [Test]
        public void TestUpdateQuery()
        {
            //Cas simple
            var batch = new Batch();
            batch.BatchId = 1;
            batch.BatchType = "CheckService";
            var builtQuery = QueryBuilder.BuildUpdateQuery(typeof(Batch), batch);
            var expectedQuery = "UPDATE Batches SET Batch_Type='CheckService',Name='',Description='' WHERE Batch_ID = 1;";
            Assert.AreEqual(expectedQuery, builtQuery);
        }

        [Test]
        public void TestDeleteQuery()
        {
            //Cas simple
            var batch = new Batch();
            batch.BatchId = 1;
            var batch2 = new Batch();
            batch2.BatchId = 2;
            var builtQuery = QueryBuilder.BuildDeleteQuery(typeof(Batch), new List<long> { 1, 2 });
            var expectedQuery = "DELETE FROM Batches WHERE Batch_ID IN (1,2);";
            Assert.AreEqual(expectedQuery, builtQuery);
        }
    }
}
