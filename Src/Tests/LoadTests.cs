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
    
    [Fact]
    public void TestLoadParserTestEOF()
    {
        _ciFixture.RunCommand("load " + Path.GetFullPath("../../../../../../../Tst/Tests/Parsers/EOFErrors/ParserTest.4ml"),
            "LoadTests: Load command for ParserTest.4ml failed.");
        Assert.False(_ciFixture.GetLoadResult(), "LoadTests: Loading ParserTest.4ml failed.");
    }
    
    [Fact]
    public void TestLoadParserTest2EOF()
    {
        _ciFixture.RunCommand("load " + Path.GetFullPath("../../../../../../../Tst/Tests/Parsers/EOFErrors/ParserTest2.4ml"),
            "LoadTests: Load command for ParserTest2.4ml failed.");
        Assert.False(_ciFixture.GetLoadResult(), "LoadTests: Loading ParserTest2.4ml failed.");
    }
    
    [Fact]
    public void TestLoadParserTest3EOF()
    {
        _ciFixture.RunCommand("load " + Path.GetFullPath("../../../../../../../Tst/Tests/Parsers/EOFErrors/ParserTest3.4ml"),
            "LoadTests: Load command for ParserTest3.4ml failed.");
        Assert.False(_ciFixture.GetLoadResult(), "LoadTests: Loading ParserTest3.4ml failed.");
    }
    
    [Fact]
    public void TestLoadParserTest4EOF()
    {
        _ciFixture.RunCommand("load " + Path.GetFullPath("../../../../../../../Tst/Tests/Parsers/EOFErrors/ParserTest4.4ml"),
            "LoadTests: Load command for ParserTest4.4ml failed.");
        Assert.False(_ciFixture.GetLoadResult(), "LoadTests: Loading ParserTest4.4ml failed.");
    }
    
    [Fact]
    public void TestLoadParserTest5EOF()
    {
        _ciFixture.RunCommand("load " + Path.GetFullPath("../../../../../../../Tst/Tests/Parsers/EOFErrors/ParserTest5.4ml"),
            "LoadTests: Load command for ParserTest5.4ml failed.");
        Assert.False(_ciFixture.GetLoadResult(), "LoadTests: Loading ParserTest5.4ml failed.");
    }
}