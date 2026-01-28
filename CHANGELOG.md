# Changelog

## 28.1.2025

### Changed 

- Change port and ip range configuration to load multiple ranges

### Updated

#### Yang

- improved logging

### Fixed

- Missing error handling for configLoader
- Error when setting a negative value in commands
- NetworkScanner waiting infinitely for reply from non-existent hosts

## 27.1.2025

### Updated

#### Yang

- Add logging to some functions

### Fixed

#### Yang

- Add missing lock on some functions

### Added

#### Yang

- Add network scanner
- Bank robbery command

## 26.1.2025

### Update

#### Yang

- Add more settings to configuration file

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

- Created UI
- Activated the logger visualization

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
