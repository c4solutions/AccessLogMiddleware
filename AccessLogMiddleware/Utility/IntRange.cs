using System.Linq;

namespace AccessLogMiddleware.Utility
{
    // Original code from: https://stackoverflow.com/a/25334542/7828970
    internal static class IntRange
    {
        public static int[] ParseRange(string ranges)
        {
            string[] groups = ranges.Split(',');
            return groups.SelectMany(t => GetRangeNumbers(t)).ToArray();
        }

        public static int[] GetRangeNumbers(string range)
        {
            int[] RangeNums = range
                .Split('-')
                .Select(t => new string(t.Where(char.IsDigit).ToArray())) // Digits Only
                .Where(t => !string.IsNullOrWhiteSpace(t)) // Only if has a value
                .Select(t => int.Parse(t)) // digit to int
                .ToArray();
            return RangeNums.Length.Equals(2) ? Enumerable.Range(RangeNums.Min(), (RangeNums.Max() + 1) - RangeNums.Min()).ToArray() : RangeNums;
        }
    }
}