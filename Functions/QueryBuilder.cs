using SQLPersistor.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLPersistor.Functions
{
    //Class to create SQL, it may be dbms dependent
    public class QueryBuilder
    {
        public static string BuildLoadQuery(Type type, long id)
        {
            var query = BuildLoadAllQuery(type);
            query = query.Replace(";", "");

            var persistedFields = PersistenceHelperFunctions.GetPersistedFields(type);
            var pkFieldName = "";
            foreach (var persistedField in persistedFields)
            {
                if (persistedField.PrimaryKey) pkFieldName = persistedField.DbFieldName;
            }

            query += " WHERE " + pkFieldName + "=" + id;
            query += ";";
            return query;
        }

        public static string BuildLoadAllQuery(Type type, string where ="")
        {
            var tableName = PersistenceHelperFunctions.GetTableName(type);
            if (string.IsNullOrEmpty(tableName)) return "";

            var persistedFields = PersistenceHelperFunctions.GetPersistedFields(type);
            if (persistedFields.Count == 0) return "";

            var query = "SELECT ";

            var firstField = true;
            foreach (var persistedField in persistedFields)
            {
                if (!firstField) query += ",";
                else firstField = false;
                query += persistedField.DbFieldName;
            }

            query += " FROM " + tableName;
            if (!string.IsNullOrEmpty(where))
            {
                query += " WHERE ";
                query += where;
            }
            query += ";";
            return query;
        }

        public static string BuildLoadAllIdsQuery(Type type, string where = "")
        {
            var tableName = PersistenceHelperFunctions.GetTableName(type);
            if (string.IsNullOrEmpty(tableName)) return "";

            var persistedFields = PersistenceHelperFunctions.GetPersistedFields(type);
            if (persistedFields.Count == 0) return "";

            var query = "SELECT ";

            foreach (var persistedField in persistedFields)
            {
                if(persistedField.PrimaryKey)
                {
                    query += persistedField.DbFieldName;
                    break;
                }
            }

            query += " FROM " + tableName;
            if (!string.IsNullOrEmpty(where))
            {
                query += " WHERE ";
                query += where;
            }
            query += ";";
            return query;
        }

        public static string BuildInsertQuery(Type type, List<object> objects)
        {
            var query = "";
            if (objects.Count == 0) return query;

            var tableName = PersistenceHelperFunctions.GetTableName(type);
            if (string.IsNullOrEmpty(tableName)) return "";

            var persistedFields = PersistenceHelperFunctions.GetPersistedFields(type);
            if (persistedFields.Count == 0) return "";

            query = "INSERT INTO " + tableName;
            query += " (";
            var firstField = true;
            foreach (var persistedField in persistedFields)
            {
                if (persistedField.PrimaryKey) continue;
                if (firstField) firstField = false;
                else query += ",";
                query += persistedField.DbFieldName;
            }
            query += ")";

            query += " VALUES ";
            var firstObj = true;
            foreach (var obj in objects)
            {
                if (firstObj) firstObj = false;
                else query += ",";

                query += "(";

                firstField = true;
                foreach (var persistedField in persistedFields)
                {
                    if (persistedField.PrimaryKey) continue;
                    if (firstField) firstField = false;
                    else query += ",";
                    query += PersistenceHelperFunctions.GetSqlFormatedFieldValue(obj, persistedField);
                }
                query += ")";
            }

            query += ";";

            return query;
        }

        public static string BuildDeleteQuery(Type type, List<long> idsList)
        {
            var query = "";
            if (idsList.Count == 0) return query;

            var tableName = PersistenceHelperFunctions.GetTableName(type);
            if (string.IsNullOrEmpty(tableName)) return "";

            var persistedFields = PersistenceHelperFunctions.GetPersistedFields(type);
            if (persistedFields.Count == 0) return "";

            query = "DELETE FROM " + tableName;

            var pkField = "";
            foreach (var persistedField in persistedFields)
            {
                if (persistedField.PrimaryKey)
                {
                    pkField = persistedField.DbFieldName;
                    break;
                }
            }
            if (string.IsNullOrEmpty(pkField)) return "";

            query += " WHERE " + pkField + " IN (";

            var firstObj = true;
            foreach (var id in idsList)
            {
                if (firstObj) firstObj = false;
                else query += ",";

                query += id;
            }

            query += ")";
            query += ";";

            return query;
        }

        public static string BuildUpdateQuery(Type type,object obj)
        {
            var tableName = PersistenceHelperFunctions.GetTableName(type);
            if (string.IsNullOrEmpty(tableName)) return "";

            var persistedFields = PersistenceHelperFunctions.GetPersistedFields(type);
            if (persistedFields.Count == 0) return "";

            var query = "UPDATE " + tableName;
            query += " SET ";
            var pkValue = "";
            var pkFieldName = "";
            var firstField = true;
            foreach (var persistedField in persistedFields)
            {
                if (persistedField.PrimaryKey)
                {
                    pkValue = PersistenceHelperFunctions.GetSqlFormatedFieldValue(obj, persistedField);
                    pkFieldName = persistedField.DbFieldName;
                }
                else
                {
                    if (firstField) firstField = false;
                    else query += ",";
                    query += persistedField.DbFieldName + "=" 
                        + PersistenceHelperFunctions.GetSqlFormatedFieldValue(obj, persistedField);
                }
            }
            query += " WHERE " + pkFieldName + " = " + pkValue;
            query += ";";

            return query;
        }

        public static string BuildLoadForeignFieldQuery(PersistedForeignField foreignField, List<object> valuesList)
        {
            var query = "SELECT ";
            query += foreignField.DbForeignKey;
            query += ",";
            query += foreignField.DbForeignFieldName;
            query += " FROM ";
            query += foreignField.ForeignTableName;
            query += " WHERE ";
            query += foreignField.DbForeignKey;
            query += " IN (";
            var first = true;
            foreach (var value in valuesList)
            {
                if (first) first = false;
                else query += ",";
                query += PersistenceHelperFunctions.FormatValue(value, typeof(long));
            }
            query += ");";

            return query;
        }

        public static string BuildLoadForeignQuery(Type foreignType, string foreignKey,
            List<long> foreignKeys)
        {
            var where = foreignKey;
            where += " IN (";
            var first = true;
            foreach (var value in foreignKeys)
            {
                if (first) first = false;
                else where += ",";
                where += PersistenceHelperFunctions.FormatValue(value, typeof(long));
            }
            where += ")";
            return BuildLoadAllQuery(foreignType, where);
        }

        public static string BuildDeleteForeignListQuery(PersistedForeignList foreignField,
            List<long> foreignKeys)
        {
            var query = "DELETE FROM ";
            query += PersistenceHelperFunctions.GetTableName(foreignField.ForeignType);
            query += " WHERE ";
            query += foreignField.DbForeignKey;
            query += " IN (";
            var first = true;
            foreach (var value in foreignKeys)
            {
                if (first) first = false;
                else query += ",";
                query += PersistenceHelperFunctions.FormatValue(value, typeof(long));
            }
            query += ");";
            return query;
        }

        public static string BuildCreateTableQuery(Type type)
        {
            var tableName = PersistenceHelperFunctions.GetTableName(type);
            if (string.IsNullOrEmpty(tableName)) return "";

            var persistedFields = PersistenceHelperFunctions.GetPersistedFields(type);
            if (persistedFields.Count == 0) return "";

            var query = "DROP TABLE IF EXISTS "+ tableName + ";";
            query += "CREATE TABLE " + tableName + "(";

            var pkFieldName = "";
            foreach (var persistedField in persistedFields)
            {
                query += persistedField.DbFieldName;
                query += " " + GetMySqlType(persistedField.CodeFieldType);
                if (persistedField.PrimaryKey)
                {
                    query += " NOT NULL AUTO_INCREMENT";
                    pkFieldName = persistedField.DbFieldName;
                }              
                query += ",";
            }

            query += "PRIMARY KEY (" + pkFieldName + ")";
            query += ")ENGINE = INNODB;";
            return query;
        }

        public static string GetMySqlType(Type fieldType)
        {
            if (fieldType == typeof(long)) return "BIGINT";
            if (fieldType == typeof(string)) return "VARCHAR(100)";
            if (fieldType == typeof(DateTime)) return "datetime";
            if (fieldType == typeof(bool)) return "tinyint(1)";
            if (fieldType == typeof(double)) return "double";
            if (fieldType.IsEnum) return "INT";
            return "";
        }
    }
}
