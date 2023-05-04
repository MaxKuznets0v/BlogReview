using BlogReview.Data;
using BlogReview.Models;
using BlogReview.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlogReview.Services
{
    public class CommentUtility
    {
        private readonly ArticleContext context;
        public CommentUtility(ArticleContext context)
        {
            this.context = context;
        }
        internal async Task<Comment> CreateComment(User author, Article article, string comment)
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
        internal async Task RemoveComment(Comment comment)
        {
            context.Comments.Remove(comment);
            await context.SaveChangesAsync();
        }
        internal Comment? GetCommentById(Guid id)
        {
            return context.Comments.FirstOrDefault(c => c.Id == id);
        }
        internal async Task<List<Dictionary<string, string>>> GetAllCommentsAsList(UserUtility userUtility, User? user, Guid articleId)
        {
            List<Comment> comments = context.Articles.Include(a => a.Comments)
                .FirstAsync(a => a.Id == articleId).Result.Comments;
            List<Dictionary<string, string>> res = new();
            foreach (var com in comments)
            {
                res.Add(GetDictFromComment(com, await userUtility.IsEditAllowed(com.Author, user)));
            }
            return res;
        }
        internal Dictionary<string, string> GetDictFromComment(Comment comment, bool editable)
        {
            return new Dictionary<string, string>
            {
                { "author", comment.Author.UserName },
                { "authorId", comment.Author.Id.ToString() },
                { "content", comment.Content },
                { "commentId", comment.Id.ToString() },
                { "editable", editable.ToString() }
            };
        }
    }
}
