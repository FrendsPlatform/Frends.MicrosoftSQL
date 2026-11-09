# Changelog

## [5.0.0] - 2026-11-09

### Added

- Added new `Connection` properties `UseGivenUserCredentialsForRemoteConnections`, `Username` and `Password` to support Windows Authentication for remote connections.

### Changed

- **[Breaking Change]** Moved `ConnectionString` from `Input` to new `Connection` tab.
- **[Breaking Change]** Exceptions are now rethrown instead of wrapping in general Exception.
- Rollback now uses a non-cancelled token so cancellation of a Process does not prevent transaction rollback.

## [4.0.0] - 2026-11-02

### Changed

- **[Breaking Change]** Replaced `Result.ErrorMessage` with `Result.Error` object containing `Message` and `AdditionalInfo`.
- **[Breaking Change]** Error messages and exception types thrown on failure have changed; the original exception is now thrown (with details of the transaction state in the message when a bulk insert fails).
- Added `Options.ErrorMessageOnFailure` to override the error message on failure.
- Upgraded the Task to target .NET 8.

## [3.3.0] - 2026-06-18

### Changed

- In successful execution, Result.Count will show number of all rows.
- In case of failure, Result.Count will show estimated number of rows copied before the failure.

## [3.2.0] - 2026-06-18

### Changed

- Update documentation.

## [3.1.0] - 2026-01-22

### Changed

- Improve execution of async methods.

## [3.0.0] - 2025-01-15

### Added

- [Breaking] Added parameters ColumnMapping and ManualColumnMapping.
- Added column mapping feature which allows user to select from JsonPropertyNames, JsonPropertyOrder and
  ManualColumnMapping options how the column mapping is handled in bulk insert. Default behavior will remain the same
  JsonPropertyOrder.

## [2.2.0] - 2024-09-10

### Changed

- Updated Options.NotifyAfter property to be set dynamically based on the total row count, with a minimum value of 1,
  ensuring rowsCopied is updated correctly.

## [2.1.0] - 2024-08-26

### Changed

- Updated Newtonsoft.Json to the latest version 13.0.3.

## [2.0.0] - 2024-08-05

### Changed

- [Breaking] The task now uses Microsoft.Data.SqlClient instead of System.Data.SqlClient.

## [1.1.0] - 2023-01-26

### Added

- Options.ThrowErrorOnFailure and Result.ErrorMessage was added to let the user choose how to handle errors.

## [1.0.0] - 2023-01-10

### Added

- Initial implementation
