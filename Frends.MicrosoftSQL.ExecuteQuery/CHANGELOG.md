# Changelog

## [2.0.0] - 2024-08-01
### Changed
- [Breaking] The task now uses Microsoft.Data.SqlClient instead of System.Data.SqlClient.

## [1.2.1] - 2024-03-01
### Changed
- Removed finally block from the Task so that the SQLConnection pool is not touched after every call to the ExecuteQuery method.
### Updated
- Newtonsoft.Json to version 13.0.3
- System.Data.SqlClient to version 4.8.6

## [1.2.0] - 2023-11-30
### Changed
- [Breaking] QueryParameter.Value type to object so that binary data can be used.

## [1.1.0] - 2023-01-27
### Changed
- Naming: Result.QueryResult to Result.Data.

## [1.0.0] - 2023-01-18
### Added
- Initial implementation