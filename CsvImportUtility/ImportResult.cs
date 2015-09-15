using System;
using System.Collections.Generic;
using System.IO;

namespace CsvImportUtility
{
    /// <summary>
    /// Holds results of a data import
    /// </summary>
    public class ImportResult
    {
        public ImportResult()
        {
            this.ErrorMessages = new List<string>();
        }

        /// <summary>
        /// The import file
        /// </summary>
        public FileInfo ImportFile { get; set; }

        /// <summary>
        /// The DbContext
        /// </summary>
        public Type DbContextType { get; set; }

        /// <summary>
        /// The data entity affected by the import
        /// </summary>
        public Type EntityType { get; set; }

        /// <summary>
        /// Number of rows read from the import file. This may be different from the actual number of rows in the import file.
        /// </summary>
        public int RowsRead { get; set; }

        /// <summary>
        /// Number of rows imported
        /// </summary>
        public int RowsImported { get; set; }

        /// <summary>
        /// Contains errors encountered during the parsing of the data and the update of the datastore
        /// </summary>
        public List<string> ErrorMessages { get; set; }

        /// <summary>
        /// Indicates when the process starts. Process includes time to parse data.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Indicate when the process ends, whether the data is imported or failing to import due to errors.
        /// </summary>
        public DateTime EndTime { get; set; }
    }

}
