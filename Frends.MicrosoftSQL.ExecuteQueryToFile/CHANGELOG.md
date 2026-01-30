# Changelog

## [2.3.0] - 2026-01-30
### Fixed
- Fixed an issue that was causing problems with Frends processes' cleanup and assembly unloading.

## [2.2.0] - 2026-01-22

### Changed

- Improve execution of async methods.

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
