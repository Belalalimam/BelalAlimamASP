namespace NakhlaBelal.Utitlies
{
    /// <summary>
    /// Canonical unit-of-measure keys + display helpers.
    /// Keeps UnitPrice as a string (no migration) but normalizes and translates everywhere.
    /// </summary>
    public static class UnitHelper
    {
        // Canonical Arabic values — MUST match the dropdown <option value="..."> in Admin views
        public const string Meter = "متر";
        public const string Piece = "قطعة";
        public const string Kilo  = "كيلو";
        public const string Yard  = "ياردة";

        /// <summary>
        /// Normalize any stored unit string to one of the canonical values.
        /// Accepts old legacy values (m, kg, yrd, yard, piece, pc, meter, kilo, ...).
        /// </summary>
        public static string Normalize(string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return Meter;
            var u = unit.Trim().ToLowerInvariant();

            // Arabic canonicals
            if (u == Meter) return Meter;
            if (u == Piece) return Piece;
            if (u == Kilo)  return Kilo;
            if (u == Yard)  return Yard;

            // English / legacy
            if (u == "m" || u == "meter" || u == "metre" || u == "mt") return Meter;
            if (u == "pc" || u == "pcs" || u == "piece" || u == "unit") return Piece;
            if (u == "kg" || u == "kilo" || u == "kilogram" || u == "kilos") return Kilo;
            if (u == "yd" || u == "yrd" || u == "yard" || u == "yards") return Yard;

            return unit;
        }

        /// <summary> Full Arabic label, e.g. "متر" / "قطعة" / "كيلو" / "ياردة". </summary>
        public static string GetArabicLabel(string? unit)
        {
            var n = Normalize(unit);
            return n switch
            {
                Meter => "متر",
                Piece => "قطعة",
                Kilo  => "كيلو",
                Yard  => "ياردة",
                _     => n
            };
        }

        /// <summary> Short/abbreviation label for compact cards, e.g. "م" / "قطعة" / "كجم" / "ياردة". </summary>
        public static string GetShortLabel(string? unit)
        {
            var n = Normalize(unit);
            return n switch
            {
                Meter => "م",
                Piece => "قطعة",
                Kilo  => "كجم",
                Yard  => "ياردة",
                _     => n
            };
        }

        /// <summary> English abbreviation, e.g. "m" / "pc" / "kg" / "yd". </summary>
        public static string GetEnglishShort(string? unit)
        {
            var n = Normalize(unit);
            return n switch
            {
                Meter => "m",
                Piece => "pc",
                Kilo  => "kg",
                Yard  => "yd",
                _     => n
            };
        }
    }
}
