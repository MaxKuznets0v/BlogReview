using BlogReview.Data;
using BlogReview.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace BlogReview.Controllers
{
    public class FeedController : Controller
    {
        private readonly ArticleContext articleContext;
        private readonly UserManager<User> userManager;
        public FeedController(ArticleContext context, UserManager<User> userManager) 
        { 
            articleContext = context;
            this.userManager = userManager;
        }
        public IActionResult Index()
        {
            return View(articleContext.Articles);
        }
        [Authorize]
        public IActionResult CreateArticle(Guid id)
        {
            if (id != Guid.Empty)
            {
                Article existingEntry = articleContext.Articles.FirstOrDefault(x => x.Id == id);
                if (existingEntry == null)
                {
                    return NotFound();
                }
                return View("CreateArticle", model: existingEntry);
            }
            return View("CreateArticle");
        }
        public IActionResult Article(Guid id)
        {
            Article article = articleContext.Articles.SingleOrDefault(a => a.Id == id);
            if (article == null)
            {
                return NotFound();
            }
            return View("Article", article);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Article(Article article)
        {
            User currentUser = await GetUser(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (article.Id != Guid.Empty)
            {
                if (article.AuthorId != currentUser.Id)
                {
                    return Forbid();
                }
                Article existing = await articleContext.Articles.FirstAsync(a => a.Id == article.Id);
                existing.Title = article.Title;
                existing.Content = article.Content;
                existing.Rating = article.Rating;
                await articleContext.SaveChangesAsync();
            }
            else
            {
                articleContext.ArticleObjects.Add(article.ArticleObject);
                article.Id = Guid.NewGuid();
                article.Author = currentUser;
                articleContext.Articles.Add(article);
                await articleContext.SaveChangesAsync();
            }
            return RedirectToAction("Article", new { id = article.Id });
        }
        private async Task<User> GetUser(string userId)
        {
            return await userManager.FindByIdAsync(userId);
        }
    }
}
