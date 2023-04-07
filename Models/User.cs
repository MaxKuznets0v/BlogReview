namespace BlogReview.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public virtual List<Article> Articles { get; set; } = new();
        public virtual List<ArticleObject> Ratings { get; set; } = new();
        public virtual List<Comment> Comments { get; set; } = new();
    }
}
