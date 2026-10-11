using System;
using System.Collections.Generic;

namespace FUNews.Client.DataAccess.Models;

public class OfflineDataSnapshot
{
    public int Version { get; set; } = 1;
    public DateTime RefreshedAt { get; set; } = DateTime.UtcNow;
    public List<CategoryApiModel> Categories { get; set; } = new();
    public List<TagApiModel> Tags { get; set; } = new();
    public List<NewsArticleApiModel> Articles { get; set; } = new();
}
