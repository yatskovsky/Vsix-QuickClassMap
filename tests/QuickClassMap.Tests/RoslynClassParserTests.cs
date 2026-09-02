namespace QuickClassMap.Tests;

public class RoslynClassParserTests(RoslynClassParserFixture fixture)
    : IClassFixture<RoslynClassParserFixture>
{
    [Fact]
    public void ParseClasses_WithBasicClassDefinition_ReturnsClassInfo()
    {
        const string source = "namespace Sample { public class Customer { } }";

        var classInfo = Assert.Single(fixture.ParseClasses(source));

        Assert.Equal("Customer", classInfo.Name);
        Assert.Equal("Sample.Customer", classInfo.FullName);
        Assert.False(classInfo.IsInterface);
    }
}
