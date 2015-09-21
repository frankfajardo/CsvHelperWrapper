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
    CsvConfiguration config,
    IProgress<string> Progress,
    CancellationToken CancelToken)
````

##### CsvFilePath
This contains the full UNC path of the csv file to import.

##### ImportAction
This specifies whether to append to the relevant dataset or replace (ie, clear existing) content of that dataset before importing data

##### CsvConfiguration
This is the [CsvConfiguration] to use, if not using the default configuration for CsvHelper. You can specify here how to read the csv file, or how to map it to the target `TEntity` if the row structure of your csv does not quite match the target.

##### Progress
This is an `IProgress<string>` which returns progress text messages during the operation. Use this if you wish to monitor progress of the import.

##### CancelToken
This is a `CancellationToken` you can provide if you wish to be able to cancel import.

### About CsvHelper

For info on CsvHelper, check out [CsvHelper documentation]

[CsvHelper documentation]:http://joshclose.github.io/CsvHelper/
[CsvConfiguration]:http://joshclose.github.io/CsvHelper/\#configuration
[CsvHelper on GitHub]:https://github.com/JoshClose/CsvHelper
