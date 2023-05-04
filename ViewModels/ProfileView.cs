using BlogReview.Models;
namespace BlogReview.ViewModels
{
    public class ProfileView
    {
        public User Author { get; set; }
        public int Rating { get; set; }
        public List<ArticleView> Articles { get; set; }
        public bool IsEditAllowed { get; set; }
        public string UsernameAllowedChars { get; set; }
    }
}
