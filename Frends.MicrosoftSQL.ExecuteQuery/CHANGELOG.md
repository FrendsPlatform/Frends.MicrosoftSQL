# Changelog

## [1.3.0] - 2024-08-02
### Fixed
- If parameter value is `null`, it will be passed as `DBNull.Value` (#35)

## [1.2.0] - 2023-11-30
### Changed
- [Breaking] QueryParameter.Value type to object so that binary data can be used.

## [1.1.0] - 2023-01-27
### Changed
- Naming: Result.QueryResult to Result.Data.

## [1.0.0] - 2023-01-18
### Added
- Initial implementation