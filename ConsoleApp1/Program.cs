using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.IO.Compression;
using System.Net;
using System.ComponentModel;
using FIAS.TableAdapters;

using FIAS;
using ConsoleApp1;
using Universe.IO.DownloadClient.Ftp;


namespace FiasLoad // Note: actual namespace depends on the project name.
{
    public class Root_All
    {
        [JsonProperty("VersionId")]
        public long VersionId { get; set; }

        [JsonProperty("TextVersion")]
        public string TextVersion { get; set; }

        [JsonProperty("FiasCompleteDbfUrl")]
        public string FiasCompleteDbfUrl { get; set; }

        [JsonProperty("FiasCompleteXmlUrl")]
        public string FiasCompleteXmlUrl { get; set; }

        [JsonProperty("FiasDeltaDbfUrl")]
        public string FiasDeltaDbfUrl { get; set; }

        [JsonProperty("FiasDeltaXmlUrl")]
        public string FiasDeltaXmlUrl { get; set; }

        [JsonProperty("Kladr4ArjUrl")]
        public string Kladr4ArjUrl { get; set; }

        [JsonProperty("Kladr47ZUrl")]
        public string Kladr47ZUrl { get; set; }

        [JsonProperty("GarXMLFullURL")]
        public Uri GarXmlFullUrl { get; set; }

        [JsonProperty("GarXMLDeltaURL")]
        public string GarXmlDeltaUrl { get; set; }

        [JsonProperty("Date")]
        public string Date { get; set; }
    }


    public class Root_Last
    {
        [JsonProperty("VersionId")]
        public int VersionId { get; set; }

        [JsonProperty("TextVersion")]
        public string TextVersion { get; set; }

        [JsonProperty("FiasCompleteDbfUrl")]
        public string FiasCompleteDbfUrl { get; set; }

        [JsonProperty("FiasCompleteXmlUrl")]
        public string FiasCompleteXmlUrl { get; set; }

        [JsonProperty("FiasDeltaDbfUrl")]
        public string FiasDeltaDbfUrl { get; set; }

        [JsonProperty("FiasDeltaXmlUrl")]
        public string FiasDeltaXmlUrl { get; set; }

        [JsonProperty("Kladr4ArjUrl")]
        public string Kladr4ArjUrl { get; set; }

        [JsonProperty("Kladr47ZUrl")]
        public string Kladr47ZUrl { get; set; }

        [JsonProperty("GarXMLFullURL")]
        public string GarXMLFullURL { get; set; }

        [JsonProperty("GarXMLDeltaURL")]
        public string GarXMLDeltaURL { get; set; }

        [JsonProperty("Date")]
        public string Date { get; set; }
    }


    internal class Program
    {

        static private readonly string ConnectionSQL = "Data Source=SIGMA\\SIGMA;Initial Catalog=fias;MultipleActiveResultSets=True";
        private static List<Root_All> Get_All;
        private static Root_Last Get_Last;
        private static List<Root_All>? ServiceData;
        public static int LastVerison;
        public static int СurrentUpdate;

        static async Task Main(string[] args)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                var SQL = new SqlCommand();
                SQL.CommandType = CommandType.Text;
                SQL.Connection = new SqlConnection(ConnectionSQL);
                SQL.Connection.Open();
                SQL.CommandText = "SELECT TOP(1) [LastVersionID] FROM [dbo].[DbInfo]";
                object _lastVersion = SQL.ExecuteScalar();
                SQL.Connection.Close();

                LastVerison = (int)_lastVersion;
                Console.WriteLine("последняя версия Фиас: " + _lastVersion);
                Logger.WriteLine("последняя версия Фиас: " + _lastVersion);

                //возвращает информацию в виде json обо всех версиях файлов, доступных для скачивания 

                HttpClient client = new HttpClient();
                string url = "https://fias.nalog.ru/WebServices/Public/GetLastDownloadFileInfo";
                HttpResponseMessage response = await client.GetAsync(url);
                string content = await response.Content.ReadAsStringAsync();
                Root_Last Fias_Updates_Last = JsonConvert.DeserializeObject<Root_Last>(content);
                Get_Last = Fias_Updates_Last;

                Console.WriteLine("Последнее актуальное обновление: " + Fias_Updates_Last.Date);
                Logger.WriteLine("Последнее актуальное обновление: " + Fias_Updates_Last.Date);

                //возвращает информацию о последней версии файлов, доступных для скачивания
                HttpClient client_1 = new HttpClient();
                string url_1 = "https://fias.nalog.ru/WebServices/Public/GetAllDownloadFileInfo";
                HttpResponseMessage response_1 = await client_1.GetAsync(url_1);
                string content_1 = await response_1.Content.ReadAsStringAsync();
                List<Root_All> Fias_Updates_All = JsonConvert.DeserializeObject<List<Root_All>>(content_1);
                Get_All = Fias_Updates_All;

                Console.WriteLine("Все актуальные версии: " + Fias_Updates_All.Count);
                Logger.WriteLine("Все актуальные версии: " + Fias_Updates_All.Count);

                //Поиск актуальной версии для скачивания 
                ServiceData = null;
                ServiceData = Get_All.Where(x => x.VersionId > LastVerison).
                                           OrderBy(x => x.VersionId).Take(1).ToList();

                Console.WriteLine("Доступные обновления базы Фиас: " + ServiceData[0].Date);
                Logger.WriteLine("Доступные обновления базы Фиас: " + ServiceData[0].Date);

