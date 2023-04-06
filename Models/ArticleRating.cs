using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogReview.Models
{
    public class ArticleRating
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Article")]
        public Guid ArticleId { get; set; }
        public virtual Article Article { get; set; }
        [Range(1, 5)]
        public int Rating { get; set; }
    }
}
