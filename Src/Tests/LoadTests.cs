using System.IO;
using Xunit;

namespace Tests;

[Collection("FormulaCollection")]
public class LoadTests : IClassFixture<FormulaFixture>
{
    private readonly FormulaFixture _ciFixture;

    public LoadTests(FormulaFixture fixture)
    {
        _ciFixture = fixture;
    }
    
    [Fact]
    public void TestLoadNoSpaceMappingExample()
    {
        _ciFixture.RunCommand("load " + Path.GetFullPath("../../../../../models/NoSpacesFolder/MappingExample.4ml"),
            "LoadTests: Load command for no spaces MappingExample.4ml failed.");
        Assert.True(_ciFixture.GetLoadResult(), "LoadTests: Loading no spaces MappingExample.4ml failed.");
    }
    
    [Fact]
    public void TestLoadSpacesMappingExample()
    {
        _ciFixture.RunCommand("load " + Path.GetFullPath("../../../../../models/Test Spaces Folder/MappingExample.4ml"),
            "LoadTests: Load command for spaces MappingExample.4ml failed.");
        Assert.True(_ciFixture.GetLoadResult(), "LoadTests: Loading spaces MappingExample.4ml failed.");
    }
}