using System.ComponentModel.DataAnnotations.Schema;

namespace BlogReview.Models
{
    public class AuthorLikes
    {
        public int Id { get; set; }
        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
        [ForeignKey("Article")]
        public Guid ArticleId { get; set; }
        public virtual Article Article { get; set; }
    }
}
