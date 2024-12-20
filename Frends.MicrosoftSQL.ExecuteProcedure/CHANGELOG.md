# Changelog

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