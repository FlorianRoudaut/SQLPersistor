using SQLPersistor.Attributes;
using SQLPersistor.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SQLPersistor.Functions
{
    //High Level class for peristence, Handle class complexity
    public static class Persistor<T> where T : class, new()
    {
        public static T Find(IDatabaseConnection dbConnection, long id)
        {
            try
            {
                var type = typeof(T);
                return Find(dbConnection, type, id) as T;
            }
            catch (Exception e)
            {
                var typeName = typeof(T).Name;
                var excep = new Exception("Error Loading " + typeName + " ID=" + id, e);
                return null;
            }
        }

        public static List<T> LoadAll(IDatabaseConnection dbConnection, string where = "")
        {
            try
            {
                var type = typeof(T);
                var query = QueryBuilder.BuildLoadAllQuery(type, where);

                var list = new List<object>();
                var results = dbConnection.SelectQuery("LoadAll" + type, "LoadAll" + type, query);
                foreach (var result in results)
                {
                    var obj = BuildObject(type, result);
                    if (obj != null) list.Add(obj);
                }

                LoadForeignFields(dbConnection, type, list);
                LoadForeignLists(dbConnection, type, list);
                LoadForeignClasses(dbConnection, type, list);

                return list.Select(t => t as T).ToList();
            }
            catch (Exception e)
            {
                var typeName = typeof(T).Name;
                var excep = new Exception("Error Loading " + typeName + " where " + where, e);
                return new List<T>();
            }

        }

        public static T SaveOrUpdate(IDatabaseConnection dbConnection, T obj)
        {
            dbConnection.BeginTransaction();
            long pkValue = 0;
            try
            {
                var type = typeof(T);
                pkValue = PersistenceHelperFunctions.GetPrimaryKeyValue(type, obj);
                if (pkValue <= 0)
                {
                    long newId;
                    var newObj = Insert(dbConnection, type, obj, out newId) as T;
                    InsertForeignLists(dbConnection, type, obj, newObj, newId);
                    InsertForeignClasses(dbConnection, type, newObj, newId);

                    dbConnection.Commit();
                    return newObj;
                }
                else
                {
                    var query = QueryBuilder.BuildUpdateQuery(type, obj);
                    dbConnection.ExecuteQuery("Save" + type, "Update" + type, query);
                    UpdateForeignLists(dbConnection, type, obj);
                    UpdateForeignClasses(dbConnection, type, obj);

                    dbConnection.Commit();
                    return obj;
                }
            }
            catch (Exception e)
            {
                var typeName = typeof(T).Name;
                var excep = new Exception("Error Saving " + typeName + " with ID=" + pkValue, e);
                dbConnection.Rollback();
                return obj;
            }
        }

        public static void Delete(IDatabaseConnection dbConnection, List<T> tList)
        {
            if (tList == null || tList.Count == 0) return;
            dbConnection.BeginTransaction();

            try
            {
                var type = typeof(T);
                var idsList = new List<long>();
                foreach (var t in tList)
                {
                    var pk = PersistenceHelperFunctions.GetPrimaryKeyValue(type, t);
                    idsList.Add(pk);
                }

                Delete(dbConnection, type, idsList);
                dbConnection.Commit();
            }
            catch (Exception e)
            {
                var typeName = typeof(T).Name;
                var excep = new Exception("Error Deleting " + typeName, e);
                dbConnection.Rollback();
            }
        }

        private static object BuildObject(Type type, Dictionary<int, object> result)
        {
            try
            {
                var newObj = Activator.CreateInstance(type);

                var persistedFields = PersistenceHelperFunctions.GetPersistedFields(type);
                var row = 0;
                foreach (var persistedField in persistedFields)
                {
                    try
                    {
                        var value = result[row];
                        row = row + 1;
                        if (value == null || value is DBNull) continue;
                        var fieldInfo = type.GetProperty(persistedField.CodeFieldName, BindingFlags.Public
                            | BindingFlags.Instance);
                        if (fieldInfo.PropertyType.IsEnum)
                        {
                            var enumValue = Enum.ToObject(fieldInfo.PropertyType, value);
                            fieldInfo.SetValue(newObj, enumValue);
                        }
                        else
                        {
                            fieldInfo.SetValue(newObj, value);
                        }
                    }
                    catch (Exception e)
                    {
                        var ex = new Exception("Cannot set field " + persistedField.CodeFieldName +
                            " on object of type " + type.Name, e);
                    }
                }
                return newObj;
            }
            catch (Exception e)
            {
                var excep = new Exception("Cannot build object of type " + type.Name, e);
                return null;
            }
        }

        private static object Find(IDatabaseConnection dbConnection, Type type, long id)
        {
            var query = QueryBuilder.BuildLoadQuery(type, id);
            var results = dbConnection.SelectQuery("Find" + type, "Find" + type, query);
            foreach (var result in results)
            {
                var obj = BuildObject(type, result);
                LoadForeignFields(dbConnection, type, new List<object> { obj });
                LoadForeignLists(dbConnection, type, new List<object> { obj });
                return obj;
            }
            return null;
        }

        public static List<long> LoadAllIds(IDatabaseConnection dbConnection, Type type, string where = "")
        {
            var query = QueryBuilder.BuildLoadAllIdsQuery(type, where);

            var list = new List<long>();
            var results = dbConnection.SelectQuery("LoadAll" + type, "LoadAll" + type, query);
            foreach (var result in results)
            {
                var obj = result[0];
                long id;
                if (obj != null && long.TryParse(obj.ToString(), out id)) list.Add(id);
            }
            return list;
        }

        private static void LoadForeignFields(IDatabaseConnection dbConnection, Type type, List<object> list)
        {
            var foreignFields = PersistenceHelperFunctions.GetPersistedForeignFields(type);
            if (foreignFields.Count == 0) return;

            foreach (var foreignField in foreignFields)
            {
                LoadForeignField(dbConnection, list, foreignField);
            }
        }

        private static void LoadForeignField(IDatabaseConnection dbConnection, List<object> list,
            PersistedForeignField foreignField)
        {
            var type = typeof(T);

            //Get the list of all code values;
            var valuesList = new List<object>();
            var codeForeignKey = foreignField.CodeForeignKey;
            foreach (var obj in list)
            {
                var fk = PersistenceHelperFunctions.GetFieldValue(obj, codeForeignKey);
                if (fk != null) valuesList.Add(fk);
            }
            if (valuesList.Count == 0) return;

            //Run a query to load all values
            var query = QueryBuilder.BuildLoadForeignFieldQuery(foreignField, valuesList);

            var results = dbConnection.SelectQuery("LoadAll" + type, "Load" + foreignField.CodeForeignFieldName,
                query);

            //Create a result dictionnary with all values
            var dict = new Dictionary<long, string>();
            foreach (var result in results)
            {
                if (result.Count != 2) continue;

                var idObj = result[0];
                var valueObj = result[1];
                long idLong;
                if (idObj == null || !long.TryParse(idObj.ToString(), out idLong)) continue;
                if (valueObj == null) continue;
                dict[idLong] = valueObj.ToString();
            }

            //Insert the values in the list
            foreach (var obj in list)
            {
                var fkObj = PersistenceHelperFunctions.GetFieldValue(obj, codeForeignKey);
                long fkLong;
                if (fkObj == null || !long.TryParse(fkObj.ToString(), out fkLong)) continue;
                string value;
                if (!dict.TryGetValue(fkLong, out value)) continue;
                var fieldInfo = type.GetProperty(foreignField.CodeForeignFieldName, BindingFlags.Public
                    | BindingFlags.Instance);
                fieldInfo.SetValue(obj, value);
            }
        }

        private static void LoadForeignClasses(IDatabaseConnection dbConnection, Type type,
            List<object> list)
        {
            var foreignClasses = PersistenceHelperFunctions.GetPersistedForeignClass(type);
            if (foreignClasses.Count == 0) return;

            foreach (var foreignClass in foreignClasses)
            {
                LoadForeignClass(dbConnection, type, list, foreignClass);
            }
        }

        private static void LoadForeignClass(IDatabaseConnection dbConnection, Type type, List<object> list,
                PersistedForeignClass foreignClass)
        {
            var foreignType = foreignClass.ForeignType;

            //Get the list of all objects pks
            var valuesList = new List<long>();
            foreach (var obj in list)
            {
                var pk = PersistenceHelperFunctions.GetPrimaryKeyValue(type, obj);
                if (pk != 0) valuesList.Add(pk);
            }
            if (valuesList.Count == 0) return;

            //Run a query to load all values
            var query = QueryBuilder.BuildLoadForeignQuery(foreignClass.ForeignType, foreignClass.DbForeignKey, valuesList);
            var results = dbConnection.SelectQuery("LoadAll" + type, "Load" + foreignClass.ForeignType.Name,
                query);

            //Sort the results
            var dict = new Dictionary<long, object>();
            foreach (var result in results)
            {
                var foreignObj = BuildObject(foreignType, result);
                var fkObj = PersistenceHelperFunctions.GetFieldValue(foreignObj,
                    foreignClass.CodeForeignKey);
                long fkLong;
                if (fkObj != null && long.TryParse(fkObj.ToString(), out fkLong))
                {
                    dict[fkLong] = foreignObj;
                }
            }
            //Add the loaded objects to the list
            foreach (var obj in list)
            {
                var pk = PersistenceHelperFunctions.GetPrimaryKeyValue(type, obj);
                object objectToAdd;
                if (dict.TryGetValue(pk, out objectToAdd))
                {
                    var fieldInfo = type.GetProperty(foreignClass.CodeFieldName, BindingFlags.Public
                        | BindingFlags.Instance);
                    fieldInfo.SetValue(obj, objectToAdd);
                }
            }
        }

        private static void LoadForeignLists(IDatabaseConnection dbConnection, Type type, List<object> list)
        {
            var foreignLists = PersistenceHelperFunctions.GetPersistedForeignLists(type);
            if (foreignLists.Count == 0) return;

            foreach (var foreignList in foreignLists)
            {
                LoadForeignList(dbConnection, type, list, foreignList);
            }
        }

        private static void LoadForeignList(IDatabaseConnection dbConnection, Type type, List<object> list,
            PersistedForeignList foreignList)
        {
            var foreignType = foreignList.ForeignType;

            //Get the list of all objects pks
            var valuesList = new List<long>();
            foreach (var obj in list)
            {
                var pk = PersistenceHelperFunctions.GetPrimaryKeyValue(type, obj);
                if (pk != 0) valuesList.Add(pk);
            }
            if (valuesList.Count == 0) return;

            //Run a query to load all values
            var query = QueryBuilder.BuildLoadForeignQuery(foreignList.ForeignType, foreignList.DbForeignKey,
                valuesList);

            var results = dbConnection.SelectQuery("LoadAll" + type, "Load" + foreignList.ForeignType.Name,
                query);

            //Sort the results
            var dict = new Dictionary<long, List<object>>();
            foreach (var result in results)
            {
                var foreignObj = BuildObject(foreignType, result);
                var fkObj = PersistenceHelperFunctions.GetFieldValue(foreignObj,
                    foreignList.CodeForeignKey);
                long fkLong;
                if (fkObj != null && long.TryParse(fkObj.ToString(), out fkLong))
                {
                    if (dict.ContainsKey(fkLong)) dict[fkLong].Add(foreignObj);
                    else dict[fkLong] = new List<object> { foreignObj };
                }
            }
            //Add the loaded objects to the list
            foreach (var obj in list)
            {
                var listObj = PersistenceHelperFunctions.GetFieldValue(obj, foreignList.CodeFieldName);
                var listToFill = (IList)listObj;
                listToFill.Clear();
                var pk = PersistenceHelperFunctions.GetPrimaryKeyValue(type, obj);
                List<object> objectsToAdd;
                if (dict.TryGetValue(pk, out objectsToAdd))
                {
                    foreach (var foreignObj in objectsToAdd)
                    {
                        listToFill.Add(foreignObj);
                    }
                }
            }
        }

        private static void Delete(IDatabaseConnection dbConnection, Type type, List<long> idsList)
        {
            DeleteForeignLists(dbConnection, type, idsList);
            var query = QueryBuilder.BuildDeleteQuery(type, idsList);
            dbConnection.ExecuteQuery("Delete" + type.Name, "Delete" + type.Name, query);
        }
        private static void DeleteForeignLists(IDatabaseConnection dbConnection, Type type,
            List<long> idsList)
        {
            var foreignLists = PersistenceHelperFunctions.GetPersistedForeignLists(type);
            if (foreignLists.Count == 0) return;

            foreach (var foreignList in foreignLists)
            {
                DeleteForeignList(dbConnection, type, idsList, foreignList);
            }
        }

        private static void DeleteForeignList(IDatabaseConnection dbConnection, Type type, List<long> idsList,
            PersistedForeignList foreignList)
        {
            var foreignType = foreignList.ForeignType;

            //Get the list of all objects pks
            if (idsList.Count == 0) return;

            //Run a query to load all values
            var query = QueryBuilder.BuildDeleteForeignListQuery(foreignList, idsList);
            dbConnection.ExecuteQuery("Delete" + foreignType.Name, "Delete" + foreignType.Name, query);
        }

        private static object Insert(IDatabaseConnection dbConnection, Type type, object obj, out long newId)
        {
            var query = QueryBuilder.BuildInsertQuery(type, new List<object> { obj });
            dbConnection.ExecuteQuery("Save" + type, "Insert" + type, query);
            var newIdQuery = "SELECT last_insert_id();";
            var result = dbConnection.SelectQuery("Save" + type, "GetNewId" + type, newIdQuery);
            newId = result.GetFirstLong();
            return Find(dbConnection, type, newId);
        }
        private static void InsertForeignLists(IDatabaseConnection dbConnection, Type type, T oldObj, T newObj, long newId)
        {
            var foreignLists = PersistenceHelperFunctions.GetPersistedForeignLists(type);
            if (foreignLists.Count == 0) return;

            foreach (var foreignList in foreignLists)
            {
                InsertForeignList(dbConnection, oldObj, newObj, newId, foreignList);
            }
        }

        private static void InsertForeignList(IDatabaseConnection dbConnection, T oldObj, T newObj, long newId,
            PersistedForeignList foreignList)
        {
            var foreignType = foreignList.ForeignType;

            var listObj = PersistenceHelperFunctions.GetFieldValue(oldObj, foreignList.CodeFieldName);
            var oldList = (IList)listObj;
            if (oldList.Count == 0) return;

            var newListObj = PersistenceHelperFunctions.GetFieldValue(newObj, foreignList.CodeFieldName);
            var newList = (IList)newListObj;
            newList.Clear();

            foreach (var oldChildObj in oldList)
            {
                var fieldInfo = foreignType.GetProperty(foreignList.CodeForeignKey, BindingFlags.Public
                    | BindingFlags.Instance);
                fieldInfo.SetValue(oldChildObj, newId);
                long newChildId;
                var newChildObj = Insert(dbConnection, foreignType, oldChildObj, out newChildId);
                newList.Add(newChildObj);
            }
        }

        private static void InsertForeignClasses(IDatabaseConnection dbConnection, Type type, T newObj, long newId)
        {
            var foreignClasses = PersistenceHelperFunctions.GetPersistedForeignClass(type);
            if (foreignClasses.Count == 0) return;

            foreach (var foreignClass in foreignClasses)
            {
                InsertForeignClass(dbConnection, type, newObj, newId, foreignClass);
            }
        }

        private static void InsertForeignClass(IDatabaseConnection dbConnection, Type type, T newObj, long newId,
            PersistedForeignClass foreignClass)
        {
            var foreignType = foreignClass.ForeignType;
            var oldForeignObj = PersistenceHelperFunctions.GetFieldValue(newObj, foreignClass.CodeFieldName);
            var fieldInfo = foreignType.GetProperty(foreignClass.CodeForeignKey, BindingFlags.Public
                | BindingFlags.Instance);
            fieldInfo.SetValue(oldForeignObj, newId);
            long newChildId;
            var newChildObj = Insert(dbConnection, foreignType, oldForeignObj, out newChildId);

            var fieldInfo2 = type.GetProperty(foreignClass.CodeFieldName, BindingFlags.Public
                | BindingFlags.Instance);
            fieldInfo2.SetValue(newObj, newChildObj);
        }

        private static void Update(IDatabaseConnection dbConnection, Type type, object obj)
        {
            var query = QueryBuilder.BuildUpdateQuery(type, obj);
            dbConnection.ExecuteQuery("Save" + type, "Update" + type, query);
            UpdateForeignLists(dbConnection, type, obj);
        }

        private static void UpdateForeignLists(IDatabaseConnection dbConnection, Type type, object obj)
        {
            var foreignLists = PersistenceHelperFunctions.GetPersistedForeignLists(type);
            if (foreignLists.Count == 0) return;

            foreach (var foreignList in foreignLists)
            {
                UpdateForeignList(dbConnection, type, obj, foreignList);
            }
        }

        private static void UpdateForeignList(IDatabaseConnection dbConnection, Type type, object obj,
            PersistedForeignList foreignList)
        {
            var foreignType = foreignList.ForeignType;

            var listObj = PersistenceHelperFunctions.GetFieldValue(obj, foreignList.CodeFieldName);
            var list = (IList)listObj;

            var parentId = PersistenceHelperFunctions.GetPrimaryKeyValue(type, obj);
            var where = foreignList.DbForeignKey + "=" + parentId;
            var oldIds = LoadAllIds(dbConnection, foreignType, where);
            var saveOrUpdatedIds = new List<long>();

            if (list.Count != 0)
            {
                var toReplace = new Dictionary<object, object>();
                foreach (var childObj in list)
                {
                    var childId = PersistenceHelperFunctions.GetPrimaryKeyValue(foreignType, childObj);
                    if (childId <= 0)
                    {
                        var fieldInfo = foreignType.GetProperty(foreignList.CodeForeignKey, BindingFlags.Public
                            | BindingFlags.Instance);
                        fieldInfo.SetValue(childObj, parentId);

                        long newChildId;
                        var newChildObj = Insert(dbConnection, foreignType, childObj, out newChildId);
                        toReplace[childObj] = newChildObj;

                    }
                    else
                    {
                        Update(dbConnection, foreignType, childObj);
                        saveOrUpdatedIds.Add(childId);
                    }
                }

                foreach (var kvp in toReplace)
                {
                    var index = list.IndexOf(kvp.Key);
                    list[index] = kvp.Value;
                }
            }

            var toRemoveIds = new List<long>();
            if (oldIds.Count != 0)
            {
                foreach (var oldId in oldIds)
                {
                    if (!saveOrUpdatedIds.Contains(oldId))
                    {
                        toRemoveIds.Add(oldId);
                    }
                }
            }

            if (toRemoveIds.Count > 0)
            {
                Delete(dbConnection, foreignType, toRemoveIds);
            }
        }

        private static void UpdateForeignClasses(IDatabaseConnection dbConnection, Type type, object obj)
        {
            var foreignClasses = PersistenceHelperFunctions.GetPersistedForeignClass(type);
            if (foreignClasses.Count == 0) return;

            foreach (var foreignClass in foreignClasses)
            {
                UpdateForeignClass(dbConnection, type, obj, foreignClass);
            }
        }
        private static void UpdateForeignClass(IDatabaseConnection dbConnection, Type type, object obj,
        PersistedForeignClass foreignClass)
        {
            var foreignType = foreignClass.ForeignType;

            var childObj = PersistenceHelperFunctions.GetFieldValue(obj, foreignClass.CodeFieldName);
            Update(dbConnection, foreignType, childObj);
        }
    }
}
