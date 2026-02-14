namespace ImageViewer.Tests.State;

using Moq;
using ImageViewer.State;
using ImageViewer.Utils;
using ImageViewer.Models;
using System.ComponentModel;
using System.Collections.Generic;

#pragma warning disable CA1707
[TestFixture]
public sealed class AppStateTests
{
    private readonly Mock<IScan> mockScan = new(MockBehavior.Strict);

    private AppState appState;

    [SetUp]
    public void SetUp()
    {
        appState = new AppState(mockScan.Object);
    }

    [TearDown]
    public void TearDown()
    {
        mockScan.Reset();
    }

    [Test]
    public void AppState_UpdatesSelectedFolderResources_WhenSelectedFolderIsChanged()
    {
        FileResource fileResource = new(TestData.ImageFolderPath, true);

        ImageResource imageResource = new(TestData.ImageFolderPath.Join(TestData.Images[0]));

        mockScan.Setup(mock => mock.GetImageResources(fileResource))
            .Returns([imageResource])
            .Verifiable();

        EventRecorder recorder = new(appState);
        appState.SelectedFolder = fileResource;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(appState.SelectedImage, Is.Null);
            Assert.That(appState.SelectedFolder, Is.EqualTo(fileResource));
            Assert.That(appState.SelectedFolderResources, Has.Count.EqualTo(1));
            Assert.That(appState.SelectedFolderResources[0], Is.EqualTo(imageResource));

            Assert.That(recorder.ChangedProperties, Has.Count.EqualTo(3));
            Assert.That(recorder.ChangedProperties, Contains.Item(nameof(AppState.SelectedFolder)));
            Assert.That(recorder.ChangedProperties, Contains.Item(nameof(AppState.SelectedFolderResources)));
            Assert.That(recorder.ChangedProperties, Contains.Item(nameof(AppState.SelectedImage)));
        }

        mockScan.VerifyAll();
    }

    [Test]
    public void AppState_UpdatesSelectedTabIndex_WhenSelectedImageIsUpdated()
    {
        ImageResource imageResource = new(TestData.ImageFolderPath.Join(TestData.Images[0]));

        EventRecorder recorder = new(appState);
        appState.SelectedImage = imageResource;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(appState.SelectedImage, Is.EqualTo(imageResource));
            Assert.That(appState.SelectedTabIndex, Is.EqualTo((int)Tabs.ImagePreview));

            Assert.That(recorder.ChangedProperties, Has.Count.EqualTo(2));
            Assert.That(recorder.ChangedProperties, Contains.Item(nameof(AppState.SelectedImage)));
            Assert.That(recorder.ChangedProperties, Contains.Item(nameof(AppState.SelectedTabIndex)));
        }
    }

    [Test]
    public void AppState_InitializesFileNodeTree_WhenLoadingStartingPath()
    {
        PathLike root = new(@"C:\");
        FileResource rootResource = new(root, true);
        List<FileResource> rootResourceList = [rootResource];
        PathLike startingPath = root.Join("TestData");

        ImageResource imageResource = new(startingPath.Join(TestData.Images[0]));

        mockScan.Setup(mock => mock.GetDriveRootResources()).Returns(rootResourceList).Verifiable();
        
        mockScan.Setup(mock => mock.GetImageResources(null)).Returns([]).Verifiable();
        mockScan.Setup(mock => mock.GetImageResources(rootResource)).Returns([imageResource]).Verifiable();
        
        mockScan.Setup(mock => mock.ExpandPath(startingPath, rootResourceList)).Verifiable();

        mockScan.Setup(mock => mock.FindResourceInTree(startingPath, rootResourceList))
            .Returns(rootResource)
            .Verifiable();

        appState.LoadStartingPath(startingPath);

        Assert.That(appState.FileNodeTree, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(appState.FileNodeTree, Has.Count.EqualTo(1));
            Assert.That(appState.FileNodeTree[0].Resource.Path, Is.EqualTo(root));

            Assert.That(appState.SelectedFolderResources, Has.Count.EqualTo(1));
            Assert.That(appState.SelectedFolderResources[0], Is.EqualTo(imageResource));

            Assert.That(appState.SelectedTabIndex, Is.EqualTo((int)Tabs.Folders));
        }

        mockScan.VerifyAll();
    }

    private sealed class EventRecorder
    {
        public EventRecorder(AppState appState)
        {
            appState.PropertyChanged += OnPropertyChanged;
        }

        public List<string?> ChangedProperties { get; } = [];

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            ChangedProperties.Add(e.PropertyName);
        }
    }
}
#pragma warning restore CA1707
