using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickClassMap.Tests;

public class RoslynDocumentParserTests(RoslynDocumentParserFixture fixture)
    : IClassFixture<RoslynDocumentParserFixture>
{
    [Fact]
    public async Task ParseWithWalkUp_WithMultipleClassesInOneDocument_ExcludesUnrelatedSibling()
    {
        var sourceFiles = new Dictionary<string, string>
        {
            ["A.cs"] = """
                namespace Sample
                {
                    public class A
                    {
                    }
                }
                """,
            ["B.cs"] = """
                namespace Sample
                {
                    public class B
                    {
                        public A Value { get; set; }
                    }

                    public class Sibling
                    {
                        public C Value { get; set; }
                    }
                }
                """,
            ["C.cs"] = """
                namespace Sample
                {
                    public class C
                    {
                    }
                }
                """
        };
        var classes = await fixture.ParseWalkUpAsync(sourceFiles, "A.cs", maxDepth: 1);

        Assert.Equal(new[] { "A", "B" }, classes.Select(classInfo => classInfo.Name).OrderBy(name => name));
    }

    [Fact]
    public async Task ParseWithWalkDown_WithMultipleClassesInOneDocument_ExcludesUnrelatedSibling()
    {
        var sourceFiles = new Dictionary<string, string>
        {
            ["A.cs"] = """
                namespace Sample
                {
                    public class A
                    {
                        public B Value { get; set; }
                    }
                }
                """,
            ["B.cs"] = """
                namespace Sample
                {
                    public class B
                    {
                    }

                    public class Sibling
                    {
                        public C Value { get; set; }
                    }
                }
                """,
            ["C.cs"] = """
                namespace Sample
                {
                    public class C
                    {
                    }
                }
                """
        };
        var classes = await fixture.ParseWalkDownAsync(sourceFiles, "A.cs", maxDepth: 2);

        Assert.Equal(new[] { "A", "B" }, classes.Select(classInfo => classInfo.Name).OrderBy(name => name));
    }

    [Fact]
    public async Task ParseWithWalkDown_WithGenericTypesOfDifferentArities_DoesNotCollapseSymbols()
    {
        var sourceFiles = new Dictionary<string, string>
        {
            ["A.cs"] = """
                namespace Sample
                {
                    public class A
                    {
                        public Box<int> Value { get; set; }
                    }
                }
                """,
            ["B.cs"] = """
                namespace Sample
                {
                    public class Box<T>
                    {
                    }

                    public class Box<TFirst, TSecond>
                    {
                    }
                }
                """
        };
        var classes = await fixture.ParseWalkDownAsync(sourceFiles, "A.cs", maxDepth: 1);

        Assert.Equal(2, classes.Count);
        Assert.Contains(classes, classInfo => classInfo.FullName == "Sample.Box<T>");
        Assert.DoesNotContain(classes, classInfo => classInfo.FullName == "Sample.Box<TFirst, TSecond>");
    }
}
