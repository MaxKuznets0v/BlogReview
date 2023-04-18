using BlogReview.Models;
namespace BlogReview.ViewModels
{
    public class ProfileView
    {
        public User Author { get; set; }
        public int Rating { get; set; }
        public List<ArticleView> Articles { get; set; }
    }
}
