namespace Infrastructure.JobSources
{
    internal static class QueryString
    {
        // "?a=1&b=2", skipping empty values.
        public static string From(Dictionary<string, string?> values)
        {
            var pairs = values
                .Where(v => !string.IsNullOrWhiteSpace(v.Value))
                .Select(v => $"{Uri.EscapeDataString(v.Key)}={Uri.EscapeDataString(v.Value!)}");
            return "?" + string.Join("&", pairs);
        }
    }
}
