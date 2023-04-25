namespace BlogReview.ViewModels
{
    public class ArticlePaginate
    {
        public IEnumerable<ArticleView> ArticleViews { get; set; }
        public string PageLoadUrl { get; set; }
    }
}
