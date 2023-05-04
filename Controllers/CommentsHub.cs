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
        private readonly UserUtility userUtility;
        public CommentsHub(ArticleContext context, UserManager<User> userManager)
        {
            articleStorage = new ArticleStorage(context);
            userUtility = new UserUtility(userManager);
        }
        public override async Task OnConnectedAsync()
        {
            Guid articleId = GetArticleId();
            AddReader(articleId, GetConnection());
            await Clients.Caller.SendAsync("GetAllComments", 
                await articleStorage.commentUtility.GetAllCommentsAsList(userUtility, await userUtility.GetUser(Context.User), articleId));
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Guid articleId = GetArticleId();
            if (articleToReaders.ContainsKey(articleId))
            {
                RemoveReader(articleId, GetConnection());
            }
            await base.OnDisconnectedAsync(exception);
        }
        public async Task MakeComment(string comment)
        {
            User? user = await userUtility.GetUser(Context.User);
            if (user == null)
            {
                return;
            }
            Guid articleId = GetArticleId();
            if (articleToReaders.ContainsKey(articleId))
            {
                Article? article = await articleStorage.GetArticleById(articleId);
                if (article != null)
                {
                    Comment userComment = await articleStorage.commentUtility.CreateComment(user, article, comment);
                    await BroadcastNewComment(articleId, userComment);
                }
            }
        }
        public async Task RemoveComment(Guid commentId)
        {
            User? user = await userUtility.GetUser(Context.User);
            if (user == null)
            {
                return;
            }
            Comment? comment = articleStorage.commentUtility.GetCommentById(commentId);
            if (comment == null || !await userUtility.IsEditAllowed(comment.Author, user))
            {
                return;
            }
            await articleStorage.commentUtility.RemoveComment(comment);
            await BroadcastRemoveComment(GetArticleId(), commentId);
        }
        private string GetConnection()
        {
            return Context.ConnectionId;
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
            User? user = await userUtility.GetUser(Context.User);
            foreach (string con in readers)
            {
                await Clients.Client(con).SendAsync("GetNewComment",
                    articleStorage.commentUtility.GetDictFromComment(comment, await userUtility.IsEditAllowed(comment.Author, user)));
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
    }
}
