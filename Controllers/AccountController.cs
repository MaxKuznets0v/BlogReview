using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BlogReview.Data;
using BlogReview.Models;
using Microsoft.AspNetCore.Identity;
using BlogReview.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using AspNet.Security.OAuth.LinkedIn;
using Microsoft.AspNetCore.Localization;
using BlogReview.Services;

namespace BlogReview.Controllers
{
    public class AccountController : Controller
    {
        private readonly ArticleContext context;
        private readonly SignInManager<User> signInManager;
        private readonly UserManager<User> userManager;
        private const string DefaultRole = "User";

        public AccountController(ArticleContext context, UserManager<User> userManager, 
            SignInManager<User> signInManager)
        {
            this.context = context;
            this.userManager = userManager;
            this.signInManager = signInManager;
        }
        public async Task<IActionResult> Index(Guid userId)
        {
            User user;
            if (userId == Guid.Empty)
            {
                user = await ArticleUtility.GetCurrentUser(userManager, User);
            }
            else
            {
                user = await ArticleUtility.GetUserById(userManager, userId);
            }
            if (user == null)
            {
                return NotFound();
            }
            List<Article> articles = await context.Articles.Where(a => a.AuthorId == userId).ToListAsync();
            ViewData["UserName"] = user.UserName;
            ViewData["EditAllowed"] = false;
            ViewData["UserId"] = user.Id.ToString();
            ViewData["AllowedCharacters"] = userManager.Options.User.AllowedUserNameCharacters;
            if (User.Identity.IsAuthenticated)
            {
                ViewData["EditAllowed"] = await ArticleUtility.IsEditAllowed(userManager, user, 
                    await ArticleUtility.GetCurrentUser(userManager, User));
            }
            return View(articles);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SetUserName(Guid userId, string userName)
        {
            if (!ValidateUserName(userName))
            {
                return BadRequest();
            }
            User user = await ArticleUtility.GetUserById(userManager, userId);
            if (user == null)
            {
                return NotFound();
            }
            User currentUser = await ArticleUtility.GetCurrentUser(userManager, User);
            if (!ArticleUtility.IsEditAllowed(userManager, user, currentUser).Result)
            {
                return Forbid();
            }
            user.UserName = userName;
            await userManager.UpdateAsync(user);
            await UpdateNameClaim(user, userName);
            return RedirectToAction("Index", "Account", new { userId = user.Id });
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );
            return Redirect(returnUrl);
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl)
        {
            return View("Login", model: await GetLoginView(returnUrl));
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLogin(string provider, string returnUrl)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account",
                                new { ReturnUrl = returnUrl });
            var properties =
            signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            returnUrl ??= Url.Content("~/");
            var loginViewModel = await GetLoginView(returnUrl);
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (HasExternalErrors(remoteError) || HasLoadingErrors(info))
            {
                return View("Login", loginViewModel);
            }
            var signInResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider,
                info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (signInResult.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email == null)
            {
                Response.StatusCode = 400;
                return RedirectToAction("Index", "Feed");
            }
            var user = await GetUserFromLoginInfoByEmail(info, email);
            await userManager.AddLoginAsync(user, info);
            await signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            FlushCookies();
            return RedirectToAction("Index", "Feed");
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin()
        {
            return View(await userManager.Users.ToListAsync());
        }

        private bool ValidateUserName(string userName)
        {
            string allowedCharacters = userManager.Options.User.AllowedUserNameCharacters;
            return userName.All(c => allowedCharacters.Contains(c));
        }
        private async Task UpdateNameClaim(User user, string userName)
        {
            var identity = (ClaimsIdentity)User.Identity;
            identity.RemoveClaim(identity.FindFirst(ClaimTypes.Name));
            identity.AddClaim(new Claim(ClaimTypes.Name, userName));
            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, new ClaimsPrincipal(identity));
        }
        private async Task<LoginViewModel> GetLoginView(string returnUrl)
        {
            return new LoginViewModel()
            {
                ReturnUrl = returnUrl,
                ExternalLogins =
                        (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
            };
        }
        private bool HasExternalErrors(string error)
        {
            return SetModelError(error != null, $"Error from external provider: {error}");
        }
        private bool HasLoadingErrors(ExternalLoginInfo info)
        {
            return SetModelError(info == null, "Error loading external login information.");
        }
        private bool SetModelError(bool hasError, string error)
        {
            if (hasError)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return hasError;
        }
        private async Task<User> SignUser(ExternalLoginInfo info)
        {
            var user = new User
            {
                UserName = info.Principal.FindFirstValue(ClaimTypes.Name),
                Email = info.Principal.FindFirstValue(ClaimTypes.Email)
            };
            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                user.UserName = user.Email;
                await userManager.CreateAsync(user);
            }
            await userManager.AddToRoleAsync(user, DefaultRole);
            return user;
        }
        private async Task<User> GetUserFromLoginInfoByEmail(ExternalLoginInfo info, string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            user ??= await SignUser(info);
            return user;
        }
        private async void FlushCookies()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    }
}
