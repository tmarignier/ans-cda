using System;
using System.Globalization;

namespace CdaCrImg.Serialization
{
    /// <summary>Formats des valeurs HL7 v3 (types TS).</summary>
    public static class Hl7Format
    {
        /// <summary>Horodatage avec fuseau : <c>yyyyMMddHHmmss+zzzz</c>, ex. <c>20210108111700+0100</c>.</summary>
        public static string Timestamp(DateTimeOffset value)
        {
            var offset = value.Offset;
            var sign = offset < TimeSpan.Zero ? "-" : "+";
            offset = offset.Duration();
            return value.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
                   + sign + offset.Hours.ToString("00", CultureInfo.InvariantCulture)
                   + offset.Minutes.ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>Date seule : <c>yyyyMMdd</c>.</summary>
        public static string Date(DateTime value) => value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }
}
