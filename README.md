# DotNetOpenTelemetry

**DotNetOpenTelemetry** is a .NET project demonstrating the integration of OpenTelemetry for observability in .NET applications. This repository showcases how to implement distributed tracing, metrics, and logging using OpenTelemetry in a .NET environment as well as redacting sensitive information via Destructurama, with [Seq](https://datalust.co/seq) as the observability backend.

## Features

- **Distributed Tracing**: Monitor and diagnose complex applications.
- **Metrics Collection**: Gather and export metrics for performance analysis.
- **Logging Integration**: Seamlessly integrate structured logging with OpenTelemetry.
- **Redactions**: Redact sensitive information when doing structured logging.

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version 6.0 or later)
- [Docker](https://www.docker.com/get-started) (for running Seq)

### Installation

1. **Clone the repository**:

   ```bash
   git clone https://github.com/Human-Glitch/DotNetOpenTelemetry.git
   cd DotNetOpenTelemetry
   ```

2. **Restore dependencies**:

   ```bash
   dotnet restore
   ```

3. **Build the project**:

   ```bash
   dotnet build
   ```

4. **Run the application**:

   ```bash
   dotnet run
   ```

## Setting Up Seq as the Observability Backend

To visualize and analyze the telemetry data, we'll set up [Seq](https://datalust.co/seq) using Docker.

### 1. Run Seq Using Docker

You can run Seq in a Docker container with the following command:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq
```

After running the command, you can access the Seq UI by navigating to [http://localhost:5341](http://localhost:5341) in your web browser.

## Configuring the .NET Application to Send Telemetry to Seq

The application is configured to read the Seq endpoint and API key from the configuration. To set these values securely, you can use the [Secret Manager tool](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) provided by .NET.

### 1. Set User Secrets

Run the following commands in your project directory to set the necessary secrets:

```bash
dotnet user-secrets set "Seq:BaseUrl" "http://localhost:5341"
dotnet user-secrets set "Otlp:ApiKey" "your-api-key"
```

Replace `"your-api-key"` with your actual Seq API key. If you're using the default Seq setup without authentication, you can omit setting the `Otlp:ApiKey`.

### 2. Access Configuration in Code

In your `Program.cs` or wherever you're configuring OpenTelemetry, retrieve the configuration values as follows:

```csharp
var builder = WebApplication.CreateBuilder(args);

var apiKey = builder.Configuration["Otlp:ApiKey"];
var seqBaseUrl = builder.Configuration["Seq:BaseUrl"];

// Use these values to configure OpenTelemetry exporters
```

Ensure that your OpenTelemetry exporters are configured to use these values to send telemetry data to Seq.

## License

This project is licensed under the [Apache 2.0 License](LICENSE).

---
