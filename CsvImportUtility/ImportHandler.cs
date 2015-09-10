using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using CsvHelper;
using CsvHelper.Configuration;


namespace CsvImportUtility
{
    public enum UpdateOption
    {
        Append,
        ClearBeforeImport
    }

    public class ImportHandler
    {

        public async static Task<ImportResult> ImportAsync<TContext, TEntity>(
                TextReader textReader,
                UpdateOption option = UpdateOption.Append,
                CsvClassMap<TEntity> mapper = null,
                Encoding csvEncoding = null,
                bool csvHasHeader = false,
                IProgress<string> progress = null,
                CancellationToken cancelToken = default(CancellationToken))
            where TContext : DbContext
            where TEntity : class
        {
            if (cancelToken.IsCancellationRequested)
            {
                cancelToken.ThrowIfCancellationRequested();
            }

            if (textReader == null) throw new ArgumentNullException("textReader");
            TContext dbContext = ImportHandler.CreateNewDbContext<TContext>();

            DbSet<TEntity> dbSet;
            if (!ImportHandler.TryGetDbSet<TEntity>(dbContext, out dbSet))
            {
                throw new InvalidOperationException(string.Format("{0} is not a valid entity in {1}.", typeof(TEntity), dbContext.GetType()));
            }

            if (option == UpdateOption.ClearBeforeImport && await dbSet.CountAsync() > 0)
            {
                dbSet.RemoveRange(dbSet);
            }

            var csvReader = new CsvReader(textReader);
            if (mapper != null)
            {
                csvReader.Configuration.RegisterClassMap(mapper);
            }
            csvReader.Configuration.HasHeaderRecord = csvHasHeader;
            csvReader.Configuration.Encoding = (csvEncoding != null) ? csvEncoding : Encoding.UTF8;
            csvReader.Configuration.SkipEmptyRecords = true;

            var result = new ImportResult();
            int rowsInError = 0, rowsParsed = 0;
            
            while (csvReader.Read())
            {
                result.RowsRead++;
                try
                {
                    var dbRow = csvReader.GetRecord<TEntity>();
                    dbSet.Add(dbRow);
                    rowsParsed++;
                }
                catch (Exception e)
                {
                    var edata = e.Data["CsvHelper"].ToString().Split(new string[] { "\r\n" }, StringSplitOptions.None);
                    var colIndexA = edata[2].Replace("Field Index: '", "").Replace("' (0 based)", "");
                    int colIndex;
                    int.TryParse(colIndexA, out colIndex);
                    var colValue = edata[3].Replace("Field Value: ", "");
                    var msg = string.Format("Row {0}, column {1} has invalid value {2}. {3}", result.RowsRead, (colIndex + 1), colValue, e.Message);
                    result.ErrorMessages.Add(msg);
                    rowsInError++;
                }
                // Generate progress report.
                if (progress != null)
                {
                    string report = string.Format("{0} {1} read, {2} {3} error",
                        result.RowsRead, (result.RowsRead != 1 ? "rows" : "row"),
                        rowsInError, (rowsInError != 1 ? "have" : "has"));
                    progress.Report(report);
                }
                // See if our task is cancelled.
                if (cancelToken.IsCancellationRequested)
                {
                    cancelToken.ThrowIfCancellationRequested();
                }
            }

            // Persist imported data
            int changesSaved = 0;
            try
            {
                changesSaved = await dbContext.SaveChangesAsync();
                result.RowsImported = rowsParsed;
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        var msg = string.Format("Entity validation error for {0} \"{1}\", property \"{2}\". {3}", 
                            eve.Entry.State, eve.Entry.Entity.GetType().Name, ve.PropertyName, ve.ErrorMessage);
                        result.ErrorMessages.Add(msg);
                    }
                }
            }
            catch (DbUpdateException e)
            {
                if (e.InnerException != null && e.InnerException.InnerException != null)
                {
                    var msg = e.InnerException.InnerException.Message;
                    result.ErrorMessages.Add(msg);
                }
            }
            catch (Exception e)
            {
                var msg = (e.InnerException != null) ? e.InnerException.Message : e.Message;
                result.ErrorMessages.Add(e.Message);
            }

            result.RowsImported = (changesSaved == 0) ? 0 : rowsParsed;
            if (progress != null)
            {
                progress.Report(string.Format("{0} rows written to database.", result.RowsImported));
            }
            return result;
        }
        
        private static TContext CreateNewDbContext<TContext>() where TContext : DbContext
        {
            var dbContext = (TContext)Activator.CreateInstance(typeof(TContext));
            dbContext.Configuration.AutoDetectChangesEnabled = false;
            return dbContext;
        }

        private static bool TryGetDbSet<TEntity>(DbContext context, out DbSet<TEntity> dbset) where TEntity : class
        {
            dbset = null;
            if (context == null) return false;

            dbset = context.Set<TEntity>();
            try
            {
                var ent = dbset.Count(); // This should return InvalidOperationException if TEntity is not in the dbcontext
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

    }


    public class ImportResult
    {
        public ImportResult()
        {
            this.ErrorMessages = new List<string>();
        }

        public int RowsRead { get; set; }
        public int RowsImported { get; set; }
        public IList<string> ErrorMessages { get; set; }
    }


}
