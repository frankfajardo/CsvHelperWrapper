using CsvHelper.Configuration;

namespace CsvImportUtility
{
    /// <summary>
    /// Interface for a CsvClassMap creator
    /// </summary>
    public interface ICsvClassMapCreate
    {
        // <summary>
        /// Creates the CsvClassMap for T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>A CsvClassMap&lt;T&gt;/></returns>
        CsvClassMap<T> CreateMap<T>();
    }
}
