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
using BlogReview.ViewModels;

namespace BlogReview.Controllers
{
    public class FeedController : ArticleController
    {
        private readonly IStringLocalizer<FeedController> localizer;
        public FeedController(ArticleContext context, UserManager<User> userManager, 
            IStringLocalizer<FeedController> localizer) : base(context, userManager)
        { 
            this.localizer = localizer;
        }
        public IActionResult Index()
        {
            return View(context.Articles);
        }
        [Authorize]
        public async Task<IActionResult> CreateArticle(Guid id)
        {
            ViewData["Groups"] = GetGroupsViewData();
            if (id != Guid.Empty)
            {
                Article existingEntry = await context.Articles.FirstOrDefaultAsync(x => x.Id == id);
                User user = GetCurrentUser().Result;
                if (existingEntry == null)
                {
                    return NotFound();
                }
                else if (!IsEditAllowed(existingEntry.Author, user).Result)
                {
                    return Forbid();
                }
                return View("CreateArticle", model: existingEntry);
            }
            return View("CreateArticle");
        }
        public async Task<IActionResult> AllTags(string query)
        {
            var tags = await context.Tags
                .Where(t => t.Name.Contains(query))
                .Select(t => new { id = t.Id, text = t.Name })
                .ToListAsync();
            return Json(tags);
        }
        public async Task<IActionResult> Article(Guid id)
        {
            Article article = await context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (article == null)
            {
                return NotFound();
            }
            User user = await GetCurrentUser();
            if (user != null)
            {
                ArticleObjectRating rating = await GetUserRating(article.Id, user);
                bool like = await GetUserLike(id, user) != null;
                ViewData["ArticleObjectRating"] = (rating != null)? rating.Rating : -1;
                ViewData["AuthorLike"] = like;
                ViewData["EditAllowed"] = await IsEditAllowed(article.Author, user);
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
                AverageRating = await GetAverageArticleObjectRating(context, article.ArticleObject),
                Category = GetGroupsViewData()[(int)article.ArticleObject.Group]
            };
            ViewData["AuthorRating"] = await GetUserTotalLikes(article.Author);
            return View("Article", articleView);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Article(Article article, string tags, Guid authorId)
        {
            User currentUser = await GetCurrentUser();
            if (article.Id != Guid.Empty)
            {
                Article existing = await context.Articles.FirstAsync(a => a.Id == article.Id);
                if (!IsEditAllowed(existing.Author, currentUser).Result)
                {
                    return Forbid();
                }
                await UpdateExistingArticle(article, existing, tags);
            }
            else
            {
                User author = currentUser;
                User requestedAuthor = await GetUserById(authorId);
                if (requestedAuthor != null && IsEditAllowed(requestedAuthor, currentUser).Result)
                {
                    author = requestedAuthor;
                }
                await CreateNewArticle(article, author, tags);
            }
            await context.SaveChangesAsync();
            return RedirectToAction("Article", new { id = article.Id });
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteArticle(Guid id)
        {
            Article article = await context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (id == Guid.Empty || article == null)
            {
                return NotFound();
            }
            User user = await GetCurrentUser();
            if (!IsEditAllowed(article.Author, user).Result)
            {
                return Forbid();
            }
            context.Articles.Remove(article);
            await context.SaveChangesAsync();
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
            User user = await GetCurrentUser();
            ArticleObjectRating rate = await context.ArticleObjectRating.FirstOrDefaultAsync(r => (r.ArticleId == articleId && r.User == user));
            if (rate != null)
            {
                rate.Rating = rating;
            }
            else
            {
                await CreateRating(user, articleId, rating);
            }
            await context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Like(Guid articleId, bool like)
        {
            if (articleId == Guid.Empty) { return NotFound(); }
            User user = await GetCurrentUser();
            AuthorLikes userLike = await GetUserLike(articleId, user);
            if (userLike != null && !like)
            {
                context.AuthorLikes.Remove(userLike);
            }
            else if (userLike == null && like)
            {
                await context.AuthorLikes.AddAsync(new AuthorLikes()
                {
                    User = user,
                    ArticleId = articleId
                });
            }
            await context.SaveChangesAsync();
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
            await context.ArticleObjects.AddAsync(sampleArticle.ArticleObject);
            sampleArticle.Author = author;
            sampleArticle.PublishDate = DateTime.Now;
            context.Articles.Add(sampleArticle);
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
                    Tag tagObject = await context.Tags.FirstOrDefaultAsync(t => t.Name == capTag);
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
            var storedTags = await context.ArticleTags
                    .Where(ao => ao.Article == article).ToListAsync();
            var incomingTags = ParseTags(tags).Result;
            RemoveArticleTags(incomingTags, storedTags);
            await AddArticleTags(article, incomingTags);
            await context.SaveChangesAsync();
        }
        private void RemoveArticleTags(List<Tag> incomingTags, List<ArticleTags> storedTags)
        {
            var incomingTagNames = incomingTags.Select(t => t.Name).ToHashSet();
            foreach (var storedTag in storedTags)
            {
                if (!incomingTagNames.Contains(storedTag.Tag.Name))
                {
                    context.Remove(storedTag);
                }
            }
        }
        private async Task AddArticleTags(Article article, List<Tag> incomingTags)
        {
            foreach (Tag tag in incomingTags)
            {
                ArticleTags articleTag = await context.ArticleTags
                    .FirstOrDefaultAsync(ao => ao.Article == article && ao.Tag == tag);
                if (articleTag == null)
                {
                    await context.ArticleTags.AddAsync(new ArticleTags()
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
            await context.ArticleObjectRating.AddAsync(rate);
        }
    }
}
