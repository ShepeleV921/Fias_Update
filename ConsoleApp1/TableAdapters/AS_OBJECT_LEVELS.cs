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
    internal class AS_OBJECT_LEVELS : IDisposable
    {
        public const string SQL_TABLE_NAME = "AS_OBJECT_LEVELS";
        public const string SQL_TABLE_TEMP_NAME = "AS_OBJECT_LEVELS_Temp";

        private readonly string _filesDir; // Путь к папке с файлами Xml
        private readonly DataTable _dt;
        private readonly SqlConnection _sqlConnection; // Подключение к источнику SQL
        private readonly XmlTextReader _reader; // Читатель данных из Xml-файла
        private readonly string _xmlFileName;
        private readonly bool _useMerge;
        private readonly string _sqlTableName;


        public AS_OBJECT_LEVELS(SqlConnection sqlConnection, string filesDirectory, string xmlFileName, bool useMerge)
        {

            _useMerge = useMerge;
            _xmlFileName = xmlFileName;
            _filesDir = filesDirectory;
            _sqlConnection = sqlConnection;
            _reader = new XmlTextReader(GetTablePath());
            _dt = new DataTable(_useMerge ? SQL_TABLE_TEMP_NAME : SQL_TABLE_NAME); // Источник данных для записи в базу

            // Порядок колонок должен строго совпадать с порядком в проекте таблицы Sql, т.к.SqlBulkCopy ориентируется на порядок колонок при вставке данных
            _dt.Columns.Add("LEVEL", typeof(string)).AllowDBNull = true;
            _dt.Columns.Add("NAME", typeof(string)).AllowDBNull = true;
            _dt.Columns.Add("STARTDATE", typeof(DateTime)).AllowDBNull = true;
            _dt.Columns.Add("ENDDATE", typeof(DateTime)).AllowDBNull = true;
            _dt.Columns.Add("UPDATEDATE", typeof(DateTime)).AllowDBNull = true;
            _dt.Columns.Add("ISACTIVE", typeof(Boolean)).AllowDBNull = true;


            XmlDocument doc = new XmlDocument();
            doc.Load(_filesDir + "\\" + _xmlFileName);



            // Преобразование Xml-файла в DataTable
            XmlNode Filas = doc.DocumentElement;
            var typeMap = new List<Type> { typeof(string), typeof(string), typeof(DateTime), typeof(DateTime), typeof(DateTime), typeof(Boolean) };
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
                SqlParameter p = cmd.Parameters.Add("@level", SqlDbType.UniqueIdentifier);

                for (int i = 0; i < _dt.Rows.Count; i++)
                {
                    int qResult = 0;
                    p.Value = _dt.Rows[i]["level"];

                    for (int k = 1; k < 100; k++)
                    {
                        try
                        {
                            cmd.CommandText = "DELETE FROM [dbo].[AS_OBJECT_LEVELS" +
                                k.ToString().PadLeft(2, '0') + "] WHERE [level] = @level";

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
	                [LEVEL] [nvarchar](2) NULL,
	                [NAME] [nvarchar](250) NULL,
	                [STARTDATE] [date] NULL,
	                [ENDDATE] [date] NULL,
                    [UPDATEDATE] [date] NULL,
	                [ISACTIVE] [bit] NULL)";
        }

        private string DropTempTableCommand()
        {
            return "DROP TABLE " + SQL_TABLE_TEMP_NAME;
        }


        private string MergeDataCommand()
        {
            return
     "MERGE INTO [AS_OBJECT_LEVELS] AS [Target] USING " + SQL_TABLE_TEMP_NAME + " AS [Source] ON [Target].[LEVEL] = [Source].[LEVEL] " +
    @"WHEN MATCHED THEN UPDATE SET 
					 [Target].[LEVEL]   = [Source].[LEVEL],
					 [Target].[NAME]       = [Source].[NAME],
					 [Target].[STARTDATE]   = [Source].[STARTDATE],
					 [Target].[ENDDATE]        = [Source].[ENDDATE],
                     [Target].[UPDATEDATE]       = [Source].[UPDATEDATE],
					 [Target].[ISACTIVE]        = [Source].[ISACTIVE]
    WHEN NOT MATCHED THEN 
    INSERT 
	     ([LEVEL],
          [NAME],
          [STARTDATE],
          [ENDDATE],
          [UPDATEDATE],
          [ISACTIVE])
     VALUES
           ([Source].[LEVEL],
                [Source].[NAME],
                [Source].[STARTDATE],
                [Source].[ENDDATE],
                [Source].[UPDATEDATE],
                [Source].[ISACTIVE]);";
        }

        public static IEnumerable<FileInfo> GetHouseFiles(string dir)
        {
            return Directory.GetFiles(dir, "*.xml").
                             Select(x => new FileInfo(x)).
                             Where(x => x.Name.StartsWith("AS_OBJECT_LEVELS", StringComparison.InvariantCultureIgnoreCase) &&
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