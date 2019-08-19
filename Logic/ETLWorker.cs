using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Compression;
using Newtonsoft.Json;
using CsvHelper;
using SawyerSight.Web.Models.ETL;
using System.Data;
using System.Diagnostics;
using SawyerSight.Web.DAL;
using SawyerSight.Web.Utils;
using Microsoft.VisualBasic.FileIO;

namespace SawyerSight.Web.Logic
{
    public class ETLWorker
    {
        string workingDirectory = "";
        public int ReportDataSetID = 0;
        List<string> errors = new List<string>();
        bool errorOccured = false;
        List<string> failedTables = new List<string>();
        private readonly IStageDataService _etlService;
        int itemsCount = 34;
        int itemsProgress = 1;

        public ETLWorker(int itemsSoFar)
        {
            _etlService = (IStageDataService)UnityConfig.DefaultContainer.Resolve(typeof(IStageDataService), null, null);
            itemsProgress = itemsSoFar;
        }

        public ETLWorker(IStageDataService etlService)
        {
            _etlService = etlService;
        }

        public bool LoadClientData(string ClientPath, int reportDataSetID, byte[] zipArchive = null)
        {

            errorOccured = false;
            ReportDataSetID = reportDataSetID;
            
            try
            {
                if(zipArchive!=null)
                {
                    
                    if (Directory.Exists(ClientPath))
                    {
                        Directory.Delete(ClientPath, true);
                    }
                    Directory.CreateDirectory(ClientPath);

                    File.WriteAllBytes(Path.Combine(ClientPath, "Archive.zip"), zipArchive);
                    ZipFile.ExtractToDirectory(Path.Combine(ClientPath, "Archive.zip"), Path.Combine(ClientPath, "Extracted"));
                }
           

                SignalRProcessor.SendImportUpdate("Extraction Completed. Loading data...", itemsProgress++, itemsCount);

                workingDirectory = Path.Combine(ClientPath, "Extracted");
                List<string> tablesForExtraction = File.ReadAllLines(Path.Combine(workingDirectory, "tables.txt")).ToList();
                ExecutionManifest executionLog = JsonConvert.DeserializeObject<ExecutionManifest>(File.ReadAllText(Path.Combine(workingDirectory, "executionLog.log")));
                if (executionLog.Status == "SUCCESS")
                {
                    foreach (var table in tablesForExtraction)
                    {
                        var tableLog = executionLog.TablesInfo.FirstOrDefault(x => x.TableName == table);
                        if (tableLog.Status == "SUCCESS" && tableLog.TableExported == 1)
                        {
                            SignalRProcessor.SendImportUpdate($"Reading table {table}", itemsProgress++, itemsCount);
                            var dt = CSVToDataTable(table, executionLog.ExportEngine);
                            if (dt != null)
                            {
                                SignalRProcessor.SendImportUpdate($"Importing table {table}", itemsProgress++, itemsCount);
                                ImportDataTable(table, dt, ReportDataSetID);
                            }
                        }
                    }
                    Directory.Delete(ClientPath, true);
                }
            }
            catch (Exception ex)
            {
                Directory.Delete(ClientPath, true);
                SignalRProcessor.SendImportUpdate($"Archive Failed Processing. Error:" + ex.Message, itemsProgress++, itemsCount);
                errorOccured = true;
            }
            return !errorOccured;
        }



        private DataTable CSVToDataTable(string table, string exportEngine)
        {
            try
            {
                return CsvFileToDatatable(table, exportEngine);

            }
            catch (Exception ex)
            {
                failedTables.Add(table);
                errorOccured = true;
                errors.Add(ex.StackTrace);
                SignalRProcessor.SendImportUpdate($"Reading table {table} from CSV file failed. Error: "+ex.Message+Environment.NewLine+"Stack Trace: "+ex.StackTrace, itemsProgress++, itemsCount);
            }
            return null;
        }

        private DataTable CsvFileToDatatable(string table, string exportEngine)
        {
            DataTable dt = new DataTable();
            try
            { 
            var inputFileName = Path.Combine(workingDirectory, table + ".csv");
            var outputFileName = Path.Combine(workingDirectory, table + "Replaced.csv");
               



                var originalFile = File.ReadLines(inputFileName).ToList();
            var cleanedFile = new List<string>();

            if (exportEngine.Equals("BCP", StringComparison.InvariantCultureIgnoreCase))
            {
                File.WriteAllLines(outputFileName, originalFile.Select(x => x.Replace("\n", "").Replace("\r", "").Replace("\r\n", "").Replace("<r>", "\r\n")).ToArray());
            }
            else
            {
                outputFileName = inputFileName;
            }

            

            using (TextReader fileReader = File.OpenText(outputFileName))
            {
                
                var csv = new CsvReader(fileReader);
                csv.Configuration.HasHeaderRecord = true;
                csv.Configuration.Delimiter = exportEngine.Equals("BCP", StringComparison.InvariantCultureIgnoreCase) ? "<c>" : ",";
                
                var bad = new List<string>();
               
                csv.Configuration.BadDataFound = context =>
                {
                    bad.Add(context.RawRecord);
                };
                csv.Read();
                csv.ReadHeader();
                foreach (var header in csv.Context.HeaderRecord)
                {
                    dt.Columns.Add(header.Trim());
                }
                dt.Columns.Add("ReportDataSetID");
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (csv.Read())
                {
                    var row = dt.NewRow();
                    var columnIndex = 0;
                    foreach (var column in csv.Context.Record)
                    {  
                        row[columnIndex] = column;
                        columnIndex++;
                    }
                    row[columnIndex] = ReportDataSetID;
                    dt.Rows.Add(row);
                }
                
                long first = sw.ElapsedMilliseconds;
                sw.Stop();
                
            }
            }
            catch(Exception ex)
            {

            }
            return dt;
        }


        private void ImportDataTable(string tableName, DataTable data, int reportDataSetID)
        {
            try
            {
                _etlService.InsertTable(tableName, data, reportDataSetID);
                SignalRProcessor.SendImportUpdate($"Table {tableName} processed.", itemsProgress++, itemsCount);
            }
            catch (Exception ex)
            {
                SignalRProcessor.SendImportUpdate($"Table {tableName} processing failed. Importing process might not complete successfully. Error:"+ex.Message+Environment.NewLine+"StackTrace:"+ex.StackTrace, itemsProgress++, itemsCount);
                failedTables.Add(tableName);
                errorOccured = true;
                errors.Add(ex.StackTrace);
            }
        }
    }
}