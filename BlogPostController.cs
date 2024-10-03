public class BlogPostController : ControllerBase
{
    private readonly IMemoryCache _memoryCache;
    private readonly IBlogRepository _blogRepository;
    private readonly CancellationTokenSource resetToken;

    public BlogPostController(IMemoryCache memoryCache, IBlogRepository blogRepository)
    {
        _memoryCache = memoryCache;
        _blogRepository = blogRepository;
        resetToken = new CancellationTokenSource();
    }

    public async Task<IActionResult> GetBlogPost(int id)
    {
        // Cache key
        var cacheKey = $"BlogPost_{id}";

        // Check if the cache contains the blog post
        if (!_memoryCache.TryGetValue(cacheKey, out BlogPost blogPost))
        {
            // Retrieve the blog post from the repository
            blogPost = await _blogRepository.GetBlogPostByIdAsync(id);

            var cacheEntryOptions = new MemoryCacheEntryOptions().AddExpirationToken(new CancellationChangeToken(resetToken.Token));

            // Save the blog post in the cache
            _memoryCache.Set(cacheKey, blogPost, cacheEntryOptions);
        }

        return Ok(blogPost);
    }
}
