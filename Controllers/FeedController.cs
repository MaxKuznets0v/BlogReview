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
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using BlogReview.Services;
using System;
using NuGet.Protocol;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Azure;

namespace BlogReview.Controllers
{
    public enum SearchMode 
    { 
        Default,
        Tag,
        ArticleObject
    }
    public class FeedController : ArticleController
    {
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer<FeedController> localizer;
        private readonly ImageStorage imageStorage;
        public FeedController(ArticleContext context, UserManager<User> userManager, 
            IStringLocalizer<FeedController> localizer, IConfiguration configuration) : base(context, userManager)
        { 
            this.configuration = configuration;
            this.localizer = localizer;
            var config = configuration.GetSection("ImageCloud:Cloudinary");
            imageStorage = new ImageStorage(new Account(config["CloudName"], config["Key"], config["Secret"]));
        }
        public async Task<IActionResult> Index(int pageSize = 9)
        {
            var views = GetArticleViews(articleStorage.GetArticlesByPage(1, pageSize));
            return View(views);
        }
        public async Task<IActionResult> LoadPage(int page, int pageSize = 9)
        {
            var views = GetArticleViews(articleStorage.GetArticlesByPage(page, pageSize));
            return PartialView("_ArticleList", views);
        }
        [Authorize]
        public async Task<IActionResult> CreateArticle(Guid id)
        {
            ViewData["Groups"] = GetGroupsViewData();
            if (id != Guid.Empty)
            {
                Article existingEntry = await articleStorage.GetArticleById(id);
                User user = await GetCurrentUser();
                if (existingEntry == null)
                {
                    return NotFound();
                }
                else if (!IsEditAllowed(existingEntry.Author, user).Result)
                {
                    return Forbid();
                }
                ArticleView view = await CreateArticleView(existingEntry);
                return View("CreateArticle", model: view);
            }
            return View("CreateArticle");
        }
        public async Task<IActionResult> AllTags(string query)
        {
            var tags = (await articleStorage.tagUtility.GetSimilarTags(query))
                .Select(t => new { id = t.Id, text = t.Name })
                .ToList();
            return Json(tags);
        }
        public async Task<IActionResult> ArticleObject(string name)
        {
            var objects = (await articleStorage.GetSimilarArticleObject(name))
                .Select(o => new { id = o.Id, text = o.Name, group = o.Group })
                .ToList();
            return Json(objects);
        }
        public async Task<IActionResult> Article(Guid id)
        {
            Article article = await articleStorage.GetArticleById(id);
            if (article == null)
            {
                return NotFound();
            }
            User user = await GetCurrentUser();
            if (user != null)
            {
                ArticleObjectRating rating = await articleStorage.ratingUtility.GetUserRating(article.Id, user);
                bool like = await articleStorage.likeUtility.GetUserLike(id, user) != null;
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
            ArticleView articleView = await CreateArticleView(article);
            ViewData["AuthorRating"] = await articleStorage.likeUtility.GetUserTotalLikes(article.Author);
            return View("Article", articleView);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Article(Article article, string tags, Guid authorId, List<IFormFile> newImages, List<string> oldImages)
        {
            User currentUser = await GetCurrentUser();
            if (article.Id != Guid.Empty)
            {
                Article existing = await articleStorage.GetArticleById(article.Id);
                if (!IsEditAllowed(existing.Author, currentUser).Result)
                {
                    return Forbid();
                }
                if (newImages.Count > 0)
                {
                    await UpdateNewImages(existing, newImages);
                } 
                else 
                {
                    await UpdateExistingImages(existing, oldImages);
                }
                await articleStorage.UpdateExistingArticle(article, existing, tags);
            }
            else
            {
                User author = await GetNewArticleAuthor(currentUser, authorId);
                await UpdateNewImages(article, newImages);
                await articleStorage.CreateNewArticle(article, author, tags);
            }
            return RedirectToAction("Article", new { id = article.Id });
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteArticle(Guid id, Guid profileId)
        {
            Article article = await articleStorage.GetArticleById(id);
            if (id == Guid.Empty || article == null)
            {
                return NotFound();
            }
            User user = await GetCurrentUser();
            if (!IsEditAllowed(article.Author, user).Result)
            {
                return Forbid();
            }
            await articleStorage.DeleteArticle(article);
            return RedirectToAction("Index", "Account", new { userId = (profileId == Guid.Empty)? user.Id : profileId });
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
            await articleStorage.ratingUtility.UpdateRating(articleId, user, rating);
            return Ok();
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Like(Guid articleId, bool like)
        {
            if (articleId == Guid.Empty) { return NotFound(); }
            User user = await GetCurrentUser();
            await articleStorage.likeUtility.UpdateLike(articleId, user, like);
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> Search(string query, SearchMode mode, int pageSize = 9)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Empty query provided!");
            }
            ViewData["Query"] = query;
            List<Article> res = mode switch
            {
                SearchMode.Tag => await articleStorage.SearchArticlesWithTagPage(query, 1, pageSize),
                SearchMode.ArticleObject => await articleStorage.SearchArticlesWithArticleObjectPage(query, 1, pageSize),
                _ => await articleStorage.FullTextSearchPage(query, 1, pageSize),
            };
            return View("SearchResults", new ArticlePaginate() { 
                ArticleViews = GetArticleViews(res), 
                PageLoadUrl = Url.Action("SearchLoadPage", "Feed")
            });
        }
        [HttpGet]
        public async Task<IActionResult> SearchLoadPage(string query, SearchMode mode, int page, int pageSize = 9)
        {
            List<Article> res = mode switch
            {
                SearchMode.Tag => await articleStorage.SearchArticlesWithTagPage(query, page, pageSize),
                SearchMode.ArticleObject => await articleStorage.SearchArticlesWithArticleObjectPage(query, page, pageSize),
                _ => await articleStorage.FullTextSearchPage(query, page, pageSize),
            };
            return PartialView("_ArticleList", GetArticleViews(res));
        }
        [HttpGet]
        public IActionResult TagCounts()
        {
            return Json(articleStorage.tagUtility.GetTagCounts());
;        }
        [HttpGet]
        public async Task<IActionResult> HighestRatingArticles(int count)
        {
            var articles = await articleStorage.GetHighestRatingArticles(count);
            return PartialView("_HighestRatingArticles", GetArticleViews(articles));
        }
        private async Task<User> GetNewArticleAuthor(User currentUser, Guid requestedId)
        {
            User author = currentUser;
            User? requestedAuthor = await GetUserById(requestedId);
            if (requestedAuthor != null && IsEditAllowed(requestedAuthor, currentUser).Result)
            {
                author = requestedAuthor;
            }
            return author;
        }
        private async Task UpdateNewImages(Article article, List<IFormFile> images)
        {
            foreach (var image in images)
            {
                if (article.ImagePublicId != null)
                {
                    await imageStorage.DeleteImage(article.ImagePublicId);
                }
                var res = await imageStorage.UploadImage(image);
                article.ImagePublicId = res.PublicId;
                break;
            }
        }
        private async Task UpdateExistingImages(Article article, List<string> publicIds)
        {
            if (publicIds.Count == 0 && article.ImagePublicId != null)
            {
                await imageStorage.DeleteImage(article.ImagePublicId);
                article.ImagePublicId = null;
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
        private async Task<ArticleView> CreateArticleView(Article article)
        {
            ArticleView view =  new()
            {
                Article = article,
                AverageRating = await articleStorage.ratingUtility.GetAverageArticleObjectRating(article.ArticleObject),
                Category = GetGroupsViewData()[(int)article.ArticleObject.Group],
                Tags = article.Tags.Select(t => t.Tag.Name).ToList(),
                AuthorRating = await articleStorage.likeUtility.GetUserTotalLikes(article.Author),
                ImageUrl = ""
            };
            if (article.ImagePublicId != null)
            {
                view.ImageUrl = imageStorage.GetUrlById(article.ImagePublicId);
            }
            return view;
        }
        private List<ArticleView> GetArticleViews(List<Article> articles)
        {
            return articles.Select(async a => await CreateArticleView(a))
                .Select(a => a.Result).ToList();
        }
    }
}
