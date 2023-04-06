namespace BlogReview.Models
{
    public enum ArticleGroup
    {
        OTHERS,
        MOVIES,
        GAMES,
        BOOKS,
        TVSERIES
    }
    public class ArticleObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ArticleGroup Group { get; set; }
        public virtual List<Article> Articles { get; set; } = new();
    }
}
