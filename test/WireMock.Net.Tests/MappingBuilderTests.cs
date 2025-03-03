#if !(NET452 || NET461 || NETCOREAPP3_1)
using System;
using System.Threading.Tasks;
using Moq;
using VerifyTests;
using VerifyXunit;
using WireMock.Handlers;
using WireMock.Logging;
using WireMock.Net.Tests.VerifyExtensions;
using WireMock.Owin;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Serialization;
using WireMock.Settings;
using WireMock.Util;
using Xunit;

namespace WireMock.Net.Tests;

[UsesVerify]
public class MappingBuilderTests
{
    private static readonly VerifySettings VerifySettings = new();
    static MappingBuilderTests()
    {
        VerifySettings.Init();
    }

    private static readonly Guid NewGuid = new("98fae52e-76df-47d9-876f-2ee32e931d9b");
    private const string MappingGuid = "41372914-1838-4c67-916b-b9aacdd096ce";
    private static readonly DateTime UtcNow = new(2023, 1, 14, 15, 16, 17);

    private readonly Mock<IFileSystemHandler> _fileSystemHandlerMock;

    private readonly MappingBuilder _sut;

    public MappingBuilderTests()
    {
        _fileSystemHandlerMock = new Mock<IFileSystemHandler>();

        var guidUtilsMock = new Mock<IGuidUtils>();
        guidUtilsMock.Setup(g => g.NewGuid()).Returns(NewGuid);

        var dateTimeUtilsMock = new Mock<IDateTimeUtils>();
        dateTimeUtilsMock.SetupGet(d => d.UtcNow).Returns(UtcNow);

        var settings = new WireMockServerSettings
        {
            FileSystemHandler = _fileSystemHandlerMock.Object,
            Logger = Mock.Of<IWireMockLogger>()
        };
        var options = new WireMockMiddlewareOptions();
        var matcherMapper = new MatcherMapper(settings);
        var mappingConverter = new MappingConverter(matcherMapper);
        var mappingToFileSaver = new MappingToFileSaver(settings, mappingConverter);

        _sut = new MappingBuilder(
            settings,
            options,
            mappingConverter,
            mappingToFileSaver,
            guidUtilsMock.Object,
            dateTimeUtilsMock.Object
        );

        _sut.Given(Request.Create()
            .WithPath("/foo")
            .UsingGet()
        )
        .WithGuid(MappingGuid)
        .RespondWith(Response.Create()
            .WithBody(@"{ msg: ""Hello world!""}")
        );
    }

    [Fact]
    public Task GetMappings()
    {
        // Act
        var mappings = _sut.GetMappings();

        // Verify
        return Verifier.Verify(mappings, VerifySettings);
    }

    [Fact]
    public Task ToJson()
    {
        // Act
        var json = _sut.ToJson();

        // Verify
        return Verifier.VerifyJson(json, VerifySettings);
    }

    [Fact]
    public void SaveMappingsToFile_FolderExists_IsFalse()
    {
        // Arrange
        var path = "path";

        // Act
        _sut.SaveMappingsToFile(path);

        // Verify
        _fileSystemHandlerMock.Verify(fs => fs.GetMappingFolder(), Times.Never);
        _fileSystemHandlerMock.Verify(fs => fs.FolderExists(path), Times.Once);
        _fileSystemHandlerMock.Verify(fs => fs.CreateFolder(path), Times.Once);
        _fileSystemHandlerMock.Verify(fs => fs.WriteMappingFile(path, It.IsAny<string>()), Times.Once);
        _fileSystemHandlerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void SaveMappingsToFile_FolderExists_IsTrue()
    {
        // Arrange
        var path = "path";
        _fileSystemHandlerMock.Setup(fs => fs.FolderExists(It.IsAny<string>())).Returns(true);

        // Act
        _sut.SaveMappingsToFile(path);

        // Verify
        _fileSystemHandlerMock.Verify(fs => fs.GetMappingFolder(), Times.Never);
        _fileSystemHandlerMock.Verify(fs => fs.FolderExists(path), Times.Once);
        _fileSystemHandlerMock.Verify(fs => fs.WriteMappingFile(path, It.IsAny<string>()), Times.Once);
        _fileSystemHandlerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void SaveMappingsToFolder_FolderIsNull()
    {
        // Arrange
        var mappingFolder = "mapping-folder";
        _fileSystemHandlerMock.Setup(fs => fs.GetMappingFolder()).Returns(mappingFolder);
        _fileSystemHandlerMock.Setup(fs => fs.FolderExists(It.IsAny<string>())).Returns(true);

        // Act
        _sut.SaveMappingsToFolder(null);

        // Verify
        _fileSystemHandlerMock.Verify(fs => fs.GetMappingFolder(), Times.Once);
        _fileSystemHandlerMock.Verify(fs => fs.FolderExists(mappingFolder), Times.Once);
        _fileSystemHandlerMock.Verify(fs => fs.WriteMappingFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _fileSystemHandlerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void SaveMappingsToFolder_FolderExists_IsTrue()
    {
        // Arrange
        var path = "path";
        _fileSystemHandlerMock.Setup(fs => fs.FolderExists(It.IsAny<string>())).Returns(true);

        // Act
        _sut.SaveMappingsToFolder(path);

        // Verify
        _fileSystemHandlerMock.Verify(fs => fs.GetMappingFolder(), Times.Never);
        _fileSystemHandlerMock.Verify(fs => fs.FolderExists(path), Times.Once);
        _fileSystemHandlerMock.Verify(fs => fs.WriteMappingFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _fileSystemHandlerMock.VerifyNoOtherCalls();
    }
}
#endif