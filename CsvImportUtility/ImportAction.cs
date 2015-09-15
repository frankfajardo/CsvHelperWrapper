namespace CsvImportUtility
{
    /// <summary>
    /// Defines import actions
    /// </summary>
    public enum ImportAction
    {
        /// <summary>
        /// Appends the imported data to the target dataset
        /// </summary>
        Append,

        /// <summary>
        /// Clears existing data in the target dataset before data is imported
        /// </summary>
        Replace
    }
}
