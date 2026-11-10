# Changelog

## [4.0.0] - 2026-11-09

### Changed

- Require Windows-authenticated connection strings and disable pooling when remote credentials are enabled.

### Added

- Added new `Connection` properties `UseGivenUserCredentialsForRemoteConnections`, `Username` and `Password` to support Windows Authentication for remote connections.

### Changed

- **[Breaking Change]** Moved `ConnectionString` from `Input` to new `Connection` tab.

## [3.0.0] - 2026-11-02

### Changed

- Updated the task to .NET 8.
- Added `Options.ErrorMessageOnFailure` and `Result.Error` with `Message` and `AdditionalInfo` fields.
- **[Breaking Change]** Removed `Result.ErrorMessage`; use `Result.Error.Message` instead.
- **[Breaking Change]** When `Options.ThrowErrorOnFailure` is enabled without a custom error message, the task now rethrows the original exception instead of wrapping it in a new exception.

## [2.3.0] - 2026-01-22

### Changed

- Improve execution of async methods.

## [2.2.0] - 2024-12-16

- Added method to form JToken from the SqlDataReader so that SqlGeography and SqlGeometry typed objects can be handled.
- Fixed how Scalar handles the data so that SqlGeography and SqlGeometry typed objects can be handled.
- Added Microsoft.SqlServer.Types version 160.1000.6 as dependency.

## [2.1.0] - 2024-08-26

### Changed

- Updated Newtonsoft.Json to the latest version 13.0.3.

## [2.0.0] - 2024-08-05

### Changed

- [Breaking] The task now uses Microsoft.Data.SqlClient instead of System.Data.SqlClient.

## [1.2.1] - 2024-02-12

### Fixed

- Fixed issue with null parameters by changing them into DBNull.Value.

### Updated

- System.Data.SqlClient to version 4.8.6.

## [1.2.0] - 2024-01-03

### Changed

- [Breaking] ProcedureParameter.Value type to object so that binary data can be used.

## [1.0.1] - 2023-08-03

### Changed

- Documentation update to Input.Execute parameter.
- Removed unnecessary runtime unloader.

## [1.0.0] - 2023-01-23

### Added

- Initial implementation
