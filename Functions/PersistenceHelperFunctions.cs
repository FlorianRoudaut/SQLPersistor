using SQLPersistor.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQLPersistor.Functions
{
    //Class to help with tags and types and reflection
    public class PersistenceHelperFunctions
    {
        public static string GetTableName(Type type)
        {
            var tableName = "";
            var typeAttributes = type.GetCustomAttributes(true);
            foreach (var attribute in typeAttributes)
            {
                var persistedClass = attribute as PersistedClass;
                if (persistedClass != null)
                {
                    tableName = persistedClass.TableName;
                }
            }
            return tableName;
        }

        public static List<PersistedField> GetPersistedFields(Type type)
        {
            var fields = type.GetProperties();
            var persistedFields = new List<PersistedField>();
            foreach (var field in fields)
            {
                var customAttributes = field.GetCustomAttributes(true);
                foreach (var attribute in customAttributes)
                {
                    var persistedField = attribute as PersistedField;
                    if (persistedField == null) continue;

                    if (string.IsNullOrEmpty(persistedField.DbFieldName))
                        persistedField.DbFieldName = field.Name;
                    persistedField.CodeFieldName = field.Name;
                    persistedField.CodeFieldType = field.PropertyType;
                    persistedFields.Add(persistedField);
                }
            }
            return persistedFields;
        }

        public static List<PersistedForeignField> GetPersistedForeignFields(Type type)
        {
            var fields = type.GetProperties();
            var persistedFields = new List<PersistedForeignField>();
            foreach (var field in fields)
            {
                var customAttributes = field.GetCustomAttributes(true);
                foreach (var attribute in customAttributes)
                {
                    var persistedField = attribute as PersistedForeignField;
                    if (persistedField == null) continue;

                    if (string.IsNullOrEmpty(persistedField.DbForeignFieldName))
                        persistedField.DbForeignFieldName = field.Name;
                    persistedField.CodeForeignFieldName = field.Name;
                    persistedFields.Add(persistedField);
                }
            }
            return persistedFields;
        }

        public static List<PersistedForeignList> GetPersistedForeignLists(Type type)
        {
            var fields = type.GetProperties();
            var persistedLists = new List<PersistedForeignList>();
            foreach (var field in fields)
            {
                var customAttributes = field.GetCustomAttributes(true);
                foreach (var attribute in customAttributes)
                {
                    var persistedList = attribute as PersistedForeignList;
                    if (persistedList == null) continue;
                    persistedList.CodeFieldName = field.Name;
                    persistedLists.Add(persistedList);
                }
            }
            return persistedLists;
        }

        public static List<PersistedForeignClass> GetPersistedForeignClass(Type type)
        {
            var fields = type.GetProperties();
            var persistedClasses = new List<PersistedForeignClass>();
            foreach (var field in fields)
            {
                var customAttributes = field.GetCustomAttributes(true);
                foreach (var attribute in customAttributes)
                {
                    var persistedList = attribute as PersistedForeignClass;
                    if (persistedList == null) continue;
                    persistedList.CodeFieldName = field.Name;
                    persistedList.ForeignType = field.PropertyType;
                    persistedClasses.Add(persistedList);
                }
            }
            return persistedClasses;
        }

        public static long GetPrimaryKeyValue(Type type, object obj)
        {
            var fields = type.GetProperties();
            var persistedFields = new List<PersistedField>();
            foreach (var field in fields)
            {
                var customAttributes = field.GetCustomAttributes(true);
                foreach (var attribute in customAttributes)
                {
                    var persistedField = attribute as PersistedField;
                    if (persistedField == null || !persistedField.PrimaryKey) continue;
                    persistedField.CodeFieldName = field.Name;
                    persistedField.CodeFieldType = field.PropertyType;

                    var pkObj = GetSqlFormatedFieldValue(obj, persistedField);
                    long pk;
                    if (pkObj != null && long.TryParse(pkObj.ToString(), out pk)) return pk;
                    return 0;
                }
            }
            return 0;
        }

        public static string GetSqlFormatedFieldValue(object obj, PersistedField persistedField)
        {
            var value = GetFieldValue(obj, persistedField.CodeFieldName);
            return FormatValue(value, persistedField.CodeFieldType);
        }

        public static object GetFieldValue(object obj, string fieldName)
        {
            var type = obj.GetType();
            var fieldInfo = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                return "";
            }
            return fieldInfo.GetValue(obj);
        }

        public static string FormatValue(object value, Type fieldType)
        {
            if (value == null) return "''";
            if (fieldType == typeof(string))
            {
                return "'" + MySQLEscape(value.ToString()) + "'";
            }
            else if (fieldType == typeof(DateTime))
            {
                var date = (DateTime)value;
                //'Y-m-d H:i:s'
                var formatedDate = "'" + date.Year + "-" + date.Month + "-" + date.Day;
                if (date.Second != 0 && date.Minute != 0 && date.Hour != 0)
                {
                    formatedDate += " " + date.Hour + ":" + date.Minute + ":" + date.Second;
                }
                formatedDate += "'";
                return formatedDate;
            }
            else if (fieldType.IsEnum)
            {
                return ((int)value).ToString();
            }
            else if(fieldType == typeof(byte[]))
            {
                return "x'" + BitConverter.ToString((byte[])value).Replace("-", "") + "'";
            }
            return value.ToString();
        }

        private static string MySQLEscape(string str)
        {
            return Regex.Replace(str, @"[\x00'""\b\n\r\t\cZ\\%_]",
                delegate (Match match)
                {
                    string v = match.Value;
                    switch (v)
                    {
                        case "\x00":            // ASCII NUL (0x00) character
                    return "\\0";
                        case "\b":              // BACKSPACE character
                    return "\\b";
                        case "\n":              // NEWLINE (linefeed) character
                    return "\\n";
                        case "\r":              // CARRIAGE RETURN character
                    return "\\r";
                        case "\t":              // TAB
                    return "\\t";
                        case "\u001A":          // Ctrl-Z
                    return "\\Z";
                        default:
                            return "\\" + v;
                    }
                });
        }
    }
}
