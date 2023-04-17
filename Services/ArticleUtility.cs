using BlogReview.Data;
using BlogReview.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogReview.Services
{
    static public class ArticleUtility
    {
        static public async Task<User?> GetCurrentUser(UserManager<User> userManager, ClaimsPrincipal principal)
        {
            if (principal.Identity.IsAuthenticated)
            {
                return await userManager.FindByNameAsync(principal.Identity.Name);
            }
            else
            {
                return null;
            }
        }
        static public async Task<User?> GetUserById(UserManager<User> userManager, Guid id)
        {
            return await userManager.FindByIdAsync(id.ToString());
        }
        static public async Task<bool> IsEditAllowed(UserManager<User> userManager, User author, User currentUser)
        {
            return await userManager.IsInRoleAsync(currentUser, "Admin")
                || author.Id == currentUser.Id;
        }
        static public async Task<double> GetAverageArticleObjectRating(ArticleContext articleContext, ArticleObject articleObject)
        {
            double averageRating = 0;
            List<ArticleObjectRating> ratings = await articleContext.ArticleObjectRating
                .Where(r => r.Article.ArticleObjectId == articleObject.Id).ToListAsync();
            if (ratings.Count == 0)
            {
                return 0;
            }
            foreach (var rating in ratings)
            {
                averageRating += rating.Rating;
            }
            return averageRating / ratings.Count;
        }
        static public async Task<double> GetAverageArticleObjectRating(ArticleContext articleContext, Article article)
        {
            return await GetAverageArticleObjectRating(articleContext, article.ArticleObject);
        }
        static public async Task<ArticleObjectRating> GetUserRating(ArticleContext articleContext, Guid articleId, User user)
        {
            return await articleContext.ArticleObjectRating
                .FirstOrDefaultAsync(a => a.ArticleId == articleId && user.Id == a.UserId);
        }
        static public async Task<AuthorLikes> GetUserLike(ArticleContext articleContext, Guid articleId, User user)
        {
            return await articleContext.AuthorLikes
                .FirstOrDefaultAsync(al => al.ArticleId == articleId && al.UserId == user.Id);
        }
        static public async Task<int> GetUserTotalLikes(ArticleContext articleContext, User user)
        {
            return (await articleContext.AuthorLikes.Where(al => al.Article.Author == user).ToListAsync()).Count;
        }
    }
}
