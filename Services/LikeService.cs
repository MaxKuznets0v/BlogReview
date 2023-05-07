using BlogReview.Data;
using BlogReview.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogReview.Services
{
    public class LikeService
    {
        private readonly ArticleContext context;
        public LikeService(ArticleContext context) 
        {
            this.context = context;
        }
        internal async Task<AuthorLikes?> GetUserLike(Guid articleId, User user)
        {
            return await context.AuthorLikes.FirstOrDefaultAsync(
                al => al.ArticleId == articleId && al.UserId == user.Id);
        }
        internal async Task<int> GetUserTotalLikes(User user)
        {
            return (await context.AuthorLikes.Where(al => al.Article.Author == user).ToListAsync()).Count;
        }
        internal async Task UpdateLike(Guid articleId, User user, bool like)
        {
            AuthorLikes? userLike = await GetUserLike(articleId, user);
            if (userLike != null && !like)
            {
                context.AuthorLikes.Remove(userLike);
            }
            else if (userLike == null && like)
            {
                await AddLike(articleId, user);
            }
            await context.SaveChangesAsync();
        }
        private async Task AddLike(Guid articleId, User user)
        {
            await context.AuthorLikes.AddAsync(new AuthorLikes()
            {
                User = user,
                ArticleId = articleId
            });
        }
    }
}
