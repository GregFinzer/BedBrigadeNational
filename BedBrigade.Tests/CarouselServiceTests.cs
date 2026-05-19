using BedBrigade.Client.Services;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace BedBrigade.Tests;

public class CarouselServiceTests
{
    private string _contentRootPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _contentRootPath = Path.Combine(Path.GetTempPath(), $"BedBrigadeCarousel_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_contentRootPath, "wwwroot"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_contentRootPath))
        {
            Directory.Delete(_contentRootPath, true);
        }
    }

    [Test]
    public void ReplaceCarousel_ShouldResolveDirectoryCaseInsensitive_ForPolarisHomeCarousel()
    {
        CreateCarouselImage("media", "polaris", "pages", "Home", "carousel", "1.webp");
        CreateCarouselImage("media", "polaris", "pages", "Home", "carousel", "2.webp");
        CarouselService service = CreateService();
        string html = "<div data-component=\"bbcarousel\" id=\"polaris-home-carousel\" src=\"/media/polaris/pages/home/carousel\"></div>";

        string result = service.ReplaceCarousel(html);

        Assert.Multiple(() =>
        {
            Assert.That(result, Does.Contain("carousel-inner"));
            Assert.That(result, Does.Contain("/media/polaris/pages/Home/carousel/"));
            Assert.That(result, Does.Not.Contain("data-component=\"bbcarousel\""));
        });
    }

    [Test]
    public void ReplaceCarousel_ShouldNotThrow_WhenCarouselDirectoryDoesNotExist()
    {
        CarouselService service = CreateService();
        string html = "<div data-component=\"bbcarousel\" id=\"missing-carousel\" src=\"/media/polaris/pages/missing/carousel\"></div>";

        Assert.That(() => service.ReplaceCarousel(html), Throws.Nothing);
        Assert.That(service.ReplaceCarousel(html), Is.EqualTo(html));
    }

    private CarouselService CreateService()
    {
        Mock<IWebHostEnvironment> hostingEnvironmentMock = new Mock<IWebHostEnvironment>();
        hostingEnvironmentMock.SetupGet(env => env.ContentRootPath).Returns(_contentRootPath);
        hostingEnvironmentMock.SetupGet(env => env.WebRootPath).Returns(Path.Combine(_contentRootPath, "wwwroot"));

        ICachingService cachingService = new CachingService { IsCachingEnabled = false };
        return new CarouselService(cachingService, hostingEnvironmentMock.Object);
    }

    private void CreateCarouselImage(params string[] segments)
    {
        string directory = Path.Combine(_contentRootPath, "wwwroot", Path.Combine(segments[..^1]));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, segments[^1]), "image");
    }
}

