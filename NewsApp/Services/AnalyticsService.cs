using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NewsApp.Models;

namespace NewsApp.Services
{
    public class AnalyticsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _backendUrl;
        private readonly string _userId;

        public AnalyticsService(string backendUrl, string userId)
        {
            _backendUrl = backendUrl;
            _userId = userId;
            _httpClient = new HttpClient();
        }

        public async Task TrackEventAsync(string eventName, Dictionary<string, string> properties = null)
        {
            var evt = new AnalyticsEvent
            {
                EventName = eventName,
                UserId = _userId,
                Timestamp = DateTime.UtcNow,
                Properties = properties ?? new Dictionary<string, string>()
            };
            var json = JsonSerializer.Serialize(evt);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                await _httpClient.PostAsync(_backendUrl, content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Analytics error: {ex.Message}");
            }
        }
    }
}