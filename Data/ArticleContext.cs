using Microsoft.EntityFrameworkCore;
using BlogReview.Models;
using Microsoft.Extensions.Options;

namespace BlogReview.Data
{
    public class ArticleContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleObject> ArticleObjects { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ArticleRating> ArticleRatings { get; set; }

        public ArticleContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>();
            modelBuilder.Entity<Article>().ToTable(t => t.HasCheckConstraint("Rating", "Rating >= 0 AND Rating < 11"));
            modelBuilder.Entity<ArticleObject>();
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany(a => a.Comments)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ArticleRating>().ToTable(t => t.HasCheckConstraint("Rating", "Rating > 0 AND Rating < 6"));
            base.OnModelCreating(modelBuilder);
        }
    }
}
