using Microsoft.EntityFrameworkCore;

namespace BlogReview.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual List<ArticleTags> Articles { get; set; } = new();
    }
}
