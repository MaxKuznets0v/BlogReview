using BlogReview.Data;
using Microsoft.EntityFrameworkCore;
using BlogReview.Models;

namespace BlogReview.Services
{
    public class ArticleStorage
    {
        private readonly ArticleContext context;
        public readonly RatingUtility ratingUtility;
        public readonly TagUtility tagUtility;
        public readonly LikeUtility likeUtility;

        public ArticleStorage(ArticleContext context) 
        { 
            this.context = context;
            ratingUtility = new RatingUtility(context);
            tagUtility = new TagUtility(context);
            likeUtility = new LikeUtility(context);
        }
        public DbSet<Article> GetAllArticles()
        {
            return context.Articles;
        }
        public async Task<Article> GetArticleById(Guid id)
        {
            return await context.Articles.FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task UpdateExistingArticle(Article newData, Article existing, string tags)
        {
            existing.Title = newData.Title;
            existing.Content = newData.Content;
            existing.Rating = newData.Rating;
            await tagUtility.UpdateArticleTags(existing, tags);
            await context.SaveChangesAsync();
        }
        public async Task CreateNewArticle(Article sampleArticle, User author, string tags)
        {
            await context.ArticleObjects.AddAsync(sampleArticle.ArticleObject);
            sampleArticle.Author = author;
            sampleArticle.PublishDate = DateTime.Now;
            context.Articles.Add(sampleArticle);
            await tagUtility.UpdateArticleTags(sampleArticle, tags);
            await context.SaveChangesAsync();
        }
        public async Task DeleteArticle(Article article)
        {
            context.Articles.Remove(article);
            await context.SaveChangesAsync();
        }
    }
}
