using BlogReview.Models;
namespace BlogReview.ViewModels
{
    public class ArticleView
    {
        const int maxRating = 5;
        public Article Article { get; set; }
        public int AuthorRating { get; set; }
        public double AverageRating { get; set; }
        public string Category { get; set; }
        public List<string> ImageUrls { get; set; }
        public List<string> Tags { get; set; }
        public string DisplayRating(string defaultValue)
        {
            if (AverageRating <= 0) 
            {
                return defaultValue;
            }
            return AverageRating.ToString("0.00") + "/" + maxRating;
        }
    }
}
