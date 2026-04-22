using System.Collections.Generic;
using System.Threading.Tasks;
using NewsApp.Models;

namespace NewsApp.Services
{
    public interface INewsService
    {
        Task<List<Article>> GetHeadlinesAsync(List<string> categories);
    }
}