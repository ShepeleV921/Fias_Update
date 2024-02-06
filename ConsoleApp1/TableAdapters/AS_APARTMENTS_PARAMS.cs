using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace FIAS.TableAdapters
{
    internal class AS_APARTMENTS_PARAMS : IDisposable
    {
        public const string SQL_TABLE_NAME = "AS_APARTMENTS_PARAMS";
        public const string SQL_TABLE_TEMP_NAME = "AS_APARTMENTS_PARAMS_Temp";

        private readonly string _filesDir; // Путь к папке с файлами Xml
        private readonly DataTable _dt;
        private readonly SqlConnection _sqlConnection; // Подключение к источнику SQL
        private readonly XmlTextReader _reader; // Читатель данных из Xml-файла
        private readonly string _xmlFileName;
        private readonly bool _useMerge;
        private readonly string _sqlTableName;


        public AS_APARTMENTS_PARAMS(SqlConnection sqlConnection, string filesDirectory, string xmlFileName, bool useMerge)
        {

            _useMerge = useMerge;
            _xmlFileName = xmlFileName;
            _filesDir = filesDirectory;
            _sqlConnection = sqlConnection;
            _reader = new XmlTextReader(GetTablePath());
            _dt = new DataTable(_useMerge ? SQL_TABLE_TEMP_NAME : SQL_TABLE_NAME); // Источник данных для записи в базу

            // Порядок колонок должен строго совпадать с порядком в проекте таблицы Sql, т.к.SqlBulkCopy ориентируется на порядок колонок при вставке данных
            _dt.Columns.Add("ID", typeof(string)).AllowDBNull = true;
            _dt.Columns.Add("OBJECTID", typeof(string)).AllowDBNull = true;
            _dt.Columns.Add("CHANGEID", typeof(string)).AllowDBNull = true;
            _dt.Columns.Add("CHANGEIDEND", typeof(string)).AllowDBNull = true;
            _dt.Columns.Add("TYPEID", typeof(string)).AllowDBNull = true;
            _dt.Columns.Add("VALUE", typeof(string)).AllowDBNull = true;
            _dt.Columns.Add("UPDATEDATE", typeof(DateTime)).AllowDBNull = true;
            _dt.Columns.Add("STARTDATE", typeof(DateTime)).AllowDBNull = true;
            _dt.Columns.Add("ENDDATE", typeof(DateTime)).AllowDBNull = true;


            XmlDocument doc = new XmlDocument();
            doc.Load(_filesDir + "\\" + _xmlFileName);



            // Преобразование Xml-файла в DataTable
            XmlNode Filas = doc.DocumentElement;
            var typeMap = new List<Type> { typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(DateTime), typeof(DateTime) };
            foreach (XmlNode Fila in Filas.ChildNodes)
            {
                int delta = 0;
                List<string> Valores = new List<string>();
                List<object> Values = new List<object>();
                for (int i = 0; i < _dt.Columns.Count; i++)
                {
                    if (Fila.Attributes.Count > (i - delta))
                    {

                        if (Fila.Attributes[i - delta].Name == _dt.Columns[i].ColumnName)
                        {
                            Valores.Add(Fila.Attributes[i - delta].Value);

                            switch (typeMap[i].Name)
                            {
                                case "Boolean":

                                    Values.Add(Fila.Attributes[i - delta].Value == "1" ? true : false);

                                    break;

                                default:

                                    Values.Add(Convert.ChangeType(Fila.Attributes[i - delta].Value, typeMap[i]));

                                    break;
                            }



                        }
                        else
                        {
                            Values.Add(null);
                            delta++;
                        }
                    }

                }
                _dt.Rows.Add(Values.ToArray());
            }
        }

        public string GetTablePath()
        {
            return System.IO.Path.Combine(_filesDir, _xmlFileName);
        }



        public void UpsertToServer()
        {
            using (SqlTransaction trn = _sqlConnection.BeginTransaction())
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(_sqlConnection, SqlBulkCopyOptions.TableLock, trn)
            {
                DestinationTableName = _useMerge ? SQL_TABLE_TEMP_NAME : SQL_TABLE_NAME
            })


            using (SqlCommand cmd = new SqlCommand() { Connection = _sqlConnection, Transaction = trn })

            {
                if (_useMerge)
                {
                    cmd.CommandTimeout = 0;
                    cmd.CommandText = CreateTempTableCommand();
                    cmd.ExecuteNonQuery(); // Создание временной таблицы
                }

                bulkCopy.WriteToServer(_dt); // Запись данных во временную таблицу

                if (_useMerge)
                {
                    cmd.CommandTimeout = 0;
                    cmd.CommandText = MergeDataCommand();
                    cmd.ExecuteNonQuery(); // Слияние данных из временной таблицы в основную

                    cmd.CommandText = DropTempTableCommand();
                    cmd.ExecuteNonQuery(); // Удаление временной 
                }

                _dt.Clear(); // Очистка источника

                trn.Commit(); // Исполнение транзакции
            }
        }




        public void DeleteFromServer()
        {
            using (SqlTransaction trn = _sqlConnection.BeginTransaction())
            using (SqlCommand cmd = new SqlCommand(string.Empty, _sqlConnection, trn))
            {
                SqlParameter p = cmd.Parameters.Add("@ID", SqlDbType.UniqueIdentifier);

                for (int i = 0; i < _dt.Rows.Count; i++)
                {
                    int qResult = 0;
                    p.Value = _dt.Rows[i]["ID"];

                    for (int k = 1; k < 100; k++)
                    {
                        try
                        {
                            cmd.CommandText = "DELETE FROM [dbo].[AS_APARTMENTS_PARAMS" +
                                k.ToString().PadLeft(2, '0') + "] WHERE [ID] = @ID";

                            qResult = cmd.ExecuteNonQuery();
                            if (qResult > 0)
                                break;
                        }
                        catch
                        {
                            ; // В нумерации таблиц могут быть пропуски
                        }
                    }
                }

                _dt.Clear(); // Очистка источника

                try
                {
                    trn.Commit(); // Исполнение транзакции
                }
                catch
                {
                    trn.Rollback();
                    throw;
                }
            }
        }


        private string CreateTempTableCommand()
        {
            return
                "CREATE TABLE " + SQL_TABLE_TEMP_NAME + @" (
	                [ID] [nvarchar](19) NULL,
	                [OBJECTID] [nvarchar](19) NULL,
	                [CHANGEID] [nvarchar](36) NULL,
	                [CHANGEIDEND] [nvarchar](19) NULL,
	                [TYPEID] [nvarchar](19) NULL,
	                [VALUE] [nvarchar](250) NULL,
	                [UPDATEDATE] [date] NULL,
	                [STARTDATE] [date] NULL,
	                [ENDDATE] [date] NULL)";
        }

        private string DropTempTableCommand()
        {
            return "DROP TABLE " + SQL_TABLE_TEMP_NAME;
        }


        private string MergeDataCommand()
        {
            return
     "MERGE INTO [AS_APARTMENTS_PARAMS] AS [Target] USING " + SQL_TABLE_TEMP_NAME + " AS [Source] ON [Target].[ID] = [Source].[ID] " +
    @"WHEN MATCHED THEN UPDATE SET 
					 [Target].[ID]   = [Source].[ID],
					 [Target].[OBJECTID]       = [Source].[OBJECTID],
					 [Target].[CHANGEID]   = [Source].[CHANGEID],
					 [Target].[CHANGEIDEND]       = [Source].[CHANGEIDEND],
					 [Target].[TYPEID]   = [Source].[TYPEID],
					 [Target].[VALUE]        = [Source].[VALUE],
					 [Target].[UPDATEDATE]        = [Source].[UPDATEDATE],
					 [Target].[STARTDATE]   = [Source].[STARTDATE],
					 [Target].[ENDDATE]     = [Source].[ENDDATE]
    WHEN NOT MATCHED THEN 
    INSERT 
	     ([ID],
          [OBJECTID],
          [CHANGEID],
          [CHANGEIDEND],
          [TYPEID],
          [VALUE],
          [UPDATEDATE],
          [STARTDATE],
          [ENDDATE])
     VALUES
           ([Source].[ID],
                [Source].[OBJECTID],
                [Source].[CHANGEID],
                [Source].[CHANGEIDEND],
                [Source].[TYPEID],
                [Source].[VALUE],
                [Source].[UPDATEDATE],
                [Source].[STARTDATE],
                [Source].[ENDDATE]);";
        }

        public static IEnumerable<FileInfo> GetHouseFiles(string dir)
        {
            return Directory.GetFiles(dir, "*.xml").
                             Select(x => new FileInfo(x)).
                             Where(x => x.Name.StartsWith("AS_APARTMENTS_PARAMS", StringComparison.InvariantCultureIgnoreCase) &&
                                        char.IsDigit(x.Name[5]) &&
                                        char.IsDigit(x.Name[6])
                                  );
        }



        public void Dispose()
        {
            if (_reader != null)
                // _reader.Dispose();

                _dt.Clear();
        }

    }
}

