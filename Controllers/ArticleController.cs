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
        protected readonly UserUtility userUtility;
        protected ArticleController(ArticleContext context, UserManager<User> userManager)
        {
            userUtility = new UserUtility(userManager);
            articleStorage = new ArticleStorage(context);
        }
    }
}
