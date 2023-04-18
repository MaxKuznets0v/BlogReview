using BlogReview.Data;
using BlogReview.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlogReview.Controllers
{
    abstract public class ArticleController : Controller
    {
        protected readonly ArticleContext context;
        protected readonly UserManager<User> userManager;
        protected ArticleController(ArticleContext context, UserManager<User> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }
        protected async Task<User?> GetCurrentUser()
        {
            if (User.Identity.IsAuthenticated)
            {
                return await userManager.FindByNameAsync(User.Identity.Name);
            }
            else
            {
                return null;
            }
        }
        protected async Task<User?> GetUserById(Guid id)
        {
            return await userManager.FindByIdAsync(id.ToString());
        }
        protected async Task<bool> IsEditAllowed(User author, User currentUser)
        {
            return await userManager.IsInRoleAsync(currentUser, "Admin")
                || author.Id == currentUser.Id;
        }
        protected async Task<double> GetAverageArticleObjectRating(ArticleObject articleObject)
        {
            double averageRating = 0;
            List<ArticleObjectRating> ratings = await context.ArticleObjectRating
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
        protected async Task<double> GetAverageArticleObjectRating(Article article)
        {
            return await GetAverageArticleObjectRating(article.ArticleObject);
        }
        protected async Task<ArticleObjectRating> GetUserRating(Guid articleId, User user)
        {
            return await context.ArticleObjectRating
                .FirstOrDefaultAsync(a => a.ArticleId == articleId && user.Id == a.UserId);
        }
        protected async Task<AuthorLikes> GetUserLike(Guid articleId, User user)
        {
            return await context.AuthorLikes.FirstOrDefaultAsync(
                al => al.ArticleId == articleId && al.UserId == user.Id);
        }
        protected async Task<int> GetUserTotalLikes(User user)
        {
            return (await context.AuthorLikes.Where(al => al.Article.Author == user).ToListAsync()).Count;
        }
    }
}
