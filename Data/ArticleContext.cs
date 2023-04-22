﻿using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql;
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
        public DbSet<ArticleObjectRating> ArticleObjectRating { get; set; }
        public DbSet<AuthorLikes> AuthorLikes { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ArticleTags> ArticleTags { get; set; }

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
            modelBuilder.Entity<Tag>().HasIndex(t => t.Name)
                .HasDatabaseName("TagFullTextIndex")
                .IsFullText();
            modelBuilder.Entity<ArticleTags>();
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany(a => a.Comments)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ArticleObjectRating>()
                .HasOne(aor => aor.User)
                .WithMany(u => u.ArticleObjectRatings)
                .HasForeignKey(aor => aor.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ArticleObjectRating>()
                .HasOne(aor => aor.Article)
                .WithMany(ao => ao.UserRatings)
                .HasForeignKey(aor => aor.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ArticleObjectRating>().ToTable(t => t.HasCheckConstraint("Rating", "Rating > 0 AND Rating < 6"));
            modelBuilder.Entity<AuthorLikes>()
                .HasOne(al => al.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AuthorLikes>()
                .HasOne(al => al.Article)
                .WithMany(a => a.Likes)
                .HasForeignKey(al => al.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<IdentityUserRole<Guid>>().HasKey(r => new { r.UserId, r.RoleId });

            modelBuilder.Entity<Article>().HasIndex(a => new { a.Title, a.Content })
                .HasDatabaseName("ArticleFullTextIndex")
                .IsFullText();
            modelBuilder.Entity<Comment>().HasIndex(c => c.Content)
                .HasDatabaseName("CommentFullTextIndex")
                .IsFullText();
            modelBuilder.Entity<ArticleObject>().HasIndex(ao => ao.Name)
                .HasDatabaseName("ArticleObjectFullTextIndex")
                .IsFullText();
            modelBuilder.Entity<User>().HasIndex(a => a.UserName)
                .HasDatabaseName("UserNameFullTextIndex")
                .IsFullText();
            base.OnModelCreating(modelBuilder);
        }
    }
}
