# Changelog

## [2.2.0] - 2024-11-26
### Added
- Added method to form JToken from the SqlDataReader so that SqlGeography typed objects can be handled.
- Fixed how Scalar handles the data so that SqlGeography typed objects can be handled.
- Added Microsoft.SqlServer.Types version 160.1000.6 as dependency.

## [2.1.0] - 2024-09-10
### Fixed
- Fixed how null values are handled by setting them as DBNull.Value.
- Fixed how JValue parameters are handled by adding a check for those values and assigning ToString() method on the values.

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