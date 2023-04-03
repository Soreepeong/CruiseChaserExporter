using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using JetBrains.Annotations;

namespace CruiseChaserExporter.Util;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class TaggedLogger {
    public static LogLevelEnum LogLevel = LogLevelEnum.Debug;
    public static LogLevelEnum DebugLevel = LogLevelEnum.Debug;

    private const char IndentChar = ' ';
    private const int IndentMultiplier = 2;

    private readonly string _logTag;

    public TaggedLogger(string logTag, TaggedLogger? parentLogger = null) =>
        _logTag = logTag + (parentLogger is null ? "" : ":" + parentLogger._logTag);

    [StringFormatMethod("format")]
    public void V(string? format = null, params object?[] args) => DoLog(LogLevelEnum.Verbose, null, format, args);
    
    [StringFormatMethod("format")]
    public void D(string? format = null, params object?[] args) => DoLog(LogLevelEnum.Debug, null, format, args);
    
    [StringFormatMethod("format")]
    public void I(string? format = null, params object?[] args) => DoLog(LogLevelEnum.Info, null, format, args);
    
    [StringFormatMethod("format")]
    public void W(string? format = null, params object?[] args) => DoLog(LogLevelEnum.Warning, null, format, args);
    
    [StringFormatMethod("format")]
    public void E(string? format = null, params object?[] args) => DoLog(LogLevelEnum.Error, null, format, args);
    
    [StringFormatMethod("format")]
    public void C(string? format = null, params object?[] args) => DoLog(LogLevelEnum.Critical, null, format, args);

    [StringFormatMethod("format")]
    public void V(Exception e, string? format = null, params object?[] args)
        => DoLog(LogLevelEnum.Verbose, e, format, args);

    [StringFormatMethod("format")]
    public void D(Exception e, string? format = null, params object?[] args)
        => DoLog(LogLevelEnum.Debug, e, format, args);

    [StringFormatMethod("format")]
    public void I(Exception e, string? format = null, params object?[] args)
        => DoLog(LogLevelEnum.Info, e, format, args);

    [StringFormatMethod("format")]
    public void W(Exception e, string? format = null, params object?[] args)
        => DoLog(LogLevelEnum.Warning, e, format, args);

    [StringFormatMethod("format")]
    public void E(Exception e, string? format = null, params object?[] args)
        => DoLog(LogLevelEnum.Error, e, format, args);

    [StringFormatMethod("format")]
    public void C(Exception e, string? format = null, params object?[] args)
        => DoLog(LogLevelEnum.Critical, e, format, args);

    [StringFormatMethod("format")]
    public void V<T>(string? format = null, params object?[] args)
        => DoLog<T>(LogLevelEnum.Verbose, null, format, args);

    [StringFormatMethod("format")]
    public T D<T>(string? format = null, params object?[] args) => DoLog<T>(LogLevelEnum.Debug, null, format, args);
    
    [StringFormatMethod("format")]
    public T I<T>(string? format = null, params object?[] args) => DoLog<T>(LogLevelEnum.Info, null, format, args);
    
    [StringFormatMethod("format")]
    public T W<T>(string? format = null, params object?[] args) => DoLog<T>(LogLevelEnum.Warning, null, format, args);
    
    [StringFormatMethod("format")]
    public T E<T>(string? format = null, params object?[] args) => DoLog<T>(LogLevelEnum.Error, null, format, args);
    
    [StringFormatMethod("format")]
    public T C<T>(string? format = null, params object?[] args) => DoLog<T>(LogLevelEnum.Critical, null, format, args);

    [StringFormatMethod("format")]
    public T V<T>(Exception e, string? format = null, params object?[] args) =>
        DoLog<T>(LogLevelEnum.Verbose, e, format, args);

    [StringFormatMethod("format")]
    public T D<T>(Exception e, string? format = null, params object?[] args) =>
        DoLog<T>(LogLevelEnum.Debug, e, format, args);

    [StringFormatMethod("format")]
    public T I<T>(Exception e, string? format = null, params object?[] args) =>
        DoLog<T>(LogLevelEnum.Info, e, format, args);

    [StringFormatMethod("format")]
    public T W<T>(Exception e, string? format = null, params object?[] args) =>
        DoLog<T>(LogLevelEnum.Warning, e, format, args);

    [StringFormatMethod("format")]
    public T E<T>(Exception e, string? format = null, params object?[] args) =>
        DoLog<T>(LogLevelEnum.Error, e, format, args);

    [StringFormatMethod("format")]
    public T C<T>(Exception e, string? format = null, params object?[] args) =>
        DoLog<T>(LogLevelEnum.Critical, e, format, args);

    [StringFormatMethod("format")]
    private T DoLog<T>(LogLevelEnum level, Exception? e, string? format, object?[] args) {
        DoLog(level, e, format, args);
        return default!;
    }

    [StringFormatMethod("format")]
    private void DoLog(LogLevelEnum level, Exception? e, string? format, object?[] args) {
        if (level < LogLevel && level < DebugLevel)
            return;

        var sb = new StringBuilder();
        sb.Append('[').Append(level switch {
            LogLevelEnum.Verbose => "VRB:",
            LogLevelEnum.Debug => "DBG:",
            LogLevelEnum.Info => "INF:",
            LogLevelEnum.Warning => "WRN:",
            LogLevelEnum.Error => "ERR:",
            LogLevelEnum.Critical => "CRI:",
            LogLevelEnum.None => "---:",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        }).Append(_logTag).Append(']');
        if (!string.IsNullOrWhiteSpace(format)) {
            var formatted = args.Any() ? string.Format(format, args) : format;
            sb.Append(' ').Append(formatted.ReplaceLineEndings("\n" + new string(IndentChar, IndentMultiplier)));
        }

        sb.AppendLine();

        if (e is not null)
            FormatException(sb, e);

        var s = sb.ToString();
        if (level >= LogLevel)
            Console.Write(s);
        if (level >= DebugLevel)
            Debug.Write(s);
    }

    private static void FormatException(StringBuilder sb, Exception e, int depth = 1) {
        sb.Append(IndentChar, depth * IndentMultiplier)
            .Append(e.GetType().Name).Append(": ").Append(e.Message)
            .AppendLine();
        if (e is AggregateException ae) {
            foreach (var ie in ae.InnerExceptions)
                FormatException(sb, ie, depth + 1);
        } else if (e.StackTrace is { } st) {
            sb.Append(IndentChar, depth * IndentMultiplier)
                .Append(st.ReplaceLineEndings("\n" + new string(IndentChar, depth * IndentMultiplier)))
                .AppendLine();
        }
    }
}
