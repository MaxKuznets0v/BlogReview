using System.ComponentModel.DataAnnotations.Schema;

namespace BlogReview.Models
{
    public class Comment
    {
        public int Id { get; set; }
        [ForeignKey("Author")]
        public Guid AuthorId { get; set; }
        public virtual User Author { get; set; }
        [ForeignKey("Article")]
        public Guid ArticleId { get; set; }
        public virtual Article Article { get; set; }
        public string Content { get; set; }
    }
}
