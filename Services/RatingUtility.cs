using BlogReview.Data;
using BlogReview.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogReview.Services
{
    public class RatingUtility
    {
        private readonly ArticleContext context;
        public RatingUtility(ArticleContext context) 
        {
            this.context = context;
        }
        internal async Task UpdateRating(Guid articleId, User user, int rating)
        {
            ArticleObjectRating rate = await context.ArticleObjectRating.FirstOrDefaultAsync(r => (r.ArticleId == articleId && r.User == user));
            if (rate != null)
            {
                if (rate.Rating != rating)
                {
                    SetRating(rate, rating);
                }
                else
                {
                    RemoveRating(rate);
                }
            }
            else
            {
                await CreateRating(user, articleId, rating);
            }
            await context.SaveChangesAsync();
        }
        internal async Task CreateRating(User user, Guid articleId, int rating)
        {
            ArticleObjectRating rate = new()
            {
                User = user,
                ArticleId = articleId,
                Rating = rating
            };
            await context.ArticleObjectRating.AddAsync(rate);
        }
        private void RemoveRating(ArticleObjectRating rating)
        {
            context.ArticleObjectRating.Remove(rating);
        }
        private void SetRating(ArticleObjectRating rating, int newRating)
        {
            rating.Rating = newRating;
        }
        internal async Task<double> GetAverageArticleObjectRating(ArticleObject articleObject)
        {
            var query = context.ArticleObjectRating
                .Where(r => r.Article.ArticleObjectId == articleObject.Id)
                .Select(r => r.Rating);
            if (query.Any())
            {
                return await query.AverageAsync();
            }
            return 0;
        }
        internal async Task<double> GetAverageArticleObjectRating(Article article)
        {
            return await GetAverageArticleObjectRating(article.ArticleObject);
        }
        internal async Task<ArticleObjectRating> GetUserRating(Guid articleId, User user)
        {
            return await context.ArticleObjectRating
                .FirstOrDefaultAsync(a => a.ArticleId == articleId && user.Id == a.UserId);
        }
    }
}
