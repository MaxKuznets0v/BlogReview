using BlogReview.Data;
using Microsoft.EntityFrameworkCore;
using BlogReview.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using Pomelo.EntityFrameworkCore.MySql.Extensions;
using Pomelo.EntityFrameworkCore.MySql.Query.ExpressionVisitors.Internal;
using System.Text.RegularExpressions;
using System.Drawing.Printing;
using Azure;

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
            return LoadPage(context.Articles.OrderByDescending(a => a.PublishTime), 
                pageNumber, pageSize);
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
        public async Task<List<Article>> FullTextSearchPage(string query, int pageNumber, int pageSize)
        {
            query = FilterQuery(query);
            return LoadPage(context.Articles
                .Where(a => EF.Functions.Match(new[] { a.Title, a.Content }, query, MySqlMatchSearchMode.Boolean)
                || a.Tags.Any(t => EF.Functions.Match(t.Tag.Name, query, MySqlMatchSearchMode.Boolean))
                || a.Comments.Any(c => EF.Functions.Match(c.Content, query, MySqlMatchSearchMode.Boolean)
                || EF.Functions.Match(c.Author.UserName, query, MySqlMatchSearchMode.Boolean))
                || EF.Functions.Match(a.ArticleObject.Name, query, MySqlMatchSearchMode.Boolean)
                || EF.Functions.Match(a.Author.UserName, query, MySqlMatchSearchMode.Boolean)), pageNumber, pageSize);
        }
        public async Task<List<Article>> SearchArticlesWithTagPage(string tagName, int pageNumber, int pageSize)
        {
            Tag tag = await context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
            if (tag == null)
            {
                return new();
            }
            return LoadPage(tag.Articles.Select(t => t.Article).AsQueryable(), pageNumber, pageSize);
        }
        public async Task<List<Article>> SearchArticlesWithArticleObjectPage(string name, int pageNumber, int pageSize)
        {
            ArticleObject obj = await context.ArticleObjects.FirstOrDefaultAsync(o => o.Name == name);
            if (obj == null)
            {
                return new();
            }
            return LoadPage(obj.Articles.AsQueryable(), pageNumber, pageSize);
        }
        public async Task<List<ArticleObject>> GetSimilarArticleObject(string name)
        {
            return await context.ArticleObjects
                .Where(o => o.Name.StartsWith(name))
                .ToListAsync();
        }
        private List<Article> LoadPage(IQueryable<Article> articles, int pageNumber, int pageSize) 
        {
            return articles.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
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
