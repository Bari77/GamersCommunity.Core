# GamersCommunity.Core.Logging

`GamersCommunity.Core.Logging` is the centralized logging subsystem of the **GamersCommunity** ecosystem.  
It provides a production‑ready Serilog bootstrapper with advanced formatting, multi‑sink log level control, HTTP log separation, and strongly‑typed configuration settings.

> Status: **Work in progress**.  
> External contributions are not actively solicited, but feedback and issue reports are welcome.

---

## ✨ Introduction

This package gives every GamersCommunity microservice, gateway, worker, or tool a **consistent, configurable, structured logging layer** without repeating the same setup.

It wraps Serilog with a standard configuration that includes:

- **Unified console formatting** with custom colors
- **Automatic detection of HTTP logs** (messages starting with `HTTP`)
- **Separated sub-loggers** for HTTP and non-HTTP entries
- **Environment & application enrichment**
- **Per‑sink minimum log levels** (Console, File)
- **Daily file rotation** when a file path is set
- **OTLP log export** when `Telemetry:OtlpEndpoint` is set (Development exports to the local Aspire dashboard by default)
- **Strongly-typed settings** (`LoggerSettings` / `LogMinimumLevel`)
- **Serilog internal diagnostics** written to `serilog_errors.txt`

---

## 🚀 Getting Started

### Requirements
- **.NET 10**
- Visual Studio 2022 / VS 2026 / Rider / VS Code

### Installation

Install from your NuGet feed:

```bash
dotnet add package GamersCommunity.Core.Logging
```

---

## ⚙️ Usage

### 1. Add configuration to *appsettings.json*

```json
{
  "LoggerSettings": {
    "MinimumLevel": {
      "Global": "Information",
      "ConsoleHttp": "Information",
      "ConsoleNotHttp": "Debug",
      "File": "Debug",
      "EntityFrameworkCore": "Warning"
    },
    "FilePath": "logs/log-.txt"
  }
}
```

All fields are optional — defaults are provided.

---

### 2. Bind settings + initialize logger (Program.cs)

```csharp
var loggerSettings = builder.Configuration
    .GetSection("LoggerSettings")
    .Get<LoggerSettings>() ?? new LoggerSettings();

GamersCommunity.Core.Logging.Logger.Initialize(
    loggerSettings,
    applicationName: "MyService",
    environment: builder.Environment
);

builder.Host.UseSerilog();
```

---

## 🔧 Logger Settings Overview

### `LoggerSettings`

| Property      | Description |
|---------------|-------------|
| `MinimumLevel` | Set of minimum log levels per sink |
| `FilePath`     | Path for rolling log files. Empty skips the file sink |

---

### `LogMinimumLevel`

This class enables **fine‑grained control** over log levels per output target:

| Property               | Applies to |
|------------------------|------------|
| `Global`               | Base global level for all logs |
| `ConsoleHttp`          | Console logs whose message begins with `"HTTP"` |
| `ConsoleNotHttp`       | Console logs not starting with `"HTTP"` |
| `File`                 | Rolling file logs |
| `EntityFrameworkCore`  | EF Core log level override |

Default values:

```json
{
  "Global": "Verbose",
  "ConsoleHttp": "Verbose",
  "ConsoleNotHttp": "Verbose",
  "File": "Debug",
  "EntityFrameworkCore": "Warning"
}
```

---

## 🎨 Console Output Formatting

### Non‑HTTP logs

```
[21/11/2025 10:45:12 - Information] [Env:Development] [App:Gateway] - Service started successfully
```

### HTTP logs

```
[21/11/2025 10:45:12 - Information] [Env:Production] [App:Gateway] [Ip:127.0.0.1] [Sender:User42] [UserId:7] - HTTP GET /api/users/7 => 200
```

Colors adapt by level thanks to the custom `SystemConsoleTheme`. On `Error` and `Fatal` entries the message
is printed in red; the timestamp, the `Env`/`App` prefix and the stack frames keep their usual colors.

---

## 📦 File Logging

When `FilePath` is defined, logs are written to:

```
logs/log-20251121.txt
```

Features:
- Daily rolling files
- 7‑day retention
- Custom minimum level via `MinimumLevel.File`

---

## 📡 Centralized logs

Structured logs leave the process through the OTLP sink when an endpoint is resolved: the local Aspire dashboard in Development, or `Telemetry:OtlpEndpoint` in staging and production. Console output stays in the process terminal. Rolling files are written only when `FilePath` is set.

---

## 🪲 Serilog Internal Diagnostics

Internal Serilog errors (sink failures, formatting issues) are logged to:

```
serilog_errors.txt
```

Very useful when debugging sink or formatting problems.

---

## 💬 Feedback

👉 Issues: https://github.com/Bari77/GamersCommunity.Core/issues

Bug reports and suggestions are always appreciated.

---

## 🤝 Contributing

PRs are not actively requested, but constructive feedback is welcome.
To contribute:

- Fork the repo
- Create a branch
- Open a **Draft PR** for discussion

The team may close PRs that don’t align with the roadmap.

---

## 🧩 Versioning

Breaking changes may occur before **v1.0**.

---

## 📜 License

A permissive license (MIT or similar) will be published soon.

---

## 🙌 Credits

Built with ❤️ by the GamersCommunity maintainers.
