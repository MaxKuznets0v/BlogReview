namespace BlogReview.Models
{
    public enum ArticleGroup
    {
        MOVIES,
        GAMES,
        BOOKS,
        TVSERIES,
        OTHERS
    }
    public class ArticleObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ArticleGroup Group { get; set; }
        
        public virtual List<User> Users { get; set; }
        public virtual List<Article> Articles { get; set; } = new();
    }
}
