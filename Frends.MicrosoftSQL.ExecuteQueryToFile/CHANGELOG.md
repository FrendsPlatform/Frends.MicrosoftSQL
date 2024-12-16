# Changelog

## [2.1.0] - 2024-12-16
### Added
- Added Microsoft.SqlServer.Types dependency so that SqlGeography and SqlGeometry typed objects can be handled.

## [2.0.0] - 2024-08-05
### Changed
- [Breaking] The task now uses Microsoft.Data.SqlClient instead of System.Data.SqlClient.

## [1.0.1] - 2024-02-12
### Fixed
- Fixed handling of null query parameters by changing the parameter value to DBNull.Value.

## [1.0.0] - 2024-02-07
### Changed
- Initial implementation
