using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogReview.Models
{
    public class Article
    {
        [Key]
        public Guid Id { get; set; }
        [ForeignKey("Author")]
        public Guid AuthorId { get; set; }
        public virtual User Author { get; set; }
        public string Title { get; set; }
        [ForeignKey("ArticleObject")]
        public Guid ArticleObjectId { get; set; }
        public virtual ArticleObject ArticleObject { get; set; }
        public string Content { get; set; }
        public int Rating { get; set; }

        public virtual List<ArticleRating> ArticleRatings { get; set; } = new();
        public virtual List<Comment> Comments { get; set; } = new();
    }
}
