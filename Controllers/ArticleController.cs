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
        protected readonly ArticleStorageService articleStorage;
        protected readonly UserService userService;
        protected ArticleController(ArticleStorageService articleStorage, UserService userService)
        {
            this.userService = userService;
            this.articleStorage = articleStorage;
        }
    }
}
