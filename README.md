# Stock Analysis App

This repository contains a simple stock analysis application built on top of **ASP.NET Core** and **C#**.  The goal of the project is to provide a foundation for building quantitative trading tools that ingest historical market data, backtest technical strategies and deliver actionable alerts in real time.

## Architecture

The application is split into two main components:

* **StockApi** – a minimal ASP.NET Core Web API project that exposes REST endpoints for backtesting and alerts.  Additional controllers and services can be added here as you expand the functionality.
* **BackfillConsole** – a .NET console application that downloads historical minute-level bars from a market data provider (such as Polygon.io) and stores them into a PostgreSQL/TimescaleDB database.  This script can be run on demand or scheduled as a recurring task to keep your database up to date.

This repository does not include the front-end Angular client described in the design discussions, but it lays the groundwork for adding a user interface later on.  The backend projects can be built and run independently.

## Prerequisites

1. **.NET 8 SDK** – make sure the .NET 8 SDK is installed on your machine.  You can download it from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).
2. **Docker** – TimescaleDB is typically run inside a Docker container.  Follow the instructions in the project documentation to start a TimescaleDB container.
3. **PostgreSQL/TimescaleDB** – the backfill script requires a running instance of PostgreSQL with the TimescaleDB extension enabled.  You can run the official TimescaleDB image:

   ```sh
   docker run -d --name timescaledb -e POSTGRES_PASSWORD=postgres -p 5432:5432 timescale/timescaledb:latest-pg16
   ```

4. **Polygon.io API key** – to fetch historical data you need a Polygon.io API key.  Sign up at <https://polygon.io> and set the `POLYGON_KEY` environment variable before running the backfill script.

## Projects

### StockApi

The `StockApi` project is a minimal ASP.NET Core Web API with a single controller to illustrate how you might structure endpoints.  Currently it contains a placeholder `BacktestController` that simply echoes a default message.  As you build out the application, you can add routes for submitting backtests, retrieving results, and managing alerts.

To run the API:

```sh
cd stock-app/backend/StockApi
dotnet run
```

The API will listen on `http://localhost:5000` by default.  You can test the backtest endpoint with a tool like `curl`:

```sh
curl -X POST http://localhost:5000/api/backtest -H "Content-Type: application/json" -d '{}'
```

### BackfillConsole

The `BackfillConsole` project downloads historical minute-level bars for a given ticker symbol from Polygon.io and inserts them into a TimescaleDB table.  It demonstrates how to call a REST API with `HttpClient`, parse the response, and bulk insert into PostgreSQL using Dapper.

To run the backfill script:

```sh
cd stock-app/backend/BackfillConsole
dotnet run -- SPY 2025-08-05 2025-08-08
```

Replace `SPY` with the symbol you want to backfill, and adjust the date range as needed.  The script uses the `POLYGON_KEY` environment variable, so be sure it is set in your shell.

## Database Schema

Before running the backfill script, create the `minute_bars` table and hypertable in your TimescaleDB database:

```sql
CREATE EXTENSION IF NOT EXISTS timescaledb;

CREATE TABLE IF NOT EXISTS minute_bars (
    symbol TEXT NOT NULL,
    ts TIMESTAMPTZ NOT NULL,
    open DOUBLE PRECISION,
    high DOUBLE PRECISION,
    low DOUBLE PRECISION,
    close DOUBLE PRECISION,
    volume BIGINT,
    PRIMARY KEY (symbol, ts)
);
SELECT create_hypertable('minute_bars', 'ts', if_not_exists => TRUE);
```

## License

This project is provided under the MIT License.  See [LICENSE](LICENSE) for details.
