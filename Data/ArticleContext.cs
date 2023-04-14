﻿using Microsoft.EntityFrameworkCore;
using BlogReview.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BlogReview.Data
{
    public class ArticleContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleObject> ArticleObjects { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ArticleObjectRating> ArticleRatings { get; set; }

        public ArticleContext(DbContextOptions<ArticleContext> options) : base(options)
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .ConfigureWarnings(b => b.Ignore(RelationalEventId.AmbientTransactionWarning));
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Article>().ToTable(t => t.HasCheckConstraint("Rating", "Rating >= 0 AND Rating < 11"));
            modelBuilder.Entity<ArticleObject>();
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany(a => a.Comments)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ArticleObjectRating>().ToTable(t => t.HasCheckConstraint("Rating", "Rating > 0 AND Rating < 6"));
            base.OnModelCreating(modelBuilder);
        }
    }
}
