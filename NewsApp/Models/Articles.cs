using System;

namespace NewsApp.Models
{
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string ContentHtml { get; set; }   // Full article HTML
        public string Url { get; set; }
        public string Category { get; set; }
        public string Source { get; set; }         // "NYTimes" or "RSS"
        public DateTime PublishDate { get; set; }
        public bool IsFavorite { get; set; }
    }
}