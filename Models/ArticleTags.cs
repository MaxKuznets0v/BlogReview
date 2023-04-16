using System.ComponentModel.DataAnnotations.Schema;

namespace BlogReview.Models
{
    public class ArticleTags
    {
        public int Id { get; set; }
        [ForeignKey("Article")]
        public Guid ArticleId { get; set; }
        public virtual Article Article { get; set; }
        [ForeignKey("Tag")]
        public int TagId { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
