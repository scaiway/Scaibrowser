namespace Scaidome.OpcUa.Browser.Services;

/// <summary>What the user filled in on the connect dialog. Passwords are never persisted.</summary>
public sealed record ConnectionSettings(
    string ServerUrl,
    bool UseSecurity,
    bool AutoAcceptServerCertificate,
    string? UserName,
    string? Password);
