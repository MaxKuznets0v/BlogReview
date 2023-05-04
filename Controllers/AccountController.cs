﻿using System;
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
using Microsoft.AspNetCore.Localization;
using System.Numerics;

namespace BlogReview.Controllers
{
    public class AccountController : ArticleController
    {
        private readonly SignInManager<User> signInManager;
        private const string DefaultRole = "User";
        private const int BlockYears = 100;

        public AccountController(ArticleContext context, UserManager<User> userManager, 
            SignInManager<User> signInManager) : base(context, userManager)
        {
            this.signInManager = signInManager;
        }
        public async Task<IActionResult> Index(Guid userId)
        {
            User? user;
            User? currentUser = await GetCurrentUser();
            if (userId == Guid.Empty)
            {
                user = currentUser;
            }
            else
            {
                user = await GetUserById(userId);
            }
            if (user == null)
            {
                return NotFound();
            }
            return View(await GetProfileView(user, currentUser));
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SetUserName(Guid userId, string userName)
        {
            if (!ValidateUserName(userName))
            {
                return BadRequest();
            }
            User user = await GetUserById(userId);
            if (user == null)
            {
                return NotFound();
            }
            User currentUser = await GetCurrentUser();
            if (!IsEditAllowed(user, currentUser).Result)
            {
                return Forbid();
            }
            user.UserName = userName;
            await userManager.UpdateAsync(user);
            await UpdateNameClaim(currentUser == user, userName);
            return RedirectToAction("Index", "Account", new { userId = user.Id });
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult SetLanguage(string culture, string returnUrl)
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
            if (user.LockoutEnd != null)
            {
                return RedirectToAction("Login", "Account", new { blocked = true });
            }
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
            ViewData["userId"] = (await GetCurrentUser()).Id.ToString();
            return View(await userManager.Users.ToListAsync());
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BlockUser(Guid id)
        {
            User? currentUser = await GetCurrentUser();
            User? userToBlock = await GetUserById(id);
            if (currentUser == null || userToBlock == null) 
            {
                return NotFound();
            }
            else if (await IsAbleToEditUser(currentUser, userToBlock))
            {
                userToBlock.LockoutEnd = DateTime.UtcNow.AddYears(BlockYears);
                await userManager.UpdateAsync(userToBlock);
                return RedirectToAction("Admin");
            }
            else
            {
                return Forbid();
            }
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnBlockUser(Guid id)
        {
            User? userToUnBlock = await GetUserById(id);
            User? currentUser = await GetCurrentUser();
            if (currentUser == null || userToUnBlock == null)
            {
                return NotFound();
            }
            else if (await IsAbleToEditUser(currentUser, userToUnBlock))
            {
                userToUnBlock.LockoutEnd = null;
                await userManager.UpdateAsync(userToUnBlock);
                return RedirectToAction("Admin");
            }
            else
            {
                return Forbid();
            }
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            User? userToDelete = await GetUserById(id);
            User? currentUser = await GetCurrentUser();
            if (currentUser == null || userToDelete == null)
            {
                return NotFound();
            } 
            else if (await IsAbleToEditUser(currentUser, userToDelete))
            {
                await userManager.DeleteAsync(userToDelete);
                return RedirectToAction("Admin");
            } 
            else
            {
                return Forbid();
            }
        }
        [HttpPost]
        [Authorize(Roles = "MasterAdmin")]
        public async Task<IActionResult> SetUserAdmin(Guid id)
        {
            User? user = await GetUserById(id);
            User? currentUser = await GetCurrentUser();
            if (currentUser == null || user == null)
            {
                return NotFound();
            }
            else if (await userManager.IsInRoleAsync(user, "User"))
            {
                await userManager.RemoveFromRoleAsync(user, "User");
                await userManager.AddToRoleAsync(user, "Admin");
            }
            return RedirectToAction("Admin");
        }
        [HttpPost]
        [Authorize(Roles = "MasterAdmin")]
        public async Task<IActionResult> RemoveUserAdmin(Guid id)
        {
            User? user = await GetUserById(id);
            User? currentUser = await GetCurrentUser();
            if (currentUser == null || user == null)
            {
                return NotFound();
            }
            else if (await userManager.IsInRoleAsync(user, "Admin") &&
                !await userManager.IsInRoleAsync(user, "MasterAdmin"))
            {
                await userManager.RemoveFromRoleAsync(user, "Admin");
                await userManager.AddToRoleAsync(user, "User");
            }
            return RedirectToAction("Admin");
        }

        private async Task<bool> IsAbleToEditUser(User actor, User user)
        {
            return await userManager.IsInRoleAsync(actor, "MasterAdmin") ||
                !(await userManager.IsInRoleAsync(user, "Admin"));
        }
        private async Task<ProfileView> GetProfileView(User user, User? currentUser)
        {
            return new ProfileView()
            {
                Author = user,
                Articles = GetUserArticleViews(user.Id),
                Rating = await articleStorage.likeUtility.GetUserTotalLikes(user),
                IsEditAllowed = await IsEditAllowed(user, currentUser),
                UsernameAllowedChars = userManager.Options.User.AllowedUserNameCharacters
            };
        }
        private List<ArticleView> GetUserArticleViews(Guid userId)
        {
            return articleStorage.GetAllArticles()
            .Where(a => a.AuthorId == userId)
            .Select(article => new ArticleView
            {
                Article = article,
                AverageRating = articleStorage.ratingUtility.GetAverageArticleObjectRating(article).Result
            })
            .ToList();
        } 
        private bool ValidateUserName(string userName)
        {
            string allowedCharacters = userManager.Options.User.AllowedUserNameCharacters;
            return userName.All(c => allowedCharacters.Contains(c));
        }
        private async Task UpdateNameClaim(bool signin, string userName)
        {
            var identity = (ClaimsIdentity)User.Identity;
            identity.RemoveClaim(identity.FindFirst(ClaimTypes.Name));
            identity.AddClaim(new Claim(ClaimTypes.Name, userName));
            if (signin)
            {
                await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, new ClaimsPrincipal(identity));
            }
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
