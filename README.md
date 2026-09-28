# Scaidome Browser

A Windows desktop client (WPF) for browsing and monitoring OPC UA servers.

## Features

- Connect to an OPC UA server with or without security, anonymously or with username/password
- Recently used connections are remembered (passwords are never persisted)
- Browse the server's address space as a tree
- Monitor variables by double-clicking, dragging or pressing **+**, or add a node by its Node ID
- Live values with data type, status code and source/server timestamps
- Read and write values of monitored items

## Solution structure

| Project | Description |
|---|---|
| `src/Scaidome.OpcUa.Browser` | The WPF browser application (MVVM) |

The OPC UA client is consumed from NuGet:

| Package | Description |
|---|---|
| [`Scaidome.OpcUa.Client`](https://www.nuget.org/packages/Scaidome.OpcUa.Client) | OPC UA client wrapping the OPC Foundation SDK. The SDK is referenced with `PrivateAssets="compile"`, so consumers can't compile against it directly |
| [`Scaidome.Abstractions`](https://www.nuget.org/packages/Scaidome.Abstractions) | Shared types (`DataValue`, `StatusCodes`, messages) that do not depend on the OPC UA SDK. Comes in transitively via `Scaidome.OpcUa.Client` |

Package versions are managed centrally in `Directory.Packages.props`.

## Requirements

- Windows
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Build and run

```sh
dotnet build Scaidome.OpcUa.Browser.slnx
dotnet run --project src/Scaidome.OpcUa.Browser
```

## Certificates

OPC UA clients and servers must trust each other's certificates. On the first secure connection, the server certificate is rejected unless **auto-accept server certificate** is enabled in the connect dialog. The server will likewise reject the client certificate until it is trusted on the server side.

See the [Scaidome.OpcUa.Client package](https://www.nuget.org/packages/Scaidome.OpcUa.Client) documentation for details on security and the PKI certificate store layout.

A public demo server to test against: <https://github.com/digitalpetri/opc-ua-demo-server>

## License

Proprietary. See [LICENSE](LICENSE).
