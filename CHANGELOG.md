# Changelog

## 24.1.2025

### Changed

#### Yang

- Change logging to use Serilog library
- Rename ConnectionManager to TcpConnection
- Change ConfigLoader to singleton

### Fixed

#### Yang

- Add missing imports
- Add empty constructor for BankAccount deserialization


### Added

#### Yang

- Add BankRepository
- Add class for managing connections
- Add working command execution

#### Krejčí

## 23.1.2025

### Added

#### Yang

- Command parser
- Command execution logic.
- Logger
- Config loader

#### Krejčí

- Tcp listener
- Client connection manager
