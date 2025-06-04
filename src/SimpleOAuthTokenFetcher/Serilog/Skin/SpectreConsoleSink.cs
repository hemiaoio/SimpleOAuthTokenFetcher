using Serilog.Core;
using Serilog.Events;
using Spectre.Console;

namespace SimpleOAuthTokenFetcher.Serilog.Skin;

public class SpectreConsoleSink : ILogEventSink
{
    /// <summary>Emit the provided log event to the sink.</summary>
    /// <param name="logEvent">The log event to write.</param>
    /// <seealso cref="T:Serilog.Core.IBatchedLogEventSink" />
    /// <remarks>Implementers should allow exceptions to propagate when event emission fails. The logger will handle
    /// exceptions and produce diagnostics appropriately.</remarks>
    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage().EscapeMarkup();
        var color = logEvent.Level switch
        {
            LogEventLevel.Information => "green",
            LogEventLevel.Warning => "yellow",
            LogEventLevel.Error => "red",
            LogEventLevel.Fatal => "red bold",
            LogEventLevel.Debug => "blue",
            _ => "white"
        };
        AnsiConsole.MarkupLine($"[{color}]{logEvent.Timestamp:HH:mm:ss} [[{logEvent.Level}]] {message}[/]");
    }
}