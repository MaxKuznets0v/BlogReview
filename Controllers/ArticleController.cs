using BlogReview.Data;
using BlogReview.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BlogReview.Services;

namespace BlogReview.Controllers
{
    abstract public class ArticleController : Controller
    {
        protected readonly ArticleStorage articleStorage;
        protected readonly UserManager<User> userManager;
        protected ArticleController(ArticleContext context, UserManager<User> userManager)
        {
            articleStorage = new ArticleStorage(context);
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
    }
}
