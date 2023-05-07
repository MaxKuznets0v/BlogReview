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
            if (context.ActionDescriptor.RouteValues["action"] != "Logout" && 
                (IsUserBlocked(userTask.Result) || IsUserRoleChanged(userTask.Result)))
            {
                context.Result = RedirectToAction("Logout", "Account");
                return;
            }
            base.OnActionExecuting(context);
        }
        private bool IsUserBlocked(User? user)
        {
            return User.Identity != null && User.Identity.IsAuthenticated &&
                (user == null || userService.IsUserBlocked(user));
        }
        private bool IsUserRoleChanged(User? user)
        {
            if (user == null)
            {
                return false;
            }
            var dbRolesTask = userService.GetUserRoles(user);
            dbRolesTask.Wait();
            var currentRoles = new List<string>(User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value));
            return !Enumerable.SequenceEqual(dbRolesTask.Result.ToList().OrderBy(r => r), currentRoles.OrderBy(r => r));
        }
    }
}
