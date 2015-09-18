# CSV Import Handler (Async) - Wrapper for CsvHelper


### License Info

MIT License.

Please note separate licensing info for [CsvHelper on GitHub].


### Overview

This is just a simple wrapper for the CsvHelper utility so that you can do this:
```
ImportHandler.ImportAsync<DbContext, TEntity>(csvFilePath);
```

and this will handle the update to the database.

### Syntax

```
ImportHandler.ImportAsync<DbContext, TEntity>(
    string CsvFilePath
    ImportAction ImportAction,
    Encoding Encoding,
    ICsvClassMapCreate CsvClassMapCreator,
    bool HasHeaderRow,
    IProgress<string> Progress,
    CancellationToken CancelToken)
````

##### CsvFilePath
This contains the full UNC path of the csv file to import.

##### ImportAction
This specifies whether to append to the relevant dataset or replace (ie, clear existing) content of that dataset before importing data

##### Encoding
This specifies the encoding of the import file. Defaults to UTF8 if not specified.

##### CsvClassMapCreator
This specifies the creator of a `CsvClassMap<TEntity>` for this import. If not specified, CsvHelper's auto-map behaviour is applied for the import.

##### HasHeaderRow
This indicates if the csv file as a header row. Defaults to false.

##### Progress
This is an `IProgress<string>` which returns progress text messages during the operation. Use this if you wish to monitor progress of the import.

##### CancelToken
This is a `CancellationToken` you can provide if you wish to be able to cancel import.

### About CsvHelper

For info on CsvHelper, check out [CsvHelper on Github]

[CsvHelper on GitHub]:https://github.com/JoshClose/CsvHelper