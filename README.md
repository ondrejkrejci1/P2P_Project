# P2P-Project
## Authors
* **Ondřej Krejčí**
* **Zhao Xiang Yang**

## Description
This project implements a decentralized Peer-to-Peer (P2P) banking node. Each node represents a standalone bank capable of account management, transaction processing, and inter-bank communication.


---

## Execution and Control

### Requirements
* **Environment:** .NET 8.0 Runtime.
* **Manual Control:** Compatible with **PuTTY** (Raw mode) or Telnet for manual command entry.

### How to Run
The application is a console-based P2P node. To run it without an IDE:

1. Open a terminal/command prompt in the project root.
2. Run the following command:
   ```bash
   dotnet run --project P2P_Project.csproj
   ```
The application will start listening on the port defined in your `config.json` (within the range 65525–65535).

---

## Configuration

The application uses `config/config.json` to manage node identity and network discovery settings.

### Schema Description

| Key | Type | Description |
| :--- | :--- | :--- |
| **IPAddress** | string | Ipv4 address of the application. |
| **Port** | int | The TCP port the node listens on. Must be between 1024 and 65535. |
| **TimeoutTime** | int | Global timeout in milliseconds for network operations. Recommended: 200-500ms. |
| **ScanIpRanges** | array | A list of IP ranges (Start/End) to scan for other peers. |
| **ScanPortRanges** | array | A list of port ranges (Start/End) to scan for the bank node aplication. |

### Example Configuration (config.json)

```json
{
  "IPAddress": "127.0.0.1",
  "Port": 65525,
  "TimeoutTime": 200,
  "ScanIpRanges": [
    {
      "Start": "10.0.0.1",
      "End": "10.0.0.254"
    },
    {
      "Start": "192.168.1.50",
      "End": "192.168.1.100"
    }
  ],
  "ScanPortRanges": [
    {
      "Start": 65525,
      "End": 65535
    },
	{
      "Start":8080,
      "End": 8090
    }
  ]
}
```

---

## Banking Protocol and Commands

All communication is text-based using **UTF-8** encoding.
* **Requests:** Must start with a 2-letter command code.
* **Responses:** Must start with the same code (success) or `ER` (error).

### Core Commands

| Code | Command Name | Description | Example Call | Success Response |
| :--- | :--- | :--- | :--- | :--- |
| **BC** | Bank Code | Returns the node's IP address. Used as a ping. | `BC` | `BC 10.1.2.3` |
| **AC** | Account Create | Creates a new account (ID 10000-99999). | `AC` | `AC 12345/10.1.2.3` |
| **AD** | Account Deposit | Deposits funds into an account. | `AD 12345/10.1.2.3 500` | `AD` |
| **AW** | Account Withdraw | Withdraws funds if balance permits. | `AW 12345/10.1.2.3 200` | `AW` |
| **AB** | Account Balance | Returns the current account balance. | `AB 12345/10.1.2.3` | `AB 300` |
| **AR** | Account Remove | Deletes an account (only if balance is 0). | `AR 12345/10.1.2.3` | `AR` |
| **BA** | Bank Amount | Returns total funds held by this bank. | `BA` | `BA 500000` |
| **BN** | Bank Number | Returns the total number of clients. | `BN` | `BN 12` |
| **RP** | Robbery Plan | Calculates optimal heist strategy. | `RP 1000000` | `RP <details>` |
