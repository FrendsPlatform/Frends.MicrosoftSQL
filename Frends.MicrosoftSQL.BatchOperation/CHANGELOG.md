# Changelog

## [4.0.0] - 2026-11-09

### Added

- Added new `Connection` properties `UseGivenUserCredentialsForRemoteConnections`, `Username` and `Password` to support Windows Authentication for remote connections.

### Changed

- **[Breaking Change]** Moved `ConnectionString` from `Input` to new `Connection` tab.
- **[Breaking Change]** Exceptions are now rethrown instead of wrapping in general Exception.
- Rollback now uses a non-cancelled token so cancellation of a Process does not prevent transaction rollback.

## [3.0.0] - 2026-11-02

### Changed

- Added the `Error` result object with `Message` and `AdditionalInfo` fields for failed batch operations.
- Upgraded the task and its tests to .NET 8.
- **[Breaking Change]** Removed the `ErrorMessage` result field; use `Error.Message` and `Error.AdditionalInfo` instead.
- **[Breaking Change]** The task rethrows original exceptions instead of wrapping them in a new exception.

## [2.2.0] - 2026-01-22

### Changed

- Improve execution of async methods.

## [2.1.0] - 2024-08-26

### Changed

- Updated Newtonsoft.Json to the latest version 13.0.3.

## [2.0.0] - 2024-08-05

### Changed

- [Breaking] The task now uses Microsoft.Data.SqlClient instead of System.Data.SqlClient.

## [1.0.0] - 2023-02-03

### Added

- Initial implementation
