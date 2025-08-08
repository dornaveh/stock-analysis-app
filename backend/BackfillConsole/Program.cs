backend/BackfillConsole/Program.csusing System.Net.Http.Json;
using Dapper;
using Npgsql;

// This console application backfills historical minute‑level bars from Polygon.io into a
// TimescaleDB/PostgreSQL table named minute_bars.  It accepts up to three
// command‑line arguments: the symbol, start date and end date.

var apiKey = Environment.GetEnvironmentVariable("POLYGON_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("POLYGON_KEY environment variable is not set.  Please export your Polygon.io API key.");
    return;
}

// Determine the symbol and date range from arguments or use sensible defaults.
var symbol = args.Length > 0 ? args[0] : "SPY";
DateTime startDate;
DateTime endDate;
if (args.Length > 1)
{
    if (!DateTime.TryParse(args[1], out startDate))
    {
        Console.Error.WriteLine($"Invalid start date: {args[1]}");
        return;
    }
}
else
{
    startDate = DateTime.UtcNow.Date.AddDays(-5);
}

if (args.Length > 2)
{
    if (!DateTime.TryParse(args[2], out endDate))
    {
        Console.Error.WriteLine($"Invalid end date: {args[2]}");
        return;
    }
}
else
{
    endDate = DateTime.UtcNow.Date;
}

// Ensure start date is not after end date
if (startDate > endDate)
{
    Console.Error.WriteLine("Start date cannot be after end date.");
    return;
}

await using var conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=postgres;Database=postgres");
await conn.OpenAsync();

using var http = new HttpClient { BaseAddress = new Uri("https://api.polygon.io/") };

var current = startDate.Date;
while (current <= endDate.Date)
{
    var url = $"v2/aggs/ticker/{symbol}/range/1/minute/{current:yyyy-MM-dd}/{current:yyyy-MM-dd}?adjusted=true&sort=asc&limit=50000&apiKey={apiKey}";
    try
    {
        var response = await http.GetFromJsonAsync<AggResponse>(url);
        var results = response?.results ?? new List<Result>();

        var rows = results.Select(r => new
        {
            symbol,
            ts = DateTimeOffset.FromUnixTimeMilliseconds(r.t).UtcDateTime,
            o = r.o,
            h = r.h,
            l = r.l,
            c = r.c,
            v = r.v
        }).ToArray();

        const string sql = @"INSERT INTO minute_bars (symbol, ts, open, high, low, close, volume)
                             VALUES (@symbol, @ts, @o, @h, @l, @c, @v)
                             ON CONFLICT (symbol, ts) DO NOTHING;";

        if (rows.Length > 0)
        {
            await conn.ExecuteAsync(sql, rows);
            Console.WriteLine($"Inserted {rows.Length} bars for {symbol} on {current:yyyy-MM-dd}.");
        }
        else
        {
            Console.WriteLine($"No data returned for {symbol} on {current:yyyy-MM-dd}.");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to fetch data for {current:yyyy-MM-dd}: {ex.Message}");
    }

    // Advance to the next day
    current = current.AddDays(1);
    // Be gentle on the API rate limits
    await Task.Delay(200);
}

// Response classes to deserialize Polygon JSON
public record AggResponse(List<Result> results);
public record Result(long t, double o, double h, double l, double c, long v);
