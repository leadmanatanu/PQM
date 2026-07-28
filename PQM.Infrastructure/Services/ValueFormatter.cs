using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gurux.DLMS;
using Gurux.DLMS.Enums;

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

            // byte[] -> Check if DLMS octet-string date/time before falling back to hex.
            if (value is byte[] bytes)
            {
                if (bytes.Length >= 5 && bytes.Length <= 12)
                {
                    try
                    {
                        var gx = (GXDateTime)GXDLMSClient.ChangeType(bytes, DataType.DateTime);
                        if (gx != null && gx.Value.DateTime.Year > 1)
                        {
                            return gx.Value.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                    catch
                    {
                        try
                        {
                            var gxDate = (GXDateTime)GXDLMSClient.ChangeType(bytes, DataType.Date);
                            if (gxDate != null && gxDate.Value.DateTime.Year > 1)
                            {
                                return gxDate.Value.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
                            }
                        }
                        catch { }
                    }
                }

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
                var list = enumerable.Cast<object?>().ToList();

                // If this is a 2-element DLMS register structure [scalarValue, unit/type], return the scalar value
                if (list.Count == 2 && list[1] != null)
                {
                    string typeName = list[1]!.GetType().Name;
                    if (typeName.Contains("Unit") || typeName.Contains("DataType") || typeName.Contains("ObjectType"))
                    {
                        return FormatValue(list[0]);
                    }
                }

                var formattedElements = list.Select(FormatValue);
                return "[" + string.Join("|", formattedElements) + "]";
            }

            // Fallback: everything else uses ToString().
            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Sanitizes raw string values from ReadingValues, DeviceLatestReadings, or DeviceEvents.
        /// Strips legacy bracketed DLMS structure format (e.g. "[0.001|Current]" -> "0.001").
        /// Decodes DLMS octet-string date/time hex values (e.g. "07EA071BFF0C13FFFF014A00" -> "2026-07-27 12:19:00").
        /// </summary>
        public static string CleanValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            var trimmed = rawValue.Trim();

            // Strip bracketed DLMS structure format from historical rows, e.g. "[0.001|Current]" -> "0.001"
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                var inner = trimmed.Substring(1, trimmed.Length - 2);
                var parts = inner.Split('|');
                if (parts.Length == 2)
                {
                    trimmed = parts[0].Trim();
                }
            }

            // Attempt to decode raw hex string if it represents a DLMS date/time octet-string
            if (TryFormatDlmsHexOctetString(trimmed, out var formattedDate))
            {
                return formattedDate;
            }

            return trimmed;
        }

        private static bool TryFormatDlmsHexOctetString(string hexStr, out string formattedDate)
        {
            formattedDate = string.Empty;
            if (string.IsNullOrWhiteSpace(hexStr)) return false;

            var cleanedHex = hexStr.Trim();

            // DLMS date/time octet-strings are 5 to 12 bytes (10 to 24 hex characters)
            if (cleanedHex.Length < 10 || cleanedHex.Length > 24 || cleanedHex.Length % 2 != 0)
            {
                return false;
            }

            for (int i = 0; i < cleanedHex.Length; i++)
            {
                char c = cleanedHex[i];
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                {
                    return false;
                }
            }

            try
            {
                byte[] bytes = new byte[cleanedHex.Length / 2];
                for (int i = 0; i < cleanedHex.Length; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(cleanedHex.Substring(i, 2), 16);
                }

                try
                {
                    var gx = (GXDateTime)GXDLMSClient.ChangeType(bytes, DataType.DateTime);
                    if (gx != null && gx.Value.DateTime.Year > 1)
                    {
                        formattedDate = gx.Value.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
                        return true;
                    }
                }
                catch
                {
                    try
                    {
                        var gxDate = (GXDateTime)GXDLMSClient.ChangeType(bytes, DataType.Date);
                        if (gxDate != null && gxDate.Value.DateTime.Year > 1)
                        {
                            formattedDate = gxDate.Value.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
                            return true;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return false;
        }
    }
}
