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
using BlogReview.Services;
using BlogReview.ViewModels;

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
                User user = ArticleUtility.GetCurrentUser(userManager, User).Result;
                if (existingEntry == null)
                {
                    return NotFound();
                }
                else if (!ArticleUtility.IsEditAllowed(userManager, existingEntry.Author, user).Result)
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
            User user = await ArticleUtility.GetCurrentUser(userManager, User);
            if (user != null)
            {
                ArticleObjectRating rating = await ArticleUtility.GetUserRating(articleContext, article.Id, user);
                bool like = await ArticleUtility.GetUserLike(articleContext, id, user) != null;
                ViewData["ArticleObjectRating"] = (rating != null)? rating.Rating : -1;
                ViewData["AuthorLike"] = like;
                ViewData["EditAllowed"] = await ArticleUtility.IsEditAllowed(userManager, article.Author, user);
            } 
            else
            {
                ViewData["ArticleObjectRating"] = -1;
                ViewData["AuthorLike"] = false;
                ViewData["EditAllowed"] = false;
            }
            ArticleView articleView = new() 
            { 
                Article = article,
                AverageRating = await ArticleUtility.
                    GetAverageArticleObjectRating(articleContext, article.ArticleObject),
                Category = GetGroupsViewData()[(int)article.ArticleObject.Group]
            };
            ViewData["AuthorRating"] = await ArticleUtility.GetUserTotalLikes(articleContext, article.Author);
            return View("Article", articleView);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Article(Article article, string tags)
        {
            User currentUser = await ArticleUtility.GetCurrentUser(userManager, User);
            if (article.Id != Guid.Empty)
            {
                Article existing = await articleContext.Articles.FirstAsync(a => a.Id == article.Id);
                if (!ArticleUtility.IsEditAllowed(userManager, existing.Author, currentUser).Result)
                {
                    return Forbid();
                }
                await UpdateExistingArticle(article, existing, tags);
            }
            else
            {
                await CreateNewArticle(article, currentUser, tags);
            }
            await articleContext.SaveChangesAsync();
            return RedirectToAction("Article", new { id = article.Id });
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteArticle(Guid id)
        {
            Article article = await articleContext.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (id == Guid.Empty || article == null)
            {
                return NotFound();
            }
            User user = await ArticleUtility.GetCurrentUser(userManager, User);
            if (!ArticleUtility.IsEditAllowed(userManager, article.Author, user).Result)
            {
                return Forbid();
            }
            articleContext.Articles.Remove(article);
            await articleContext.SaveChangesAsync();
            return RedirectToAction("Index", "Account", new { userId = user.Id });
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RateArticleObject(Guid articleId, int rating)
        {
            if (articleId == Guid.Empty)
            {
                return NotFound();
            }
            User user = await ArticleUtility.GetCurrentUser(userManager, User);
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
            User user = await ArticleUtility.GetCurrentUser(userManager, User);
            AuthorLikes userLike = await ArticleUtility.GetUserLike(articleContext, articleId, user);
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
        
        private async Task UpdateExistingArticle(Article newData, Article existing, string tags)
        {
            existing.Title = newData.Title;
            existing.Content = newData.Content;
            existing.Rating = newData.Rating;
            await UpdateArticleTags(existing, tags);
        }
        private async Task CreateNewArticle(Article sampleArticle, User author, string tags)
        {
            await articleContext.ArticleObjects.AddAsync(sampleArticle.ArticleObject);
            sampleArticle.Author = author;
            sampleArticle.PublishDate = DateTime.Now;
            articleContext.Articles.Add(sampleArticle);
            await UpdateArticleTags(sampleArticle, tags);
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
            if (tags != null)
            {
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
            }
            return res;
        }
        private static string CapitalizeTag(string tag)
        {
            tag = tag.ToLower();
            return tag[0].ToString().ToUpper() + tag[1..];
        }
        private async Task UpdateArticleTags(Article article, string tags)
        {
            var storedTags = await articleContext.ArticleTags
                    .Where(ao => ao.Article == article).ToListAsync();
            var incomingTags = ParseTags(tags).Result;
            RemoveArticleTags(incomingTags, storedTags);
            await AddArticleTags(article, incomingTags);
            await articleContext.SaveChangesAsync();
        }
        private void RemoveArticleTags(List<Tag> incomingTags, List<ArticleTags> storedTags)
        {
            var incomingTagNames = incomingTags.Select(t => t.Name).ToHashSet();
            foreach (var storedTag in storedTags)
            {
                if (!incomingTagNames.Contains(storedTag.Tag.Name))
                {
                    articleContext.Remove(storedTag);
                }
            }
        }
        private async Task AddArticleTags(Article article, List<Tag> incomingTags)
        {
            foreach (Tag tag in incomingTags)
            {
                ArticleTags articleTag = await articleContext.ArticleTags
                    .FirstOrDefaultAsync(ao => ao.Article == article && ao.Tag == tag);
                if (articleTag == null)
                {
                    await articleContext.ArticleTags.AddAsync(new ArticleTags()
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
    }
}
