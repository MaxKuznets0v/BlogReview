using BlogReview.Models;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogReview.Services
{
    public class UserUtility
    {
        private const int BlockYears = 100;
        private readonly UserManager<User> userManager;
        public UserUtility(UserManager<User> userManager) 
        { 
            this.userManager = userManager;
        }
        public async Task<User?> GetUser(ClaimsPrincipal principal)
        {
            if (principal.Identity != null && principal.Identity.IsAuthenticated)
            {
                return await userManager.FindByNameAsync(principal.Identity.Name);
            }
            else
            {
                return null;
            }
        }
        public async Task<User?> GetUser(Guid id)
        {
            return await userManager.FindByIdAsync(id.ToString());
        }
        public async Task<User?> GetUserByEmail(string email)
        {
            return await userManager.FindByEmailAsync(email);
        }
        public async Task<bool> IsEditAllowed(User author, User? currentUser)
        {
            if (currentUser == null)
            {
                return false;
            }
            return await IsAdmin(currentUser) || author.Id == currentUser.Id;
        }
        public async Task<bool> IsAdmin(User user)
        {
            return await IsUserInRole(user, "Admin") || await IsUserInRole(user, "MasterAdmin");
        }
        public async Task<bool> IsUserInRole(User user, string roleName)
        {
            return await userManager.IsInRoleAsync(user, roleName);
        }
        public async Task AddRole(User user,  string role)
        {
            await userManager.AddToRoleAsync(user, role);
        }
        public async Task RemoveRole(User user, string role)
        {
            await userManager.RemoveFromRoleAsync(user, role);
        }
        public async Task AddLogin(User user, ExternalLoginInfo? info)
        {
            await userManager.AddLoginAsync(user, info);
        }
        public async Task<IdentityResult> CreateUser(User user)
        {
            return await userManager.CreateAsync(user);
        }
        public async Task<User> CreateUser(ExternalLoginInfo info, string role)
        {
            var user = new User
            {
                UserName = info.Principal.FindFirstValue(ClaimTypes.Name),
                Email = info.Principal.FindFirstValue(ClaimTypes.Email)
            };
            var result = await CreateUser(user);
            if (!result.Succeeded)
            {
                user.UserName = user.Email;
                await CreateUser(user);
            }
            await AddRole(user, role);
            return user;
        }
        public async Task BlockUser(User user)
        {
            user.LockoutEnd = DateTime.UtcNow.AddYears(BlockYears);
            await UpdateUser(user);
        }
        public async Task UnBlockUser(User user)
        {
            user.LockoutEnd = null;
            await UpdateUser(user);
        }
        public bool IsUserBlocked(User user)
        {
            return user.LockoutEnd != null;
        }
        public async Task UpdateUser(User user)
        {
            await userManager.UpdateAsync(user);
        }
        public async Task RemoveUser(User user)
        {
            await userManager.DeleteAsync(user);
        }
        public async Task<List<User>> GetUserList()
        {
            return await userManager.Users.ToListAsync();
        }
        public string GetUserNameAllowedChars()
        {
            return userManager.Options.User.AllowedUserNameCharacters;
        }
    }
}
