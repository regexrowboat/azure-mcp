namespace AzureMcp.Areas.ApplicationInsights.Models.Query
{
    public class DistributedTraceQueryResponse
    {
        public string? id { get; set; }
        public string? operation_ParentId { get; set; }
        public string? operation_Name { get; set; }
        public string? resultCode { get; set; }
        public string? itemType { get; set; }
        public string? success { get; set; }
        public DateTimeOffset? timestamp { get; set; }
        public double? duration { get; set; }
        public string? operation_Id { get; set; }
        public string? itemId { get; set; }
        public string? problemId { get; set; }
        public string? type { get; set; }
        public string? name { get; set; }
        public string? target { get; set; }

        public static SpanSummary CreateSpanDetails(int rowId, AppLogsQueryRow<DistributedTraceQueryResponse> row)
        {
            DateTime end = row.Data.timestamp?.UtcDateTime ?? default;

            string? target = row.Data.target;
            string? problemId = row.Data.problemId;
            string? operationName = row.Data.operation_Name;
            string? name = row.Data.name;
            TimeSpan? duration = TimeSpan.FromMilliseconds(row.Data.duration ?? 0);

            var spanDetails = new SpanSummary
            {
                RowId = rowId,
                SpanId = row.Data.id,
                ParentId = row.Data.operation_ParentId,
                ItemId = row.Data.itemId,
                ResponseCode = row.Data.resultCode,
                ItemType = row.Data.itemType,
                IsSuccessful = !string.IsNullOrEmpty(row.Data.success) ? bool.Parse(row.Data.success!) : null,
                ChildSpans = new List<SpanSummary>(),
                Properties = row.OtherColumns.Select(t => new KeyValuePair<string, string>(t.Key, t.Value?.ToString()!)).ToList(),
                Name = target ?? problemId ?? operationName ?? name,
                Duration = duration,
                StartTime = end.Subtract(duration ?? TimeSpan.Zero),
                EndTime = end,
            };

            return spanDetails;
        }
    }
}
