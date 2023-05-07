using BlogReview.Data;
using BlogReview.Models;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;

namespace BlogReview.Services
{
    public class TagService
    {
        private readonly ArticleContext context;
        public TagService(ArticleContext context)
        {
            this.context = context;
        }
        internal async Task<List<Tag>> ParseTags(string tags)
        {
            List<Tag> res = new();
            if (tags != null)
            {
                foreach (string tag in tags.Split(','))
                {
                    string capTag = CapitalizeTag(tag);
                    Tag? tagObject = await context.Tags.FirstOrDefaultAsync(t => t.Name == capTag);
                    if (tagObject != null)
                    {
                        res.Add(tagObject);
                    }
                    else
                    {
                        res.Add(new Tag() { Name = capTag });
                    }
                }
            }
            return res;
        }
        internal static string CapitalizeTag(string tag)
        {
            tag = tag.ToLower();
            return tag[0].ToString().ToUpper() + tag[1..];
        }
        internal async Task UpdateArticleTags(Article article, string tags)
        {
            var storedTags = await context.ArticleTags
                    .Where(ao => ao.Article == article).ToListAsync();
            var incomingTags = ParseTags(tags).Result;
            RemoveArticleTags(incomingTags, storedTags);
            await AddArticleTags(article, incomingTags);
            await context.SaveChangesAsync();
        }
        internal void RemoveArticleTags(List<Tag> incomingTags, List<ArticleTags> storedTags)
        {
            var incomingTagNames = incomingTags.Select(t => t.Name).ToHashSet();
            foreach (var storedTag in storedTags)
            {
                if (!incomingTagNames.Contains(storedTag.Tag.Name))
                {
                    context.Remove(storedTag);
                }
            }
        }
        internal async Task AddArticleTags(Article article, List<Tag> incomingTags)
        {
            foreach (Tag tag in incomingTags)
            {
                ArticleTags? articleTag = await context.ArticleTags
                    .FirstOrDefaultAsync(ao => ao.Article == article && ao.Tag == tag);
                if (articleTag == null)
                {
                    await context.ArticleTags.AddAsync(new ArticleTags()
                    {
                        Article = article,
                        Tag = tag
                    });
                }
            }
        }
        internal async Task<List<Tag>> GetSimilarTags(string query)
        {
            return await context.Tags
                .Where(t => t.Name.Contains(query)).ToListAsync();
        }
        internal IQueryable GetTagCounts()
        {
            return context.ArticleTags
                .GroupBy(at => at.Tag.Name)
                .Select(t => new { tag = t.Key, count = t.Count() });
        }
    }
}
