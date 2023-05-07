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
        private readonly ArticleStorageService articleStorage;
        private readonly UserService userService;
        public CommentsHub(ArticleStorageService articleStorage, UserService userService)
        {
            this.articleStorage = articleStorage;
            this.userService = userService;
        }
        public override async Task OnConnectedAsync()
        {
            Guid articleId = GetArticleId();
            AddReader(articleId, GetConnection());
            await Clients.Caller.SendAsync("GetAllComments", 
                await articleStorage.commentService.GetAllCommentsAsList(userService, await userService.GetUser(Context.User), articleId));
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
            User? user = await userService.GetUser(Context.User);
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
                    Comment userComment = await articleStorage.commentService.CreateComment(user, article, comment);
                    await BroadcastNewComment(articleId, userComment);
                }
            }
        }
        public async Task RemoveComment(Guid commentId)
        {
            User? user = await userService.GetUser(Context.User);
            if (user == null)
            {
                return;
            }
            Comment? comment = articleStorage.commentService.GetCommentById(commentId);
            if (comment == null || !await userService.IsEditAllowed(comment.Author, user))
            {
                return;
            }
            await articleStorage.commentService.RemoveComment(comment);
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
            User? user = await userService.GetUser(Context.User);
            foreach (string con in readers)
            {
                await Clients.Client(con).SendAsync("GetNewComment",
                    articleStorage.commentService.GetDictFromComment(comment, await userService.IsEditAllowed(comment.Author, user)));
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
