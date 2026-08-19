using System.Data.Common;
using System.Globalization;
using System.Text;

namespace Beginor.SharpDb;

public static class MarkdownTableFormatter {

    public static async Task<string> FormatAsync(
        DbDataReader reader,
        int? maxRows = null,
        CancellationToken cancellationToken = default
    ) {
        var columnCount = reader.FieldCount;
        if (columnCount == 0) {
            return string.Empty;
        }

        var builder = new StringBuilder();

        AppendRow(builder, GetColumnNames(reader, columnCount));
        AppendSeparator(builder, columnCount);

        var rowCount = 0;
        var truncated = false;
        while (await reader.ReadAsync(cancellationToken)) {
            if (maxRows is not null && rowCount == maxRows) {
                truncated = true;
                break;
            }
            rowCount++;
            AppendRow(builder, GetValues(reader, columnCount));
        }

        if (rowCount == 0) {
            builder.AppendLine();
            builder.Append("_No rows returned._");
        }
        else if (truncated) {
            builder.AppendLine();
            builder.Append($"_Showing first {maxRows} rows; result truncated._");
        }

        return builder.ToString().TrimEnd();
    }

    private static IEnumerable<string> GetColumnNames(DbDataReader reader, int columnCount) {
        for (var i = 0; i < columnCount; i++) {
            yield return reader.GetName(i);
        }
    }

    private static IEnumerable<string> GetValues(DbDataReader reader, int columnCount) {
        for (var i = 0; i < columnCount; i++) {
            yield return FormatValue(reader.GetValue(i));
        }
    }

    private static string FormatValue(object? value) {
        return value switch {
            null or DBNull => "NULL",
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static void AppendSeparator(StringBuilder builder, int columnCount) {
        builder.Append('|');
        for (var i = 0; i < columnCount; i++) {
            builder.Append(" --- |");
        }
        builder.AppendLine();
    }

    private static void AppendRow(StringBuilder builder, IEnumerable<string> values) {
        builder.Append('|');
        foreach (var value in values) {
            builder.Append(' ');
            builder.Append(EscapeCell(value));
            builder.Append(" |");
        }
        builder.AppendLine();
    }

    private static string EscapeCell(string value) {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal)
            .Replace("\r", "<br>", StringComparison.Ordinal);
    }

}
