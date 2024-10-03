using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlogPostTests
{
    // BlogPost model and IBlogRepository interface
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

    public interface IBlogRepository
    {
        Task<BlogPost> GetBlogPostByIdAsync(int id);
    }

    [TestFixture]
    public class BlogPostControllerTests
    {
        private BlogPostController _controller;
        private Mock<IMemoryCache> _memoryCacheMock;
        private Mock<IBlogRepository> _blogRepositoryMock;

        [SetUp]
        public void Setup()
        {
            // Create mock objects for IMemoryCache and IBlogRepository
            _memoryCacheMock = new Mock<IMemoryCache>();
            _blogRepositoryMock = new Mock<IBlogRepository>();

            // Instantiate BlogPostController with the mocked dependencies
            _controller = new BlogPostController(_memoryCacheMock.Object, _blogRepositoryMock.Object);
        }

        [Test]
        public async Task GetBlogPost_ReturnsCachedBlogPost_WhenBlogPostIsInCache()
        {
            // Arrange
            var blogPostId = 1;
            var cachedBlogPost = new BlogPost { Id = blogPostId, Title = "Cached Post" };

            // Mock cache to return the blog post
            _memoryCacheMock.Setup(mc => mc.TryGetValue($"BlogPost_{blogPostId}", out cachedBlogPost)).Returns(true);

            // Act
            var result = await _controller.GetBlogPost(blogPostId) as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual(cachedBlogPost, result.Value);

            // Ensure the blog repository was never called because it was cached
            _blogRepositoryMock.Verify(repo => repo.GetBlogPostByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task GetBlogPost_RetrievesAndCachesBlogPost_WhenBlogPostIsNotInCache()
        {
            // Arrange
            var blogPostId = 2;
            BlogPost cachedBlogPost = null;
            var newBlogPost = new BlogPost { Id = blogPostId, Title = "New Post" };

            // Mock cache to return null (simulating a cache miss)
            _memoryCacheMock.Setup(mc => mc.TryGetValue($"BlogPost_{blogPostId}", out cachedBlogPost)).Returns(false);

            // Mock the blog repository to return a blog post
            _blogRepositoryMock.Setup(repo => repo.GetBlogPostByIdAsync(blogPostId)).ReturnsAsync(newBlogPost);

            // Act
            var result = await _controller.GetBlogPost(blogPostId) as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual(newBlogPost, result.Value);

            // Verify that the blog post was retrieved from the repository
            _blogRepositoryMock.Verify(repo => repo.GetBlogPostByIdAsync(blogPostId), Times.Once);

            // Verify that the blog post was added to the cache
            _memoryCacheMock.Verify(mc => mc.Set(
                $"BlogPost_{blogPostId}",
                It.Is<BlogPost>(p => p.Id == blogPostId),
                It.IsAny<MemoryCacheEntryOptions>()),
                Times.Once);
        }
    }
}
