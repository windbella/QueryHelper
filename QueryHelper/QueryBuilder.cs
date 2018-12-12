using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenderHelper.Database
{
    public class QueryBuilder
    {
        // INSERT문 생성 시 null대신 default를 사용
        public static bool UseDefaultMode { get; set; }
        // 오라클 형태의 날짜 포맷 사용
        public static bool UseOracleMode { get; set; }
        // UPDATE문 생성 시 테이블을 비교할 때 Trim 후 비교
        public static bool UseTrim { get; set; }


        // 테이블 INSERT ( 매개 변수의 데이터를 삽입 )
        public static string BuildInsert(DataTable dataTable)
        {
            StringBuilder query = new StringBuilder();
            query.Append(BuildInsertHeader(dataTable));

            int index = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                index++;
                string format = index < dataTable.Rows.Count ? "{0}, " : "{0};";
                query.AppendFormat(format, BuildInsertValue(row));
            }

            if (index == 0)
            {
                query.Clear();
            }
            return query.ToString();
        }

        // 테이블 로우 INSERT ( 매개 변수의 데이터를 삭제 )
        public static string BuildInsert(DataRow dataRow)
        {
            StringBuilder query = new StringBuilder();
            query.Append(BuildInsertHeader(dataRow.Table));
            query.AppendFormat("{0};", BuildInsertValue(dataRow));
            return query.ToString();
        }

        // 삽입문 앞 부분 생성
        private static string BuildInsertHeader(DataTable dataTable)
        {
            StringBuilder query = new StringBuilder();
            query.AppendFormat("INSERT INTO {0} ( ", dataTable.TableName);

            int index = 0;
            foreach (DataColumn column in dataTable.Columns)
            {
                index++;
                string format = index < dataTable.Columns.Count ? "{0}, " : "{0} ) ";
                query.AppendFormat(format, column.ColumnName);
            }

            query.Append("VALUES ");

            return query.ToString();
        }

        // 삽입문 값 부분 생성
        private static string BuildInsertValue(DataRow dataRow)
        {
            StringBuilder query = new StringBuilder();
            int index = 0;
            query.Append("( ");
            foreach (DataColumn column in dataRow.Table.Columns)
            {
                index++;
                string format = index < dataRow.Table.Columns.Count ? "{0}, " : "{0} )";
                if (UseDefaultMode)
                {
                    query.AppendFormat(format, Convert(dataRow[column.ColumnName]).Replace("null", "default"));
                }
                else
                {
                    query.AppendFormat(format, Convert(dataRow[column.ColumnName]));
                }
            }
            return query.ToString();
        }

        // 테이블 DELETE ( 조건 사용 )
        public static string BuildDelete(DataTable dataTable, string whereClause)
        {
            if (string.Empty.Equals(whereClause))
            {
                return string.Empty;
            }
            string query = string.Format("DELETE FROM {0} WHERE {1};", dataTable.TableName, whereClause);
            return query;
        }

        // 테이블 DELETE ( 매개 변수의 데이터 삭제 )
        public static string BuildDelete(DataTable dataTable)
        {
            string query = string.Empty;
            if (dataTable.Rows.Count > 0)
            {
                StringBuilder whereClause = new StringBuilder();
                int index = 0;
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    index++;
                    string format = index < dataTable.Rows.Count ? "( {0} ) OR " : "( {0} )";
                    whereClause.AppendFormat(format, BuildDeleteWhereClause(dataRow));
                }
                query = BuildDelete(dataTable, whereClause.ToString());
            }
            return query;
        }

        // 테이블 로우 DELETE ( 매개 변수의 데이터 삭제 )
        public static string BuildDelete(DataRow dataRow)
        {
            string query = BuildDelete(dataRow.Table, BuildDeleteWhereClause(dataRow));
            return query;
        }

        // 삭제문 조건 생성
        private static string BuildDeleteWhereClause(DataRow dataRow)
        {
            StringBuilder whereClause = new StringBuilder();
            int index = 0;
            foreach (DataColumn primayKey in dataRow.Table.PrimaryKey)
            {
                index++;
                string format = index < dataRow.Table.PrimaryKey.Length ? "{0} = {1} AND " : "{0} = {1}";
                whereClause.AppendFormat(format, primayKey.ColumnName, Convert(dataRow[primayKey.ColumnName]));
            }
            return whereClause.ToString();
        }

        // 테이블 업데이트 ( 매개 변수의 두 테이블을 비교하여 변경된 내용 삽입/삭제/수정 )
        public static string BuildUpdate(DataTable sourceDataTable, DataTable targetDataTable, IEnumerable<string> exceptedColumn = null)
        {
            StringBuilder query = new StringBuilder();

            DataTable tempSource = targetDataTable.Clone();
            DataTable tempTarget = targetDataTable.Copy();

            // DB 타입이 다른 경우 데이터 값을 맞춰주는 역할 ( ex. TRUE, FALSE - 0, 1 )
            foreach (DataRow dataRow in sourceDataTable.Rows)
            {
                tempSource.ImportRow(dataRow);
            }

            foreach (DataRow sourceRow in tempSource.Rows)
            {
                string filterExpression = string.Empty;
                int index = 0;
                foreach (DataColumn primayKey in tempSource.PrimaryKey)
                {
                    index++;
                    string format = index < tempSource.PrimaryKey.Length ? "{0} = {1} AND " : "{0} = {1}";
                    filterExpression += string.Format(format, primayKey.ColumnName, Convert(sourceRow[primayKey.ColumnName]));
                }
                DataRow selectedRow = null;
                foreach (DataRow targetRow in tempTarget.Select(filterExpression))
                {
                    selectedRow = sourceRow;
                    if (exceptedColumn != null)
                    {
                        foreach (string column in exceptedColumn)
                        {
                            sourceRow[column] = targetRow[column];
                        }
                    }
                    if (!DataRowComparer.Default.Equals(sourceRow, targetRow))
                    {
                        query.Append(BuildUpdate(selectedRow, targetRow));
                    }
                    tempTarget.Rows.Remove(targetRow);
                    break;
                }
                if (selectedRow == null)
                {
                    query.Append(BuildInsert(sourceRow));
                }
            }

            DataTable deleteTable = tempTarget.Clone();
            foreach (DataRow targetRow in tempTarget.Rows)
            {
                deleteTable.ImportRow(targetRow);
            }
            query.Append(BuildDelete(deleteTable));

            return query.ToString();
        }

        // 테이블 업데이트 ( 매개 변수의 데이터로 수정 )
        public static string BuildUpdate(DataRow dataRow, DataRow compareRow)
        {
            StringBuilder query = new StringBuilder();
            query.AppendFormat("UPDATE {0} SET ", dataRow.Table.TableName);

            int length = query.Length;

            foreach (DataColumn column in dataRow.Table.Columns)
            {
                if (!dataRow[column.ColumnName].Equals(compareRow[column.ColumnName]))
                {
                    query.AppendFormat("{0} = {1}, ", column.ColumnName, Convert(dataRow[column.ColumnName]));
                }
            }

            if (query.Length > length)
            {
                query.Remove(query.Length - 2, 2);
                query.Append(" WHERE ");
            }

            int index = 0;
            foreach (DataColumn primayKey in dataRow.Table.PrimaryKey)
            {
                index++;
                string format = index < dataRow.Table.PrimaryKey.Length ? "{0} = {1} AND " : "{0} = {1};";
                query.AppendFormat(format, primayKey.ColumnName, Convert(dataRow[primayKey.ColumnName]));
            }
            return query.ToString();
        }

        // 테이블 업데이트 ( 매개 변수의 데이터로 수정 )
        public static string BuildUpdate(DataRow dataRow)
        {
            StringBuilder query = new StringBuilder();
            query.AppendFormat("UPDATE {0} SET ", dataRow.Table.TableName);
            int index;

            index = 0;
            foreach (DataColumn column in dataRow.Table.Columns)
            {
                index++;
                string format = index < dataRow.Table.Columns.Count ? "{0} = {1}, " : "{0} = {1} WHERE ";
                query.AppendFormat(format, column.ColumnName, Convert(dataRow[column.ColumnName]));
            }
            index = 0;
            foreach (DataColumn primayKey in dataRow.Table.PrimaryKey)
            {
                index++;
                string format = index < dataRow.Table.PrimaryKey.Length ? "{0} = {1} AND " : "{0} = {1};";
                query.AppendFormat(format, primayKey.ColumnName, Convert(dataRow[primayKey.ColumnName]));
            }
            return query.ToString();
        }

        // 프로시저 호출
        public static string BuildExcuteProcedure(string procedureName, IEnumerable<object> parameters, string dbType)
        {
            StringBuilder query = new StringBuilder();
            switch (dbType)
            {
                case "MsSql":
                    query.AppendFormat("EXEC {0}", procedureName);
                    if (parameters != null && parameters.Count() > 0)
                    {
                        query.Append(" ");
                        int index = 0;
                        foreach (object parameter in parameters)
                        {
                            index++;
                            string format = index < parameters.Count() ? "{0}, " : "{0}";
                            query.AppendFormat(format, Convert(parameter));
                        }
                    }
                    query.Append(";");
                    break;
                case "MySql":
                    query.AppendFormat("CALL {0}", procedureName);
                    if (parameters != null && parameters.Count() > 0)
                    {
                        query.Append("(");
                        int index = 0;
                        foreach (object parameter in parameters)
                        {
                            index++;
                            string format = index < parameters.Count() ? "{0}, " : "{0}";
                            query.AppendFormat(format, Convert(parameter));
                        }
                        query.Append(")");
                    }
                    query.Append(";");
                    break;
            }
            return query.ToString();
        }

        private static CultureInfo cultureInfo = new CultureInfo("en-US");

        // DB 형식으로 데이터 가공
        private static string Convert(object value)
        {
            if (value is DateTime)
            {
                if (UseOracleMode)
                {
                    return "TO_DATE('" + ((DateTime)value).ToString("yyyyMMddHHmmss", cultureInfo) + "','YYYYMMDDHH24MISS')";
                }
                else
                {
                    return "'" + ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
                }
            }
            else if (value is TimeSpan)
            {
                return "'" + ((TimeSpan)value).ToString("HH:mm:ss") + "'";
            }
            else if (value is bool)
            {
                return "'" + (((bool)value) ? "1" : "0") + "'";
            }
            else if (value == DBNull.Value)
            {
                return "null";
            }
            return "'" + value.ToString() + "'";
        }
    }
}