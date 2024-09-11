# Changelog

## [2.2.0] - 2024-09-10
### Changed
- Updated Options.NotifyAfter property to be set dynamically based on the total row count, with a minimum value of 1, ensuring rowsCopied is updated correctly.

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