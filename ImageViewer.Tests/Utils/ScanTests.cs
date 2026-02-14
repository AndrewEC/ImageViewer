namespace ImageViewer.Tests.Utils;

using System.Collections.Generic;
using System.Linq;
using ImageViewer.Models;
using ImageViewer.State;
using ImageViewer.Utils;
using Moq;

#pragma warning disable CA1707
[TestFixture]
public sealed class ScanTests
{
    private readonly Mock<IConfigState> mockConfigState = new(MockBehavior.Strict);
    private readonly Mock<ISorting> mockSorting = new(MockBehavior.Strict);
    private Scan scan;

    [SetUp]
    public void SetUp()
    {
        scan = new(mockConfigState.Object, mockSorting.Object);
    }

    [Test]
    public void GetImageResources()
    {
        Config config = new()
        {
            SortMethod = SortMethod.Natural,
            ScanDepth = 2
        };
        mockConfigState.Setup(mock => mock.LoadConfig()).Returns(config).Verifiable();

        mockSorting.Setup(mock => mock.GetComparer(SortMethod.Natural))
            .Returns(new Sorting.NaturalComparer())
            .Verifiable();
        
        FileResource imageFolder = new(TestData.ImageFolderPath, false);
        List<ImageResource> images = scan.GetImageResources(imageFolder);

        PathLike[] imagePaths = [.. images.Select(image => image.Path)];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(imagePaths, Has.Length.EqualTo(3));
            Assert.That(imagePaths[0].Name(), Is.EqualTo(TestData.Images[0]));
            Assert.That(imagePaths[1].Name(), Is.EqualTo(TestData.Images[1]));
            Assert.That(imagePaths[2].Name(), Is.EqualTo(TestData.Images[2]));
        }

        mockConfigState.VerifyAll();
        mockSorting.VerifyAll();
    }

    [Test]
    public void ExpandPath_And_FindResourceInTree()
    {
        List<FileResource> rootResources = [new(TestData.CRoot, true)];

        scan.ExpandPath(TestData.ImageFolderPath, rootResources);

        FileResource? resource = scan.FindResourceInTree(TestData.ImageFolderPath, rootResources);
        Assert.That(resource, Is.Not.Null);

        PathLike expectedChildPath = TestData.ImageFolderPath.Join("NestedTestImages");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(resource.Path, Is.EqualTo(TestData.ImageFolderPath));
            Assert.That(resource.Children, Has.Count.EqualTo(1));
            Assert.That(resource.Children[0].Path, Is.EqualTo(expectedChildPath));
        }
    }
}
#pragma warning restore CA1707
