using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Threading.Tasks;

namespace CsvImportUtility
{
    public static class DbContextExtension
    {
        /// <summary>
        /// Tries to asyncronously save changes
        /// </summary>
        /// <returns>A list of error messages if the method fails.</returns>
        public static async Task<List<string>> TrySaveChangesAsync(this DbContext dbContext)
        {
            var errors = new List<string>();
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        var msg = string.Format("Entity validation error for {0} \"{1}\", property \"{2}\". {3}",
                            eve.Entry.State, eve.Entry.Entity.GetType().Name, ve.PropertyName, ve.ErrorMessage);
                        errors.Add(msg);
                    }
                }
            }
            catch (DbUpdateException e)
            {
                if (e.InnerException != null && e.InnerException.InnerException != null)
                {
                    var msg = e.InnerException.InnerException.Message;
                    errors.Add(msg);
                }
            }
            catch (Exception e)
            {
                var msg = (e.InnerException != null) ? e.InnerException.Message : e.Message;
                errors.Add(e.Message);
            }
            return errors;
        }

        /// <summary>
        /// Tries to asynchronously clear existing data a dataset
        /// </summary>
        /// <typeparam name="TEntity">The relevant data entity</typeparam>
        /// <returns>A list of error messages if the method fails.</returns>
        public static async Task<List<string>> TryClearDbSetAsync<TEntity>(this DbContext context) where TEntity : class
        {
            List<string> errors = new List<string>();
            if (context == null)
            {
                errors.Add("DBContext cannot be null");
            }
            var dbset = await context.GetDbSetAsync<TEntity>();
            if (await dbset.CountAsync() > 0)
            {
                dbset.RemoveRange(dbset);
                errors = await context.TrySaveChangesAsync();
            }
            return errors;
        }

        /// <summary>
        /// Gets the dbset for the given entity
        /// </summary>
        /// <exception cref="InvalidOperationException">When the dbcontext does not contain TEntity</exception>
        public static async Task<DbSet<TEntity>> GetDbSetAsync<TEntity>(this DbContext context) where TEntity : class
        {
            DbSet<TEntity> dbset = null;
            if (context == null)
            {
                throw new InvalidOperationException("DBContext cannot be null");
            }
            dbset = context.Set<TEntity>();
            var ent = await dbset.CountAsync(); // This should return InvalidOperationException if TEntity is not in the dbcontext
            return dbset;
        }
    }
}
