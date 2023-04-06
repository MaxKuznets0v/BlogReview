using BlogReview.Data;
using BlogReview.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace BlogReview.Controllers
{
    public class FeedController : Controller
    {
        private readonly ArticleContext articleContext;
        public FeedController(ArticleContext context) 
        { 
            articleContext = context;
        }
        public IActionResult Index()
        {
            return View(articleContext.Articles);
        }
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
            return View("Article", article);
        }
        [HttpPost]
        public async Task<IActionResult> Article(Article article)
        {
            if (article.Id != Guid.Empty)
            {
                Article existing = await articleContext.Articles.FirstAsync(a => a.Id == article.Id);
                existing.Title = article.Title;
                existing.Content = article.Content;
                existing.Rating = article.Rating;
                await articleContext.SaveChangesAsync();
            }
            else
            {
                //User baseUser = new User() { Id = Guid.NewGuid(), Name = "ADMIN" };
                //articleContext.Users.Add(baseUser);
                User baseUser = await articleContext.Users.FirstAsync();
                article.ArticleObject.Id = Guid.NewGuid();
                articleContext.ArticleObjects.Add(article.ArticleObject);
                article.Id = Guid.NewGuid();
                article.Author = baseUser;
                articleContext.Articles.Add(article);
                await articleContext.SaveChangesAsync();
            }
            return RedirectToAction("Article", new { id = article.Id });
        }
    }
}
