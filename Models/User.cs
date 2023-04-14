using Microsoft.AspNetCore.Identity;

namespace BlogReview.Models
{
    public class User : IdentityUser<Guid>
    {
        public virtual List<Article> Articles { get; set; } = new();
        public virtual List<ArticleObject> Ratings { get; set; } = new();
        public virtual List<Comment> Comments { get; set; } = new();
        public virtual ICollection<IdentityUserRole<Guid>> Roles { get; set; }
    }
}
