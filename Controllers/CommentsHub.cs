using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using BlogReview.Models;
using Microsoft.EntityFrameworkCore;
using BlogReview.Data;
using BlogReview.Services;
using Microsoft.AspNetCore.Identity;

namespace BlogReview.Controllers
{
    public class CommentsHub : Hub
    {
        private static readonly ConcurrentDictionary<Guid, List<string>> articleToReaders = new();
        private readonly ArticleStorage articleStorage;
        private readonly UserManager<User> userManager;
        public CommentsHub(ArticleContext context, UserManager<User> userManager)
        {
            this.articleStorage = new(context);
            this.userManager = userManager;
        }
        public override async Task OnConnectedAsync()
        {
            Guid articleId = GetArticleId();
            AddReader(articleId, await GetConnection());
            await Clients.Caller.SendAsync("GetAllComments", 
                await articleStorage.commentUtility.GetAllCommentsAsList(userManager, await GetUser(), articleId));
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Guid articleId = GetArticleId();
            if (articleToReaders.ContainsKey(articleId))
            {
                RemoveReader(articleId, await GetConnection());
            }
            await base.OnDisconnectedAsync(exception);
        }
        public async Task MakeComment(string comment)
        {
            if (!Context.User.Identity.IsAuthenticated) { return; }
            Guid articleId = GetArticleId();
            User user = await GetUser();
            if (articleToReaders.ContainsKey(articleId))
            {
                Article article = await articleStorage.GetArticleById(articleId);
                Comment userComment = await articleStorage.commentUtility.CreateComment(user, article, comment);
                await BroadcastNewComment(articleId, userComment);
            }
        }
        public async Task RemoveComment(Guid commentId)
        {
            if (!Context.User.Identity.IsAuthenticated) { return; }
            User user = await GetUser();
            Comment comment = await articleStorage.commentUtility.GetCommentById(commentId);
            if (comment == null || !(await IsCommentEditable(userManager, comment, user)))
            {
                return;
            }
            await articleStorage.commentUtility.RemoveComment(comment);
            await BroadcastRemoveComment(GetArticleId(), commentId);
        }
        private async Task<string> GetConnection()
        {
            return Context.ConnectionId;
        }
        private async Task<User?> GetUser()
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                return null;
            }
            return await userManager.FindByNameAsync(Context.User.Identity.Name);
        }
        private Guid GetArticleId()
        {
            return new(Context.GetHttpContext().Request.Query["articleId"]);
        }
        static private void AddReader(Guid articleId, string connection)
        {
            articleToReaders.AddOrUpdate(articleId, new List<string>() { connection },
                (k, v) => { v.Add(connection); return v; });
        }
        static private void RemoveReader(Guid articleId, string connection)
        {
            var readers = articleToReaders[articleId];
            readers.Remove(connection);
            if (readers.Count == 0)
            {
                articleToReaders.Remove(articleId, out _);
            }
        }
        private async Task BroadcastNewComment(Guid articleId, Comment comment)
        {
            var readers = articleToReaders[articleId];
            User user = await GetUser();
            foreach (string con in readers)
            {
                await Clients.Client(con).SendAsync("GetNewComment", 
                    CommentUtility.GetDictFromComment(comment, await IsCommentEditable(userManager, comment, user)));
            }
        }
        private async Task BroadcastRemoveComment(Guid articleId, Guid commentId)
        {
            var readers = articleToReaders[articleId];
            foreach (string con in readers)
            {
                await Clients.Client(con).SendAsync("RemoveComment", commentId);
            }
        }
        public static async Task<bool> IsCommentEditable(UserManager<User> userManager, Comment comment, User currentUser)
        {
            if (currentUser == null)
            {
                return false;
            }
            return (await userManager.IsInRoleAsync(currentUser, "Admin")) || comment.AuthorId == currentUser.Id;
        }
    }
}