                for (int k = 0; k < ServiceData.Count; k++)
                {

                    СurrentUpdate = (int)ServiceData[k].VersionId;
                    Console.WriteLine("Обновление: " + k + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Обновление: " + k + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                    var archivePath = ServiceData[k].GarXmlDeltaUrl;
                    using (WebClient wc = new WebClient())
                    {
                        // Скачивание delta-fias 
                        //wc.DownloadFile(archivePath, @"F:\Fias_update\gar_xml.zip");
                        wc.DownloadFile(archivePath, @"C:\VS project\1\gar_xml.zip");
                    }

                    Console.WriteLine("Обновление успешно скачано");
                    Logger.WriteLine("Обновление успешно скачано");


                    /*string zipFile = @"F:\Fias_update\gar_xml.zip";*/ // сжатый файл
                    string zipFile = @"C:\VS project\1\gar_xml.zip";

                    string tempDir = Path.GetTempPath();

                    // Создаем 4 потока
                    Thread thread1 = new Thread(DoWork1);
                    Thread thread2 = new Thread(DoWork2);
                    Thread thread3 = new Thread(DoWork3);
                    Thread thread4 = new Thread(DoWork4);
                    Thread thread5 = new Thread(DoWork5);
                    Thread thread6 = new Thread(DoWork6);
                    Thread thread7 = new Thread(DoWork7);
                    Thread thread8 = new Thread(DoWork8);
                    Thread thread9 = new Thread(DoWork9);
                    Thread thread10 = new Thread(DoWork10);

                    // Запускаем потоки
                    thread1.Start();
                    thread2.Start();
                    thread3.Start();
                    thread4.Start();
                    thread5.Start();
                    thread6.Start();
                    thread7.Start();
                    thread8.Start();
                    thread9.Start();
                    thread10.Start();

                    // Ждем, пока потоки завершат свою работу
                    thread1.Join();
                    thread2.Join();
                    thread3.Join();
                    thread4.Join();
                    thread5.Join();
                    thread6.Join();
                    thread7.Join();
                    thread8.Join();
                    thread9.Join();
                    thread10.Join();

                    Console.WriteLine("Все потоки завершили работу.");

                    FiasRepository.UpdateDbVersion(СurrentUpdate);
                    Console.WriteLine("Обновление успешно завершено: " + СurrentUpdate + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Обновление успешно завершено: " + СurrentUpdate + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
            catch (Exception ex)
            {

            }
        }

        #region Запуск 1-ого потока
        /// <summary>
        /// Работа с 1-ым потоком.
        /// </summary>
        static void DoWork1()
        {
            // Здесь должна быть ваша работа

            string zipFile = @"C:\VS project\1\gar_xml.zip";
            string tempDir = Path.GetTempPath();
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                List<ZipArchiveEntry> entries = archive.Entries.ToList();

                int size = entries.Count / 10;
                List<ZipArchiveEntry> part1 = entries.GetRange(0, size);
                List<ZipArchiveEntry> part2 = entries.GetRange(size, size);
                List<ZipArchiveEntry> part3 = entries.GetRange(size * 2, size);
                List<ZipArchiveEntry> part4 = entries.GetRange(size * 3, size);
                List<ZipArchiveEntry> part5 = entries.GetRange(size * 4, size);
                List<ZipArchiveEntry> part6 = entries.GetRange(size * 5, size);
                List<ZipArchiveEntry> part7 = entries.GetRange(size * 6, size);
                List<ZipArchiveEntry> part8 = entries.GetRange(size * 7, size);
                List<ZipArchiveEntry> part9 = entries.GetRange(size * 8, size);
                List<ZipArchiveEntry> part10 = entries.GetRange(size * 9, entries.Count - size * 9);


                // int size = archive.Count();
                foreach (ZipArchiveEntry entry in part1.OrderBy(x => x.FullName))
                {
                    Console.WriteLine("Поток 1" + " начал работу.");

                    Console.WriteLine("Загрузка файла: " + entry.FullName);
                    Logger.WriteLine("Загрузка файла: " + entry.FullName);
                    string fileName = entry.Name;
                    string filePath = Path.Combine(tempDir, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    entry.ExtractToFile(filePath);
                    Console.WriteLine("Распаковка файла.");
                    // Logger.WriteLine("Распаковка файла.");
                    ProcessFile(filePath);

                    try
                    {
                        // Файл больше не нужен, освобождаем место
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Если не получилось удалить сразу, то 
                        // вторая попытка будет при выходе из программы
                    }
                    Console.WriteLine("Поток 1" + " завершил работу.");
                }
            }
            if (File.Exists(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip"))
            {
                File.Delete(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip");
                Console.WriteLine("Файл с обновлением больше не нужен, освобождаем место");
                // Logger.WriteLine("Файл с обновлением больше не нужен, освобождаем место");

            }

        }
        #endregion

        #region Запуск 2-ого потока
        /// <summary>
        /// Работа с 2-ым потоком.
        /// </summary>
        static void DoWork2()
        {
            // Здесь должна быть ваша работа
            string zipFile = @"C:\VS project\1\gar_xml.zip";
            string tempDir = Path.GetTempPath();
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                List<ZipArchiveEntry> entries = archive.Entries.ToList();

                int size = entries.Count / 10;
                List<ZipArchiveEntry> part1 = entries.GetRange(0, size);
                List<ZipArchiveEntry> part2 = entries.GetRange(size, size);
                List<ZipArchiveEntry> part3 = entries.GetRange(size * 2, size);
                List<ZipArchiveEntry> part4 = entries.GetRange(size * 3, size);
                List<ZipArchiveEntry> part5 = entries.GetRange(size * 4, size);
                List<ZipArchiveEntry> part6 = entries.GetRange(size * 5, size);
                List<ZipArchiveEntry> part7 = entries.GetRange(size * 6, size);
                List<ZipArchiveEntry> part8 = entries.GetRange(size * 7, size);
                List<ZipArchiveEntry> part9 = entries.GetRange(size * 8, size);
                List<ZipArchiveEntry> part10 = entries.GetRange(size * 9, entries.Count - size * 9);

                // int size = archive.Count();
                foreach (ZipArchiveEntry entry in part2.OrderBy(x => x.FullName))
                {
                    Console.WriteLine("Поток 2" + " начал работу.");
                    Console.WriteLine("Загрузка файла: " + entry.FullName);
                    // Logger.WriteLine("Загрузка файла: " + entry.FullName);
                    string fileName = entry.Name;
                    string filePath = Path.Combine(tempDir, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    entry.ExtractToFile(filePath);
                    Console.WriteLine("Распаковка файла.");
                    //Logger.WriteLine("Распаковка файла.");
                    ProcessFile(filePath);

                    try
                    {
                        // Файл больше не нужен, освобождаем место
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Если не получилось удалить сразу, то 
                        // вторая попытка будет при выходе из программы
                    }
                    Console.WriteLine("Поток 2" + " завершил работу.");
                }
            }
            if (File.Exists(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip"))
            {
                File.Delete(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip");
                Console.WriteLine("Файл с обновлением больше не нужен, освобождаем место");
                //Logger.WriteLine("Файл с обновлением больше не нужен, освобождаем место");

            }

        }
        #endregion

        #region Запуск 3-ого потока
        /// <summary>
        /// Работа с 3-ым потоком.
        /// </summary>
        static void DoWork3()
        {
            // Здесь должна быть ваша работа
            
            string zipFile = @"C:\VS project\1\gar_xml.zip";
            string tempDir = Path.GetTempPath();
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                List<ZipArchiveEntry> entries = archive.Entries.ToList();

                int size = entries.Count / 10;
                List<ZipArchiveEntry> part1 = entries.GetRange(0, size);
                List<ZipArchiveEntry> part2 = entries.GetRange(size, size);
                List<ZipArchiveEntry> part3 = entries.GetRange(size * 2, size);
                List<ZipArchiveEntry> part4 = entries.GetRange(size * 3, size);
                List<ZipArchiveEntry> part5 = entries.GetRange(size * 4, size);
                List<ZipArchiveEntry> part6 = entries.GetRange(size * 5, size);
                List<ZipArchiveEntry> part7 = entries.GetRange(size * 6, size);
                List<ZipArchiveEntry> part8 = entries.GetRange(size * 7, size);
                List<ZipArchiveEntry> part9 = entries.GetRange(size * 8, size);
                List<ZipArchiveEntry> part10 = entries.GetRange(size * 9, entries.Count - size * 9);

                // int size = archive.Count();
                foreach (ZipArchiveEntry entry in part3.OrderBy(x => x.FullName))
                {
                    Console.WriteLine("Поток 3" + " начал работу.");
                    Console.WriteLine("Загрузка файла: " + entry.FullName);
                    //Logger.WriteLine("Загрузка файла: " + entry.FullName);
                    string fileName = entry.Name;
                    string filePath = Path.Combine(tempDir, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    entry.ExtractToFile(filePath);
                    Console.WriteLine("Распаковка файла.");
                    // Logger.WriteLine("Распаковка файла.");
                    ProcessFile(filePath);

                    try
                    {
                        // Файл больше не нужен, освобождаем место
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Если не получилось удалить сразу, то 
                        // вторая попытка будет при выходе из программы
                    }
                    Console.WriteLine("Поток 3" + " завершил работу.");
                }
            }
            if (File.Exists(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip"))
            {
                File.Delete(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip");
                Console.WriteLine("Файл с обновлением больше не нужен, освобождаем место");
                // Logger.WriteLine("Файл с обновлением больше не нужен, освобождаем место");

            }
            
        }
        #endregion

        #region Запуск 4-ого потока
        /// <summary>
        /// Работа с 4-ым потоком.
        /// </summary>
        static void DoWork4()
        {
            // Здесь должна быть ваша работа
            
            string zipFile = @"C:\VS project\1\gar_xml.zip";
            string tempDir = Path.GetTempPath();
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                List<ZipArchiveEntry> entries = archive.Entries.ToList();

                int size = entries.Count / 10;
                List<ZipArchiveEntry> part1 = entries.GetRange(0, size);
                List<ZipArchiveEntry> part2 = entries.GetRange(size, size);
                List<ZipArchiveEntry> part3 = entries.GetRange(size * 2, size);
                List<ZipArchiveEntry> part4 = entries.GetRange(size * 3, size);
                List<ZipArchiveEntry> part5 = entries.GetRange(size * 4, size);
                List<ZipArchiveEntry> part6 = entries.GetRange(size * 5, size);
                List<ZipArchiveEntry> part7 = entries.GetRange(size * 6, size);
                List<ZipArchiveEntry> part8 = entries.GetRange(size * 7, size);
                List<ZipArchiveEntry> part9 = entries.GetRange(size * 8, size);
                List<ZipArchiveEntry> part10 = entries.GetRange(size * 9, entries.Count - size * 9);

                // int size = archive.Count();
                foreach (ZipArchiveEntry entry in part4.OrderBy(x => x.FullName))
                {
                    Console.WriteLine("Поток 4" + " начал работу.");
                    Console.WriteLine("Загрузка файла: " + entry.FullName);
                    //Logger.WriteLine("Загрузка файла: " + entry.FullName);
                    string fileName = entry.Name;
                    string filePath = Path.Combine(tempDir, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    entry.ExtractToFile(filePath);
                    Console.WriteLine("Распаковка файла.");
                    //Logger.WriteLine("Распаковка файла.");
                    ProcessFile(filePath);

                    try
                    {
                        // Файл больше не нужен, освобождаем место
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Если не получилось удалить сразу, то 
                        // вторая попытка будет при выходе из программы
                    }
                    Console.WriteLine("Поток 4" + " завершил работу.");
                }
            }
            if (File.Exists(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip"))
            {
                File.Delete(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip");
                Console.WriteLine("Файл с обновлением больше не нужен, освобождаем место");
                //Logger.WriteLine("Файл с обновлением больше не нужен, освобождаем место");

            }
           
        }
        #endregion

        #region Запуск 5-ого потока
        /// <summary>
        /// Работа с 5-ым потоком.
        /// </summary>
        static void DoWork5()
        {
            // Здесь должна быть ваша работа
            
            string zipFile = @"C:\VS project\1\gar_xml.zip";
            string tempDir = Path.GetTempPath();
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                List<ZipArchiveEntry> entries = archive.Entries.ToList();

                int size = entries.Count / 10;
                List<ZipArchiveEntry> part1 = entries.GetRange(0, size);
                List<ZipArchiveEntry> part2 = entries.GetRange(size, size);
                List<ZipArchiveEntry> part3 = entries.GetRange(size * 2, size);
                List<ZipArchiveEntry> part4 = entries.GetRange(size * 3, size);
                List<ZipArchiveEntry> part5 = entries.GetRange(size * 4, size);
                List<ZipArchiveEntry> part6 = entries.GetRange(size * 5, size);
                List<ZipArchiveEntry> part7 = entries.GetRange(size * 6, size);
                List<ZipArchiveEntry> part8 = entries.GetRange(size * 7, size);
                List<ZipArchiveEntry> part9 = entries.GetRange(size * 8, size);
                List<ZipArchiveEntry> part10 = entries.GetRange(size * 9, entries.Count - size * 9);

                // int size = archive.Count();
                foreach (ZipArchiveEntry entry in part5.OrderBy(x => x.FullName))
                {
                    Console.WriteLine("Поток 5" + " начал работу.");
                    Console.WriteLine("Загрузка файла: " + entry.FullName);
                    //Logger.WriteLine("Загрузка файла: " + entry.FullName);
                    string fileName = entry.Name;
                    string filePath = Path.Combine(tempDir, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    entry.ExtractToFile(filePath);
                    Console.WriteLine("Распаковка файла.");
                    //Logger.WriteLine("Распаковка файла.");
                    ProcessFile(filePath);

                    try
                    {
                        // Файл больше не нужен, освобождаем место
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Если не получилось удалить сразу, то 
                        // вторая попытка будет при выходе из программы
                    }
                    Console.WriteLine("Поток 5" + " завершил работу.");
                }
            }
            if (File.Exists(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip"))
            {
                File.Delete(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip");
                Console.WriteLine("Файл с обновлением больше не нужен, освобождаем место");
                //Logger.WriteLine("Файл с обновлением больше не нужен, освобождаем место");

            }
            
        }
        #endregion

        #region Запуск 6-ого потока
        /// <summary>
        /// Работа с 6-ым потоком.
        /// </summary>
        static void DoWork6()
        {
            // Здесь должна быть ваша работа
           
            string zipFile = @"C:\VS project\1\gar_xml.zip";
            string tempDir = Path.GetTempPath();
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                List<ZipArchiveEntry> entries = archive.Entries.ToList();

                int size = entries.Count / 10;
                List<ZipArchiveEntry> part1 = entries.GetRange(0, size);
                List<ZipArchiveEntry> part2 = entries.GetRange(size, size);
                List<ZipArchiveEntry> part3 = entries.GetRange(size * 2, size);
                List<ZipArchiveEntry> part4 = entries.GetRange(size * 3, size);
                List<ZipArchiveEntry> part5 = entries.GetRange(size * 4, size);
                List<ZipArchiveEntry> part6 = entries.GetRange(size * 5, size);
                List<ZipArchiveEntry> part7 = entries.GetRange(size * 6, size);
                List<ZipArchiveEntry> part8 = entries.GetRange(size * 7, size);
                List<ZipArchiveEntry> part9 = entries.GetRange(size * 8, size);
                List<ZipArchiveEntry> part10 = entries.GetRange(size * 9, entries.Count - size * 9);

                // int size = archive.Count();
                foreach (ZipArchiveEntry entry in part6.OrderBy(x => x.FullName))
                {
                    Console.WriteLine("Поток 6" + " начал работу.");
                    Console.WriteLine("Загрузка файла: " + entry.FullName);
                    //Logger.WriteLine("Загрузка файла: " + entry.FullName);
                    string fileName = entry.Name;
                    string filePath = Path.Combine(tempDir, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    entry.ExtractToFile(filePath);
                    Console.WriteLine("Распаковка файла.");
                    //Logger.WriteLine("Распаковка файла.");
                    ProcessFile(filePath);

                    try
                    {
                        // Файл больше не нужен, освобождаем место
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Если не получилось удалить сразу, то 
                        // вторая попытка будет при выходе из программы
                    }
                    Console.WriteLine("Поток 6" + " завершил работу.");
                }
            }
            if (File.Exists(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip"))
            {
                File.Delete(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip");
                Console.WriteLine("Файл с обновлением больше не нужен, освобождаем место");
                //Logger.WriteLine("Файл с обновлением больше не нужен, освобождаем место");

            }
            
        }
        #endregion

        #region Запуск 7-ого потока
        /// <summary>
        /// Работа с 7-ым потоком.
        /// </summary>
        static void DoWork7()
        {
            // Здесь должна быть ваша работа
            
            string zipFile = @"C:\VS project\1\gar_xml.zip";
            string tempDir = Path.GetTempPath();
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                List<ZipArchiveEntry> entries = archive.Entries.ToList();

                int size = entries.Count / 10;
                List<ZipArchiveEntry> part1 = entries.GetRange(0, size);
                List<ZipArchiveEntry> part2 = entries.GetRange(size, size);
                List<ZipArchiveEntry> part3 = entries.GetRange(size * 2, size);
                List<ZipArchiveEntry> part4 = entries.GetRange(size * 3, size);
                List<ZipArchiveEntry> part5 = entries.GetRange(size * 4, size);
                List<ZipArchiveEntry> part6 = entries.GetRange(size * 5, size);
                List<ZipArchiveEntry> part7 = entries.GetRange(size * 6, size);
                List<ZipArchiveEntry> part8 = entries.GetRange(size * 7, size);
                List<ZipArchiveEntry> part9 = entries.GetRange(size * 8, size);
                List<ZipArchiveEntry> part10 = entries.GetRange(size * 9, entries.Count - size * 9);

                // int size = archive.Count();
                foreach (ZipArchiveEntry entry in part7.OrderBy(x => x.FullName))
                {
                    Console.WriteLine("Поток 7" + " начал работу.");
                    Console.WriteLine("Загрузка файла: " + entry.FullName);
                    //Logger.WriteLine("Загрузка файла: " + entry.FullName);
                    string fileName = entry.Name;
                    string filePath = Path.Combine(tempDir, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    entry.ExtractToFile(filePath);
                    Console.WriteLine("Распаковка файла.");
                    //Logger.WriteLine("Распаковка файла.");
                    ProcessFile(filePath);

                    try
                    {
                        // Файл больше не нужен, освобождаем место
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Если не получилось удалить сразу, то 
                        // вторая попытка будет при выходе из программы
                    }
                    Console.WriteLine("Поток 7" + " завершил работу.");
                }
            }
            if (File.Exists(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip"))
            {
                File.Delete(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip");
                Console.WriteLine("Файл с обновлением больше не нужен, освобождаем место");
                //Logger.WriteLine("Файл с обновлением больше не нужен, освобождаем место");

            }
            
        }
        #endregion

        #region Запуск 8-ого потока
        /// <summary>
        /// Работа с 8-ым потоком.
        /// </summary>
        static void DoWork8()
        {
            // Здесь должна быть ваша работа
            
            string zipFile = @"C:\VS project\1\gar_xml.zip";
            string tempDir = Path.GetTempPath();
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                List<ZipArchiveEntry> entries = archive.Entries.ToList();

                int size = entries.Count / 10;
                List<ZipArchiveEntry> part1 = entries.GetRange(0, size);
                List<ZipArchiveEntry> part2 = entries.GetRange(size, size);
                List<ZipArchiveEntry> part3 = entries.GetRange(size * 2, size);
                List<ZipArchiveEntry> part4 = entries.GetRange(size * 3, size);
                List<ZipArchiveEntry> part5 = entries.GetRange(size * 4, size);
                List<ZipArchiveEntry> part6 = entries.GetRange(size * 5, size);
                List<ZipArchiveEntry> part7 = entries.GetRange(size * 6, size);
                List<ZipArchiveEntry> part8 = entries.GetRange(size * 7, size);
                List<ZipArchiveEntry> part9 = entries.GetRange(size * 8, size);
                List<ZipArchiveEntry> part10 = entries.GetRange(size * 9, entries.Count - size * 9);

                // int size = archive.Count();
                foreach (ZipArchiveEntry entry in part8.OrderBy(x => x.FullName))
                {
                    Console.WriteLine("Поток 8" + " начал работу.");
                    Console.WriteLine("Загрузка файла: " + entry.FullName);
                    //Logger.WriteLine("Загрузка файла: " + entry.FullName);
                    string fileName = entry.Name;
                    string filePath = Path.Combine(tempDir, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    entry.ExtractToFile(filePath);
                    Console.WriteLine("Распаковка файла.");
                    //Logger.WriteLine("Распаковка файла.");
                    ProcessFile(filePath);

                    try
                    {
                        // Файл больше не нужен, освобождаем место
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Если не получилось удалить сразу, то 
                        // вторая попытка будет при выходе из программы
                    }
                    Console.WriteLine("Поток 8" + " завершил работу.");
                }
            }
            if (File.Exists(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip"))
            {
                File.Delete(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip");
                Console.WriteLine("Файл с обновлением больше не нужен, освобождаем место");
                //Logger.WriteLine("Файл с обновлением больше не нужен, освобождаем место");

            }
           
        }
        #endregion

        #region Запуск 9-ого потока
        /// <summary>
        /// Работа с 9-ым потоком.
        /// </summary>
        static void DoWork9()
        {
            // Здесь должна быть ваша работа
            
            string zipFile = @"C:\VS project\1\gar_xml.zip";
            string tempDir = Path.GetTempPath();
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                List<ZipArchiveEntry> entries = archive.Entries.ToList();

                int size = entries.Count / 10;
                List<ZipArchiveEntry> part1 = entries.GetRange(0, size);
                List<ZipArchiveEntry> part2 = entries.GetRange(size, size);
                List<ZipArchiveEntry> part3 = entries.GetRange(size * 2, size);
                List<ZipArchiveEntry> part4 = entries.GetRange(size * 3, size);
                List<ZipArchiveEntry> part5 = entries.GetRange(size * 4, size);
                List<ZipArchiveEntry> part6 = entries.GetRange(size * 5, size);
                List<ZipArchiveEntry> part7 = entries.GetRange(size * 6, size);
                List<ZipArchiveEntry> part8 = entries.GetRange(size * 7, size);
                List<ZipArchiveEntry> part9 = entries.GetRange(size * 8, size);
                List<ZipArchiveEntry> part10 = entries.GetRange(size * 9, entries.Count - size * 9);

                // int size = archive.Count();
                foreach (ZipArchiveEntry entry in part9.OrderBy(x => x.FullName))
                {
                    Console.WriteLine("Поток 9" + " начал работу.");
                    Console.WriteLine("Загрузка файла: " + entry.FullName);
                    //Logger.WriteLine("Загрузка файла: " + entry.FullName);
                    string fileName = entry.Name;
                    string filePath = Path.Combine(tempDir, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    entry.ExtractToFile(filePath);
                    Console.WriteLine("Распаковка файла.");
                    //Logger.WriteLine("Распаковка файла.");
                    ProcessFile(filePath);

                    try
                    {
                        // Файл больше не нужен, освобождаем место
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Если не получилось удалить сразу, то 
                        // вторая попытка будет при выходе из программы
                    }
                    Console.WriteLine("Поток 9" + " завершил работу.");
                }
            }
            if (File.Exists(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip"))
            {
                File.Delete(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip");
                Console.WriteLine("Файл с обновлением больше не нужен, освобождаем место");
                //Logger.WriteLine("Файл с обновлением больше не нужен, освобождаем место");

            }
            
        }
        #endregion

        #region Запуск 10-ого потока
        /// <summary>
        /// Работа с 10-ым потоком.
        /// </summary>
        static void DoWork10()
        {
            // Здесь должна быть ваша работа
            
            string zipFile = @"C:\VS project\1\gar_xml.zip";
            string tempDir = Path.GetTempPath();
            using (ZipArchive archive = ZipFile.OpenRead(zipFile))
            {
                List<ZipArchiveEntry> entries = archive.Entries.ToList();

                int size = entries.Count / 10;
                List<ZipArchiveEntry> part1 = entries.GetRange(0, size);
                List<ZipArchiveEntry> part2 = entries.GetRange(size, size);
                List<ZipArchiveEntry> part3 = entries.GetRange(size * 2, size);
                List<ZipArchiveEntry> part4 = entries.GetRange(size * 3, size);
                List<ZipArchiveEntry> part5 = entries.GetRange(size * 4, size);
                List<ZipArchiveEntry> part6 = entries.GetRange(size * 5, size);
                List<ZipArchiveEntry> part7 = entries.GetRange(size * 6, size);
                List<ZipArchiveEntry> part8 = entries.GetRange(size * 7, size);
                List<ZipArchiveEntry> part9 = entries.GetRange(size * 8, size);
                List<ZipArchiveEntry> part10 = entries.GetRange(size * 9, entries.Count - size * 9);

                // int size = archive.Count();
                foreach (ZipArchiveEntry entry in part10.OrderBy(x => x.FullName))
                {
                    Console.WriteLine("Поток 10" + " начал работу.");
                    Console.WriteLine("Загрузка файла: " + entry.FullName);
                    //Logger.WriteLine("Загрузка файла: " + entry.FullName);
                    string fileName = entry.Name;
                    string filePath = Path.Combine(tempDir, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    entry.ExtractToFile(filePath);
                    Console.WriteLine("Распаковка файла.");
                    //Logger.WriteLine("Распаковка файла.");
                    ProcessFile(filePath);

                    try
                    {
                        // Файл больше не нужен, освобождаем место
                        File.Delete(filePath);
                    }
                    catch
                    {
                        // Если не получилось удалить сразу, то 
                        // вторая попытка будет при выходе из программы
                    }
                    Console.WriteLine("Поток 10" + " завершил работу.");
                }
            }
            if (File.Exists(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip"))
            {
                File.Delete(@"C:\Users\j.shepelev\AppData\Local\Temp\gar_xml.zip");
                Console.WriteLine("Файл с обновлением больше не нужен, освобождаем место");
                //Logger.WriteLine("Файл с обновлением больше не нужен, освобождаем место");

            }
            
        }
        #endregion

        private static void ProcessFile(string filePath)
        {
            string dirName = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            FiasNames fiasName = FiasNameHelper.GetFiasName(fileName);
            switch (fiasName)
            {
                case FiasNames.AS_ADDR_OBJ_20:
                    Load_AS_ADDR_OBJ_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_ADDR_OBJ файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_ADDR_OBJ файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_ADDR_OBJ_DIVISION_20:
                    Load_AS_ADDR_OBJ_DIVISION_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_ADDR_OBJ_DIVISION файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_ADDR_OBJ_DIVISION файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_ADDR_OBJ_PARAMS_20:
                    Load_AS_ADDR_OBJ_PARAMS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_ADDR_OBJ_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_ADDR_OBJ_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_ADM_HIERARCHY_20:
                    Load_AS_ADM_HIERARCHY_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_ADM_HIERARCHY файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_ADM_HIERARCHY файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_APARTMENTS_20:
                    Load_AS_APARTMENTS_20_DataFromFile(
                     dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_APARTMENTS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_APARTMENTS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_APARTMENTS_PARAMS_20:
                    Load_AS_APARTMENTS_PARAMS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_APARTMENTS_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_APARTMENTS_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_CARPLACES_20:
                    Load_AS_CARPLACES_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_CARPLACES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_CARPLACES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_CARPLACES_PARAMS_20:
                    Load_AS_CARPLACES_PARAMS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_CARPLACES_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_CARPLACES_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_CHANGE_HISTORY_20:
                    Load_AS_CHANGE_HISTORY_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_CHANGE_HISTORY файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_CHANGE_HISTORY файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_HOUSES_20:
                    Load_AS_HOUSES_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_HOUSES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_HOUSES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_HOUSES_PARAMS_20:
                    Load_AS_HOUSES_PARAMS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_HOUSES_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_HOUSES_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_MUN_HIERARCHY_20:
                    Load_AS_MUN_HIERARCHY_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_MUN_HIERARCHY файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_MUN_HIERARCHY файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_NORMATIVE_DOCS_20:
                    Load_AS_NORMATIVE_DOCS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_NORMATIVE_DOCS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_NORMATIVE_DOCS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_REESTR_OBJECTS_20:
                    Load_AS_REESTR_OBJECTS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_REESTR_OBJECTS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_REESTR_OBJECTS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_ROOMS_20:
                    Load_AS_ROOMS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_ROOMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_ROOMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_ROOMS_PARAMS_20:
                    Load_AS_ROOMS_PARAMS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_ROOMS_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_ROOMS_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_STEADS_20:
                    Load_AS_STEADS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_STEADS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_STEADS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_STEADS_PARAMS_20:
                    Load_AS_STEADS_PARAMS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_STEADS_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_STEADS_PARAMS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_ADDHOUSE_TYPES_20:
                    Load_AS_ADDHOUSE_TYPES_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_ADDHOUSE_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_ADDHOUSE_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_ADDR_OBJ_TYPES_20:
                    Load_AS_ADDR_OBJ_TYPES_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_ADDR_OBJ_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_ADDR_OBJ_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_APARTMENT_TYPES_20:
                    Load_AS_APARTMENT_TYPES_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_APARTMENT_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_APARTMENT_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_HOUSE_TYPES_20:
                    Load_AS_HOUSE_TYPES_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_HOUSE_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_HOUSE_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_NORMATIVE_DOCS_KINDS_20:
                    Load_AS_NORMATIVE_DOCS_KINDS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_NORMATIVE_DOCS_KINDS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_NORMATIVE_DOCS_KINDS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_NORMATIVE_DOCS_TYPES_20:
                    Load_AS_NORMATIVE_DOCS_TYPES_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_NORMATIVE_DOCS_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_NORMATIVE_DOCS_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_OBJECT_LEVELS_20:
                    Load_AS_OBJECT_LEVELS_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_OBJECT_LEVELS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_OBJECT_LEVELS файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_OPERATION_TYPES_20:
                    Load_AS_OPERATION_TYPES_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_OPERATION_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_OPERATION_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_PARAM_TYPES_20:
                    Load_AS_PARAM_TYPES_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_PARAM_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_PARAM_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;

                case FiasNames.AS_ROOM_TYPES_20:
                    Load_AS_ROOM_TYPES_20_DataFromFile(
                    dirName, fileName, 10000, "Обновление сведений по отдельным зданиям " + fileName, true);
                    Console.WriteLine("Загрузка AS_ROOM_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загрузка AS_ROOM_TYPES файла завершена." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    break;
            }


        }
        private static void Load_AS_ADDR_OBJ_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {

            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_ADDR_OBJ AS_ADDR_OBJ = new AS_ADDR_OBJ(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_ADDR_OBJ.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_ADDR_OBJ." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_ADDR_OBJ." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_ADDR_OBJ_DIVISION_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_ADDR_OBJ_DIVISION AS_ADDR_OBJ_DIVISION = new AS_ADDR_OBJ_DIVISION(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_ADDR_OBJ_DIVISION.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_ADDR_OBJ_DIVISION." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_ADDR_OBJ_DIVISION." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_ADDR_OBJ_PARAMS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_ADDR_OBJ_PARAMS AS_ADDR_OBJ_PARAMS = new AS_ADDR_OBJ_PARAMS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_ADDR_OBJ_PARAMS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_ADDR_OBJ_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_ADDR_OBJ_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_ADM_HIERARCHY_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_ADM_HIERARCHY AS_ADM_HIERARCHY = new AS_ADM_HIERARCHY(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_ADM_HIERARCHY.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_ADM_HIERARCHY." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_ADM_HIERARCHY." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_APARTMENTS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_APARTMENTS AS_APARTMENTS = new AS_APARTMENTS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_APARTMENTS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_APARTMENTS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_APARTMENTS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_APARTMENTS_PARAMS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_APARTMENTS_PARAMS AS_APARTMENTS_PARAMS = new AS_APARTMENTS_PARAMS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_APARTMENTS_PARAMS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_APARTMENTS_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_APARTMENTS_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_CARPLACES_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_CARPLACES AS_CARPLACES = new AS_CARPLACES(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_CARPLACES.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_CARPLACES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_CARPLACES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_CARPLACES_PARAMS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_CARPLACES_PARAMS AS_CARPLACES_PARAMS = new AS_CARPLACES_PARAMS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_CARPLACES_PARAMS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_CARPLACES_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_CARPLACES_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_CHANGE_HISTORY_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_CHANGE_HISTORY AS_CHANGE_HISTORY = new AS_CHANGE_HISTORY(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_CHANGE_HISTORY.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_CHANGE_HISTORY." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_CHANGE_HISTORY." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_HOUSES_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_HOUSES AS_HOUSES = new AS_HOUSES(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_HOUSES.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_HOUSES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_HOUSES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_HOUSES_PARAMS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {

            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_HOUSES_PARAMS AS_HOUSES_PARAMS = new AS_HOUSES_PARAMS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_HOUSES_PARAMS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_HOUSES_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_HOUSES_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                }
            }
        }

        private static void Load_AS_MUN_HIERARCHY_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_MUN_HIERARCHY AS_MUN_HIERARCHY = new AS_MUN_HIERARCHY(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_MUN_HIERARCHY.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_MUN_HIERARCHY." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_MUN_HIERARCHY." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_NORMATIVE_DOCS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_NORMATIVE_DOCS AS_NORMATIVE_DOCS = new AS_NORMATIVE_DOCS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_NORMATIVE_DOCS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_NORMATIVE_DOCS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_NORMATIVE_DOCS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_REESTR_OBJECTS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_REESTR_OBJECTS AS_REESTR_OBJECTS = new AS_REESTR_OBJECTS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_REESTR_OBJECTS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_REESTR_OBJECTS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_REESTR_OBJECTS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_ROOMS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_ROOMS AS_ROOMS = new AS_ROOMS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_ROOMS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_ROOMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_ROOMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_ROOMS_PARAMS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_ROOMS_PARAMS AS_ROOMS_PARAMS = new AS_ROOMS_PARAMS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_ROOMS_PARAMS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_ROOMS_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_ROOMS_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_STEADS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_STEADS AS_STEADS = new AS_STEADS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_STEADS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_STEADS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_STEADS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_STEADS_PARAMS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_STEADS_PARAMS AS_STEADS_PARAMS = new AS_STEADS_PARAMS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_STEADS_PARAMS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_STEADS_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_STEADS_PARAMS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_ADDHOUSE_TYPES_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_ADDHOUSE_TYPES AS_ADDHOUSE_TYPES = new AS_ADDHOUSE_TYPES(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_ADDHOUSE_TYPES.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_ADDHOUSE_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_ADDHOUSE_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_ADDR_OBJ_TYPES_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_ADDR_OBJ_TYPES AS_ADDR_OBJ_TYPES = new AS_ADDR_OBJ_TYPES(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_ADDR_OBJ_TYPES.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_ADDR_OBJ_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_ADDR_OBJ_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_APARTMENT_TYPES_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_APARTMENT_TYPES AS_APARTMENT_TYPES = new AS_APARTMENT_TYPES(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_APARTMENT_TYPES.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_APARTMENT_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_APARTMENT_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_HOUSE_TYPES_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_HOUSE_TYPES AS_HOUSE_TYPES = new AS_HOUSE_TYPES(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_HOUSE_TYPES.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_HOUSE_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_HOUSE_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_NORMATIVE_DOCS_KINDS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_NORMATIVE_DOCS_KINDS AS_NORMATIVE_DOCS_KINDS = new AS_NORMATIVE_DOCS_KINDS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_NORMATIVE_DOCS_KINDS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_NORMATIVE_DOCS_KINDS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_NORMATIVE_DOCS_KINDS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_NORMATIVE_DOCS_TYPES_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_NORMATIVE_DOCS_TYPES AS_NORMATIVE_DOCS_TYPES = new AS_NORMATIVE_DOCS_TYPES(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_NORMATIVE_DOCS_TYPES.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_NORMATIVE_DOCS_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_NORMATIVE_DOCS_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_OBJECT_LEVELS_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_OBJECT_LEVELS AS_OBJECT_LEVELS = new AS_OBJECT_LEVELS(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_OBJECT_LEVELS.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_OBJECT_LEVELS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_OBJECT_LEVELS." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_OPERATION_TYPES_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_OPERATION_TYPES AS_OPERATION_TYPES = new AS_OPERATION_TYPES(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_OPERATION_TYPES.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_OPERATION_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_OPERATION_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

        private static void Load_AS_PARAM_TYPES_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_PARAM_TYPES AS_PARAM_TYPES = new AS_PARAM_TYPES(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_PARAM_TYPES.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_PARAM_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_PARAM_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }


        private static void Load_AS_ROOM_TYPES_20_DataFromFile(string filesDir, string fileName, int progressStep, string progressText, bool useMerge)
        {
            string filePath = Path.Combine(filesDir, fileName);
            if (File.Exists(filePath))
            {
                using (SqlConnection sqlConnection = FiasRepository.GetConnection())
                using (AS_ROOM_TYPES AS_ROOM_TYPES = new AS_ROOM_TYPES(sqlConnection, filesDir, fileName, useMerge))
                {
                    AS_ROOM_TYPES.UpsertToServer();
                    Console.WriteLine("Загружен в БД AS_ROOM_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));
                    Logger.WriteLine("Загружен в БД AS_ROOM_TYPES." + " Время записи: " + DateTime.Now.ToString("HH:mm:ss"));

                }
            }
        }

    }
}


