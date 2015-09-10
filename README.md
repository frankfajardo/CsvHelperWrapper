# CSV (Async) Import Handler (Wrapper for CsvHelper)


### License Info

MIT License.


### Overview

This is just a simple wrapper for the CsvHelper utility so that you can do this:
```
ImportHandler.ImportAsync<DbContext, TEntity>(textreader);
```

and this will handle the update to the database.

### Syntax

```
ImportHandler.ImportAsync<DbContext, TEntity>(
    TextReader textReader,
    UpdateOption updateOption,
    CsvClassMap<TEntity> mapper,
    Encoding csvEncoding,
    bool csvHasHeader,
    IProgress<string> progress,
    CancellationToken cancelToken)
````

##### `textReader`
This is your csv input.

##### `updateOption`
This is to specify if you wish to append to the database table or clear the table first before importing the data. The default action is to append data.

##### `mapper`
Specify a `CsvMapper<TEntity>` if your csv does not exactly match your database table.

##### `csvEncoding`
This defaults to UTF8 is not specified.

##### `csvHasHeader`
Defaults to false.

##### `progress`
This is an `IProgress<string>` which returns progress text messages. Use this if you wish to monitor progress of the import.

##### `cancelToken`
Provide a `CancellationToken` if you wish to be able to cancel this async import.

### About CsvHelper

For info on CsvHelper, go [here](https://github.com/JoshClose/CsvHelper)