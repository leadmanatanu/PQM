using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gurux.DLMS;

namespace PQM.Infrastructure.Services
{
    /// <summary>
    /// Standalone value formatter for the DLMS batch sync pipeline (DlmsMeterReader).
    /// This is intentionally separate from DLMSReader.FormatValue(), which uses a different
    /// array separator convention (", " comma-space) and is tied to the interactive/discover
    /// read path. Do NOT merge these two formatters.
    ///
    /// The "yyyy-MM-dd HH:mm:ss" format produced for DateTime/GXDateTime values is a
    /// fixed contract: ParameterValueController.Search() in Stage 2B depends on this exact
    /// format for its ClockString.CompareTo(...) date range filtering. Do NOT change the
    /// format string without updating the controller's ClockStringFormat constant too.
    /// </summary>
    public static class ValueFormatter
    {
        public static string FormatValue(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            // Strings are returned as-is — checked before IEnumerable to
            // prevent iterating over individual characters.
            if (value is string str)
            {
                return str;
            }

            // byte[] -> uppercase hex string, no separators (e.g. "0A1B2C").
            // BitConverter.ToString produces "0A-1B-2C", so we strip the dashes.
            if (value is byte[] bytes)
            {
                return BitConverter.ToString(bytes).Replace("-", "");
            }

            // DateTime -> fixed "yyyy-MM-dd HH:mm:ss" format.
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // DateTimeOffset -> unwrap to local DateTime, same fixed format.
            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // GXDateTime -> extract inner DateTime, apply Year <= 1 guard.
            // Guard is required: meters occasionally return GXDateTime values with
            // Year=0 or Year=1 for wildcarded/invalid timestamps. Formatting these
            // produces nonsense like "0001-01-01 00:00:00" which would corrupt the
            // sync watermark and the display value. Return empty string instead.
            if (value is GXDateTime gxDateTime)
            {
                var dt = gxDateTime.Value.DateTime;
                if (dt.Year <= 1)
                {
                    return string.Empty;
                }
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // IEnumerable (non-string, non-byte[]) -> recursive "[a|b|c]" format.
            // This handles object[] from DLMS profile rows and nested sub-structures.
            if (value is IEnumerable enumerable)
            {
                var formattedElements = enumerable
                    .Cast<object?>()
                    .Select(FormatValue);

                return "[" + string.Join("|", formattedElements) + "]";
            }

            // Fallback: everything else uses ToString().
            return value.ToString() ?? string.Empty;
        }
    }
}
