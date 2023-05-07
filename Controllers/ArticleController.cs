using BlogReview.Data;
using BlogReview.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BlogReview.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BlogReview.Controllers
{
    abstract public class ArticleController : Controller
    {
        protected readonly ArticleStorageService articleStorage;
        protected readonly UserService userService;
        protected ArticleController(ArticleStorageService articleStorage, UserService userService)
        {
            this.userService = userService;
            this.articleStorage = articleStorage;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            Task<User?> userTask = userService.GetUser(User);
            userTask.Wait();
            if (IsUserBlocked(context, userTask.Result))
            {
                context.Result = RedirectToAction("Logout", "Account");
                return;
            }
            base.OnActionExecuting(context);
        }
        private bool IsUserBlocked(ActionExecutingContext context, User? user)
        {
            return User.Identity != null && User.Identity.IsAuthenticated &&
                context.ActionDescriptor.RouteValues["action"] != "Logout" &&
                (user == null || userService.IsUserBlocked(user));
        }
    }
}
