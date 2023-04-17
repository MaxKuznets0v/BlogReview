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
                User user = GetUser().Result;
                if (existingEntry == null)
                {
                    return NotFound();
                }
                else if (!IsEditAllowed(existingEntry.Author, user))
                {
                    return Forbid();
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
            User user = await GetUser();
            if (user != null)
            {
                ArticleObjectRating rating = await GetRating(article.Id, user);
                bool like = await GetLike(id, user) != null;
                ViewData["ArticleObjectRating"] = (rating != null)? rating.Rating : -1;
                ViewData["AuthorLike"] = like;
                ViewData["EditAllowed"] = IsEditAllowed(article.Author, user);
            } 
            else
            {
                ViewData["ArticleObjectRating"] = -1;
                ViewData["AuthorLike"] = false;
                ViewData["EditAllowed"] = false;
            }
            double average = await GetAverageRating(article.ArticleObject);
            ViewData["ArticleObjectAvgRating"] = (average > 0)? average : -1;
            ViewData["ArticleObjectGroup"] = GetGroupsViewData()[(int)article.ArticleObject.Group];
            ViewData["AuthorRating"] = await GetUserTotalLikes(article.Author);
            return View("Article", article);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Article(Article article, string tags)
        {
            User currentUser = await GetUser();
            if (article.Id != Guid.Empty)
            {
                Article existing = await articleContext.Articles.FirstAsync(a => a.Id == article.Id);
                if (!IsEditAllowed(existing.Author, currentUser))
                {
                    return Forbid();
                }
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
            await AddArticleTags(article, tags);
            await articleContext.SaveChangesAsync();
            return RedirectToAction("Article", new { id = article.Id });
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RateArticleObject(Guid articleId, int rating)
        {
            if (articleId == Guid.Empty)
            {
                return NotFound();
            }
            User user = await GetUser();
            ArticleObjectRating rate = await articleContext.ArticleObjectRating.FirstOrDefaultAsync(r => (r.ArticleId == articleId && r.User == user));
            if (rate != null)
            {
                rate.Rating = rating;
            }
            else
            {
                await CreateRating(user, articleId, rating);
            }
            await articleContext.SaveChangesAsync();
            return Ok();
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Like(Guid articleId, bool like)
        {
            if (articleId == Guid.Empty) { return NotFound(); }
            User user = await GetUser();
            AuthorLikes userLike = await GetLike(articleId, user);
            if (userLike != null && !like)
            {
                articleContext.AuthorLikes.Remove(userLike);
            }
            else if (userLike == null && like)
            {
                await articleContext.AuthorLikes.AddAsync(new AuthorLikes()
                {
                    User = user,
                    ArticleId = articleId
                });
            }
            await articleContext.SaveChangesAsync();
            return Ok();
        }
        
        private async Task<User?> GetUser()
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
        private async Task AddArticleTags(Article article, string tags)
        {
            foreach (Tag tag in await ParseTags(tags))
            {
                if (articleContext.ArticleTags
                    .FirstOrDefault(ao => ao.Article == article && ao.Tag == tag) == null)
                {
                    await articleContext.AddAsync(new ArticleTags()
                    {
                        Article = article,
                        Tag = tag
                    });
                }
            }
        }
        private async Task CreateRating(User user, Guid articleId, int rating)
        {
            ArticleObjectRating rate = new()
            {
                User = user,
                ArticleId = articleId,
                Rating = rating
            };
            await articleContext.ArticleObjectRating.AddAsync(rate);
        }
        private async Task<double> GetAverageRating(ArticleObject articleObject)
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
        private async Task<ArticleObjectRating> GetRating(Guid articleId, User user)
        {
            return await articleContext.ArticleObjectRating
                .FirstOrDefaultAsync(a => a.ArticleId == articleId && user.Id == a.UserId);
        }
        private async Task<AuthorLikes> GetLike(Guid articleId, User user)
        {
            return await articleContext.AuthorLikes
                .FirstOrDefaultAsync(al => al.ArticleId == articleId &&  al.UserId == user.Id);
        }
        private async Task<int> GetUserTotalLikes(User user)
        {
            return (await articleContext.AuthorLikes.Where(al => al.Article.Author == user).ToListAsync()).Count;
        }
        private bool IsEditAllowed(User author, User currentUser)
        {
            return userManager.IsInRoleAsync(currentUser, "Admin").Result 
                || author.Id == currentUser.Id;
        }
    }
}
