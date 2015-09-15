using CsvHelper.Configuration;

namespace CsvImportUtility
{
    /// <summary>
    /// Interface for a CsvClassMap factory
    /// </summary>
    public interface ICsvClassMapFactory
    {
        /// <summary>
        /// Returns true if there is a CsvClassMap available for T
        /// </summary>
        /// <typeparam name="T">A class</typeparam>
        bool HasMap<T>();

        /// <summary>
        /// Returns the CsvClassMap for T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        CsvClassMap<T> GetMap<T>();
    }
}
