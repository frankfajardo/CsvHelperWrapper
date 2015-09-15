using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CsvImportUtility
{
    /// <summary>
    /// A helper class for importing csv data into a datastore
    /// </summary>
    public sealed class ImportHandler
    {
        /// <summary>
        /// Imports a csv file onto a datastore
        /// </summary>
        /// <typeparam name="TContext">The datastore context</typeparam>
        /// <typeparam name="TEntity">The data entity to update</typeparam>
        /// <param name="importCsvPath">The import file's full path</param>
        /// <param name="action">Determines how to update the dataset for this import</param>
        /// <param name="encoding">The encoding of the import file</param>
        /// <param name="hasHeaderRow">Determines if the import file has a header row</param>
        /// <param name="mapfac">The factory class to use to generate a mapper between the import data and the data entity</param>
        /// <param name="progress">The class to container progress details</param>
        /// <param name="cancelToken">The token for cancelling the import task</param>
        /// <exception cref="InvalidOperationException">When unable to create instance of TContext, of when TContext does not have a DbSet&gt;TEntity&lt;</exception>
        /// <exception cref="ArgumentException">When import file path is null, empty or contains invalid characters, or is too long.</exception>
        public async static Task<ImportResult> ImportAsync<TContext, TEntity>(
                string importCsvPath,
                ImportAction action = ImportAction.Append,
                Encoding encoding = null,
                bool hasHeaderRow = false,
                ICsvClassMapFactory mapfac = null,
                IProgress<string> progress = null,
                CancellationToken cancelToken = default(CancellationToken))
            where TContext : DbContext
            where TEntity : class
        {
            // Before starting, check that this async process has not been cancelled. If it has, end now.
            cancelToken.ThrowIfCancellationRequested();

            var result = new ImportResult();
            result.StartTime = DateTime.Now;
            result.DbContextType = typeof(TContext);
            result.EntityType = typeof(TEntity);
            try
            {
                result.ImportFile = new FileInfo(importCsvPath);
            }
            catch (Exception e)
            {
                if (e is ArgumentNullException || e is ArgumentException || e is PathTooLongException || e is NotSupportedException)
                {
                    throw new ArgumentException("Import file path is invalid.");
                }
                throw e;
            }

            // Define and configure our CsvReader
            var csvConfig = new CsvConfiguration();
            if (mapfac != null && mapfac.HasMap<TEntity>())
            {
                var mapper = mapfac.GetMap<TEntity>();
                csvConfig.RegisterClassMap(mapper);
            }
            csvConfig.HasHeaderRecord = hasHeaderRow;
            csvConfig.Encoding = (encoding != null) ? encoding : Encoding.UTF8;
            csvConfig.SkipEmptyRecords = true;

            // Create instance of dbcontext. This will throw an InvalidOperationException if it fails for any reason.
            TContext dbContext = ImportHandler.CreateNewDbContext<TContext>();
            // Clear relevant database table if required
            if (action == ImportAction.Replace)
            {
                if (progress != null)
                {
                    var report = string.Format("Clearing table for entity {0} in {1}...",
                        result.EntityType.Name, result.DbContextType.Name);
                    progress.Report(report);
                }
                List<string> errors = await dbContext.TryClearDbSetAsync<TEntity>();
                if (errors.Count > 0)
                {
                    result.ErrorMessages.AddRange(errors);
                    result.EndTime = DateTime.Now;
                    return result;
                }
                if (progress != null)
                {
                    var report = string.Format("Table for entity {0} has been cleared", result.EntityType.Name);
                    progress.Report(report);
                }
            }

            // Get dbSet for TEntity. This will throw an InvalidOperationException if TEntity is not in dbContext.
            DbSet<TEntity> dbSet = await dbContext.GetDbSetAsync<TEntity>();

            int rowsInBatch = 0;
            int commitRowCount = 50000; // Number of records to parse before each SaveChanges
            int rowsInError = 0;
            int rowsToCommit = 0;
            if (progress != null)
            {
                progress.Report("Starting to read file...");
            }

            using (var dbTransaction = dbContext.Database.BeginTransaction())
            {
                #region Read CSV

                using (var reader = new StreamReader(importCsvPath))
                using (var csvReader = new CsvReader(reader, csvConfig))
                {
                    // Start reading row by row
                    while (csvReader.Read())
                    {
                        result.RowsRead++;
                        try
                        {
                            var dbRow = csvReader.GetRecord<TEntity>();
                            dbSet.Add(dbRow);
                            rowsInBatch++;
                        }
                        catch (Exception e)
                        {
                            // Read and parse error from Exception.Data["CsvHelper"] so it is a single string.
                            // For more details, see: http://joshclose.github.io/CsvHelper/#misc-faq
                            var edata = e.Data["CsvHelper"].ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None);
                            var colIndexA = edata[2].Replace("Field Index: '", "").Replace("' (0 based)", "");
                            int colIndex;
                            int.TryParse(colIndexA, out colIndex);
                            var colValue = edata[3].Replace("Field Value: ", "");
                            var msg = string.Format("Row {0}, column {1} has invalid value {2}. {3}", result.RowsRead, (colIndex + 1), colValue, e.Message);
                            // Add to error messages and error counter.
                            result.ErrorMessages.Add(msg);
                            rowsInError++;
                        }

                        // Check that this task has not been cancelled before proceesing further.
                        if (cancelToken.IsCancellationRequested)
                        {
                            dbTransaction.Rollback();
                            cancelToken.ThrowIfCancellationRequested();
                        }

                        if (rowsInBatch >= commitRowCount)
                        {
                            List<string> errors = await dbContext.TrySaveChangesAsync();
                            if (errors.Count > 0)
                            {
                                result.ErrorMessages.AddRange(errors);
                                dbTransaction.Rollback();
                                result.EndTime = DateTime.Now;
                                return result;
                            }
                            rowsToCommit += rowsInBatch;
                            rowsInBatch = 0;
                        }

                        // Update progress.
                        if (progress != null)
                        {
                            string report = string.Format("{0} {1} read, {2} {3} error",
                                result.RowsRead, (result.RowsRead != 1 ? "rows" : "row"), rowsInError, (rowsInError != 1 ? "have" : "has"));
                            progress.Report(report);
                        }
                    }
                }

                #endregion Read CSV

                if (rowsInBatch > 0)
                {
                    List<string> errors = await dbContext.TrySaveChangesAsync();
                    if (errors.Count > 0)
                    {
                        result.ErrorMessages.AddRange(errors);
                        dbTransaction.Rollback();
                        result.EndTime = DateTime.Now;
                        return result;
                    }
                    rowsToCommit += rowsInBatch;
                    rowsInBatch = 0;
                }
                if (rowsToCommit > 0)
                {
                    if (progress != null)
                    {
                        progress.Report("Commiting all changes to database...");
                    }
                    dbTransaction.Commit();
                    result.RowsImported = rowsToCommit;
                    if (progress != null)
                    {
                        progress.Report(string.Format("{0} rows written to database.", result.RowsImported));
                    }
                }
            }

            result.EndTime = DateTime.Now;
            return result;
        }

        private static TContext CreateNewDbContext<TContext>() where TContext : DbContext
        {
            try
            {
                var dbContext = (TContext)Activator.CreateInstance(typeof(TContext));
                // Disable auto-detecting changes for fast processing.
                dbContext.Configuration.AutoDetectChangesEnabled = false;
                return dbContext;
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Unable to create instance of TContext.");
            }
        }
    }
}
