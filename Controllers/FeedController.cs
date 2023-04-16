using BlogReview.Data;
using BlogReview.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace BlogReview.Controllers
{
    public class FeedController : Controller
    {
        private readonly ArticleContext articleContext;
        private readonly UserManager<User> userManager;
        private readonly IStringLocalizer<FeedController> localizer;
        public FeedController(ArticleContext context, UserManager<User> userManager, 
            IStringLocalizer<FeedController> localizer) 
        { 
            articleContext = context;
            this.userManager = userManager;
            this.localizer = localizer;
        }
        public IActionResult Index()
        {
            return View(articleContext.Articles);
        }
        [Authorize]
        public async Task<IActionResult> CreateArticle(Guid id)
        {
            ViewData["Groups"] = GetGroupsViewData();
            if (id != Guid.Empty)
            {
                Article existingEntry = await articleContext.Articles.FirstOrDefaultAsync(x => x.Id == id);
                if (existingEntry == null)
                {
                    return NotFound();
                }
                return View("CreateArticle", model: existingEntry);
            }
            return View("CreateArticle");
        }
        public async Task<IActionResult> AllTags(string query)
        {
            var tags = await articleContext.Tags
                .Where(t => t.Name.Contains(query))
                .Select(t => new { id = t.Id, text = t.Name })
                .ToListAsync();
            return Json(tags);
        }
        public async Task<IActionResult> Article(Guid id)
        {
            Article article = await articleContext.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (article == null)
            {
                return NotFound();
            }
            double average = await GetAverageRating(article.ArticleObject);
            ViewData["ArticleObjectAvgRating"] = (average > 0)? average : -1;
            ViewData["ArticleObjectGroup"] = GetGroupsViewData()[(int)article.ArticleObject.Group];
            return View("Article", article);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Article(Article article, string tags)
        {
            User currentUser = await GetUser();
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
            }
            else
            {
                await articleContext.ArticleObjects.AddAsync(article.ArticleObject);
                article.Id = Guid.NewGuid();
                article.Author = currentUser;
                article.PublishDate = DateTime.Now;
                articleContext.Articles.Add(article);
            }
            article.Tags = await ParseTags(tags);
            await articleContext.SaveChangesAsync();
            return RedirectToAction("Article", new { id = article.Id });
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RateArticleObject(Guid id, int rating)
        {
            User user = await GetUser();
            ArticleObjectRating rate = await articleContext.ArticleObjectRating.FirstOrDefaultAsync(r => (r.ArticleObjectId == id && r.User == user));
            if (rate != null)
            {
                rate.Rating = rating;
            }
            else
            {
                await CreateRating(user, id, rating);
            }
            await articleContext.SaveChangesAsync();
            return Ok();
        }
        
        private async Task<User> GetUser()
        {
            return await userManager.FindByNameAsync(User.Identity.Name);
        }
        private List<string> GetGroupsViewData()
        {
            return new List<string> 
            { 
                localizer["ArticleObjectGroupMovies"],
                localizer["ArticleObjectGroupGames"],
                localizer["ArticleObjectGroupBooks"],
                localizer["ArticleObjectGroupTVseries"],
                localizer["ArticleObjectGroupOthers"]
            };
        }
        private async Task<List<Tag>> ParseTags(string tags)
        {
            List<Tag> res = new();
            foreach(string tag in tags.Split(',')) 
            {
                string capTag = CapitalizeTag(tag);
                Tag tagObject = await articleContext.Tags.FirstOrDefaultAsync(t => t.Name == capTag);
                if (tagObject != null) 
                { 
                    res.Add(tagObject);
                }
                else
                {
                    res.Add(new Tag() { Name = capTag });
                }
            }
            return res;
        }
        private static string CapitalizeTag(string tag)
        {
            tag = tag.ToLower();
            return tag[0].ToString().ToUpper() + tag[1..];
        }
        private async Task CreateRating(User user, Guid articleObjectId, int rating)
        {
            ArticleObjectRating rate = new()
            {
                User = user,
                ArticleObjectId = articleObjectId,
                Rating = rating
            };
            await articleContext.ArticleObjectRating.AddAsync(rate);
        }
        private async Task<double> GetAverageRating(ArticleObject articleObject)
        {
            double averageRating = 0;
            List<ArticleObjectRating> ratings = await articleContext.ArticleObjectRating
                .Where(r => r.ArticleObjectId == articleObject.Id).ToListAsync();
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
    }
}
