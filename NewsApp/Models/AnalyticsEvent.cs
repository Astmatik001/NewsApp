using System;
using System.Collections.Generic;

namespace NewsApp.Models
{
    public class AnalyticsEvent
    {
        public string? EventName { get; set; }
        public string? UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string>? Properties { get; set; }
    }
}