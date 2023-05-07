using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogReview.Models
{
    public class User : IdentityUser<Guid>
    {
        public virtual List<Article> Articles { get; set; } = new();
        public virtual List<ArticleObject> Ratings { get; set; } = new();
        public virtual List<Comment> Comments { get; set; } = new();
        public virtual List<ArticleObjectRating> ArticleObjectRatings { get; set; } = new();
        public virtual List<AuthorLikes> Likes { get; set; } = new();
    }
}
