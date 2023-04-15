using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using BlogReview.Models;
using Microsoft.EntityFrameworkCore;
using BlogReview.Data;
using Microsoft.AspNetCore.Identity;

namespace BlogReview.Controllers
{
    internal class Connection
    {
        public Guid userId { get; }
        public string connectionId { get; }
        public Connection(Guid userId, string connectionId)
        {
            this.userId = userId;
            this.connectionId = connectionId;
        }
        public bool IsLogged()
        {
            return userId != Guid.Empty;
        }
    }
    public class CommentsHub : Hub
    {
        private static readonly ConcurrentDictionary<Guid, List<Connection>> articleToReaders = new();
        private readonly ArticleContext context;
        private readonly UserManager<User> userManager;
        public CommentsHub(ArticleContext context, UserManager<User> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }
        public override async Task OnConnectedAsync()
        {
            Guid articleId = GetArticleId();
            AddReader(articleId, await GetConnection());
            await Clients.Caller.SendAsync("GetAllComments", GetAllCommentsAsList(articleId));
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
                Article article = await context.Articles.FirstAsync(a => a.Id == articleId);
                Comment userComment = await CreateComment(user, article, comment);
                await BroadcastNewComment(articleId, userComment);
            }
        }
        private async Task<Comment> CreateComment(User author, Article article, string comment)
        {
            Comment userComment = new()
            {
                Author = author,
                Article = article,
                Content = comment
            };
            await context.Comments.AddAsync(userComment);
            await context.SaveChangesAsync();
            return userComment;
        }
        private async Task<Connection> GetConnection()
        {
            Connection con;
            if (Context.User.Identity.IsAuthenticated)
            {
                User user = await GetUser();
                con = new Connection(user.Id, "");
            }
            else
            {
                con = new Connection(Guid.Empty, Context.ConnectionId);
            }
            return con;
        }
        private async Task<User> GetUser()
        {
            return await userManager.FindByNameAsync(Context.User.Identity.Name);
        }
        private Guid GetArticleId()
        {
            return new(Context.GetHttpContext().Request.Query["articleId"]);
        }
        static private void AddReader(Guid articleId, Connection connection)
        {
            articleToReaders.AddOrUpdate(articleId, new List<Connection>() { connection },
                (k, v) => { v.Add(connection); return v; });
        }
        static private void RemoveReader(Guid articleId, Connection connection)
        {
            var readers = articleToReaders[articleId];
            readers.Remove(connection);
            if (readers.Count == 0)
            {
                articleToReaders.Remove(articleId, out _);
            }
        }
        private List<Dictionary<string, string>> GetAllCommentsAsList(Guid articleId)
        {
            List<Comment> comments = context.Articles.Include(a => a.Comments)
                .FirstAsync(a => a.Id == articleId).Result.Comments;
            List<Dictionary<string, string>> res = new();
            foreach (var com in comments)
            {
                res.Add(GetDictFromComment(com));
            }
            return res;
        }
        static private Dictionary<string, string> GetDictFromComment(Comment comment)
        {
            return new Dictionary<string, string>
            {
                { "author", comment.Author.UserName },
                { "authorId", comment.Author.Id.ToString() },
                { "content", comment.Content }
            };
        }
        private async Task BroadcastNewComment(Guid articleId, Comment comment)
        {
            var readers = articleToReaders[articleId];
            foreach (Connection con in readers)
            {
                if (con.IsLogged())
                {
                    await Clients.User(con.userId.ToString()).SendAsync("GetNewComment", GetDictFromComment(comment));
                }
                else
                {
                    await Clients.Client(con.connectionId).SendAsync("GetNewComment", GetDictFromComment(comment));
                }
            }
        }
    }
}
