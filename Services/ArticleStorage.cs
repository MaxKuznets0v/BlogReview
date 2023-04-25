using BlogReview.Data;
using Microsoft.EntityFrameworkCore;
using BlogReview.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using Pomelo.EntityFrameworkCore.MySql.Extensions;
using Pomelo.EntityFrameworkCore.MySql.Query.ExpressionVisitors.Internal;
using System.Text.RegularExpressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BlogReview.Services
{
    public class ArticleStorage
    {
        private readonly ArticleContext context;
        public readonly RatingUtility ratingUtility;
        public readonly TagUtility tagUtility;
        public readonly LikeUtility likeUtility;
        public readonly CommentUtility commentUtility;

        public ArticleStorage(ArticleContext context) 
        { 
            this.context = context;
            ratingUtility = new RatingUtility(context);
            tagUtility = new TagUtility(context);
            likeUtility = new LikeUtility(context);
            commentUtility = new CommentUtility(context);
        }
        public List<Article> GetArticlesByPage(int pageNumber, int pageSize) 
        {
            return context.Articles
                .OrderByDescending(a => a.PublishTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        public List<Article> GetAllArticles()
        {
            return context.Articles.ToList();
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
            await SetArticleObject(newData);
            existing.ArticleObject = newData.ArticleObject;
            await tagUtility.UpdateArticleTags(existing, tags);
            await context.SaveChangesAsync();
        }
        public async Task CreateNewArticle(Article sampleArticle, User author, string tags)
        {
            await SetArticleObject(sampleArticle);
            sampleArticle.Author = author;
            sampleArticle.PublishTime = DateTime.Now;
            context.Articles.Add(sampleArticle);
            await tagUtility.UpdateArticleTags(sampleArticle, tags);
            await context.SaveChangesAsync();
        }
        public async Task DeleteArticle(Article article)
        {
            context.Articles.Remove(article);
            await context.SaveChangesAsync();
        }
        public async Task<List<Article>> GetHighestRatingArticles(int limit)
        {
            return await context.Articles.OrderByDescending(a => a.Rating).Take(limit).ToListAsync();
        }
        public async Task<List<Article>> FullTextSearch(string query)
        {
            query = FilterQuery(query);
            return await context.Articles
                .Where(a => EF.Functions.Match(new[] { a.Title, a.Content }, query, MySqlMatchSearchMode.Boolean)
                || a.Tags.Any(t => EF.Functions.Match(t.Tag.Name, query, MySqlMatchSearchMode.Boolean))
                || a.Comments.Any(c => EF.Functions.Match(c.Content, query, MySqlMatchSearchMode.Boolean)
                || EF.Functions.Match(c.Author.UserName, query, MySqlMatchSearchMode.Boolean))
                || EF.Functions.Match(a.ArticleObject.Name, query, MySqlMatchSearchMode.Boolean)
                || EF.Functions.Match(a.Author.UserName, query, MySqlMatchSearchMode.Boolean)).ToListAsync();
        }
        public async Task<List<Article>> FullTextSearchWithTag(string tag)
        {
            return await context.Articles
                .Where(a => a.Tags.Any(
                    t => EF.Functions.Match(t.Tag.Name, tag, MySqlMatchSearchMode.Boolean)))
                .ToListAsync();
        }
        public async Task<List<ArticleObject>> GetSimilarArticleObject(string name)
        {
            return await context.ArticleObjects
                .Where(o => o.Name.StartsWith(name))
                .ToListAsync();
        }
        private async Task SetArticleObject(Article article)
        {
            ArticleObject articleObject = await context.ArticleObjects.FirstOrDefaultAsync(o => o.Id == article.ArticleObject.Id);
            if (articleObject == null)
            {
                await context.ArticleObjects.AddAsync(article.ArticleObject);
                await context.SaveChangesAsync();
            }
            else
            {
                article.ArticleObject = articleObject;
            }
        }
        private static string FilterQuery(string q)
        {
            string res = Regex.Replace(q.Trim(), @"[^\w\s]", "");
            res = Regex.Replace(res.Trim(), @"\s", "* ");
            if (!string.IsNullOrWhiteSpace(res))
            {
                res += "*";
            }
            return res;
        }
    }
}
