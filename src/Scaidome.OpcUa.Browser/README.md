# Scaidome.OpcUa.Browser

The Scaidome Browser: a WPF desktop application for browsing and monitoring OPC UA servers. It is built on [Scaidome.OpcUa.Client](https://www.nuget.org/packages/Scaidome.OpcUa.Client) and uses the MVVM pattern without an MVVM framework or DI container.

## Build and run

```sh
dotnet run --project src/Scaidome.OpcUa.Browser
```

Requires Windows and the .NET 10 SDK.

## Usage

1. **Connect.** Click **Connect to Server** and enter the server URL (the default is the OPC Foundation reference server at `opc.tcp://localhost:62541/Quickstarts/ReferenceServer`). Optionally:
   - enable message security (sign & encrypt)
   - trust the server certificate automatically
   - log in with a username and password (anonymous if left empty)

   Recent connections are listed in the dialog, so you can reconnect with one click.
2. **Browse.** The address space is shown as a tree, starting at the Root folder with Objects expanded. Children are loaded when you expand a node.
3. **Monitor.** To add a variable to the monitored items grid, double-click it, drag it onto the grid, or press its **+** button. Use **Add by Node ID…** to add a node by its id, e.g. `ns=2;s=Temperature`.
4. **Read and write.** Each monitored item has buttons to **Read** the current value, **Write** a new value, or stop monitoring (**✕** or **Del**).

The status bar shows whether the client is connected or reconnecting. **Esc** closes an open dialog.

### Writing values

The write dialog reads the node's attributes first and only allows a write when the node is writable for the current user. You can write scalar values of these built-in types: Boolean, the integer types, Float, Double, Duration, String, DateTime, UtcTime and Guid. Numbers use invariant culture (`.` as the decimal separator), and a DateTime is entered as local time. Writing arrays isn't supported.

## Local data

| What | Where |
|---|---|
| Recent connections (the last 10; passwords are never stored) | `%APPDATA%\Scaidome.OpcUa.Browser\recent-connections.json` |
| Certificates and client log | `%LOCALAPPDATA%\Scaidome\` (see [Scaidome.OpcUa.Client](https://www.nuget.org/packages/Scaidome.OpcUa.Client)) |

## Project structure

| Folder | Contents |
|---|---|
| `Services/` | `OpcUaConnection` (owns the `IClient` and one shared subscription), the recent connections store, value formatting and parsing, and client log capture |
| `ViewModels/` | `MainViewModel`, plus one view model per dialog, address space node and monitored item |
| `Views/` | The main window and the overlay dialogs (connect, add by node id, write value) |
| `Mvvm/` | Small MVVM helpers: `ObservableObject`, `RelayCommand`, `AsyncRelayCommand` and `OverlayViewModel` |
| `Converters/` | WPF value converters |
| `Themes/` | The dark theme |

`App.xaml.cs` is the composition root: it creates the logger, the connection and the main view model.

### Implementation notes

- **Routing updates.** Each monitored item's view model is passed as the monitoring context, so an incoming `DataValue` is routed straight to its grid row through `((MonitoredNode)value.Context).Context`.
- **Threading.** Data changes arrive on an OPC UA stack thread and are marshalled to the UI dispatcher. Subscription changes are serialized with a semaphore, because the SDK subscription isn't safe for concurrent changes.
- **Connection state.** The client exposes its state but raises no event when it changes, so the status bar polls it once a second.
- **Connect errors.** The client logs failures instead of throwing them. `ClientLogCapture` keeps the most recent error so the connect dialog can show the real reason. All client log output also goes to the debugger output window.
