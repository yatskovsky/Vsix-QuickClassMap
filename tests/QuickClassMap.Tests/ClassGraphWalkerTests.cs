using System.Linq;
using System.Threading.Tasks;

using QuickClassMap.Core.Domain;
using QuickClassMap.Core.Roslyn.Traversal;

namespace QuickClassMap.Tests;

public class ClassGraphWalkerTests(ClassGraphWalkerFixture fixture)
    : IClassFixture<ClassGraphWalkerFixture>
{
    [Fact]
    public async Task ParseWithWalkDown_WithDepthOne_IncludesDirectSourceDependenciesOnly()
    {
        const string source = """
            namespace Sample
            {
                public class A
                {
                    public B Value { get; set; }
                }

                public class B
                {
                    public C Value { get; set; }
                }

                public class C
                {
                }
            }
            """;

        var classes = await fixture.ParseWalkDownAsync(source, "A", maxDepth: 1);

        Assert.Equal(new[] { "A", "B" }, classes.Select(classInfo => classInfo.Name).OrderBy(name => name));
        Assert.Contains(classes.Single(classInfo => classInfo.Name == "A").Relationships, relationship =>
            relationship.RelatedClassName == "Sample.B");
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    [InlineData(5, 6)]
    [InlineData(8, 9)]
    public async Task ParseWithWalkDown_SupportsConfiguredDepth(int maxDepth, int expectedClassCount)
    {
        const string source = """
            namespace Sample
            {
                public class A
                {
                    public B Value { get; set; }
                }

                public class B
                {
                    public C Value { get; set; }
                }

                public class C
                {
                    public D Value { get; set; }
                }

                public class D
                {
                    public E Value { get; set; }
                }

                public class E
                {
                    public F Value { get; set; }
                }

                public class F
                {
                    public G Value { get; set; }
                }

                public class G
                {
                    public H Value { get; set; }
                }

                public class H
                {
                    public I Value { get; set; }
                }

                public class I
                {
                    public J Value { get; set; }
                }

                public class J
                {
                }
            }
            """;

        var classes = await fixture.ParseWalkDownAsync(source, "A", maxDepth);

        Assert.Equal(expectedClassCount, classes.Count);
    }

    [Fact]
    public async Task ParseWithWalkUp_IncludesDirectSourceDependentsOnly()
    {
        const string source = """
            namespace Sample
            {
                public class A
                {
                }

                public class B
                {
                    public A Value { get; set; }
                }

                public class C
                {
                    public B Value { get; set; }
                }
            }
            """;
        var classes = await fixture.ParseWalkUpAsync(source, "A", maxDepth: 1);

        Assert.Equal(new[] { "A", "B" }, classes.Select(classInfo => classInfo.Name).OrderBy(name => name));
        Assert.Contains(classes.Single(classInfo => classInfo.Name == "B").Relationships, relationship =>
            relationship.RelatedClassName == "Sample.A");
    }

    [Fact]
    public async Task ParseWithWalkUp_WithDepthTwo_IncludesTransitiveSourceDependents()
    {
        const string source = """
            namespace Sample
            {
                public class A
                {
                }

                public class B
                {
                    public A Value { get; set; }
                }

                public class C
                {
                    public B Value { get; set; }
                }
            }
            """;
        var classes = await fixture.ParseWalkUpAsync(source, "A", maxDepth: 2);

        Assert.Equal(new[] { "A", "B", "C" }, classes.Select(classInfo => classInfo.Name).OrderBy(name => name));
    }

    [Fact]
    public async Task ParseWithWalkUp_WithCycle_DoesNotLoopOrDuplicateClasses()
    {
        const string source = """
            namespace Sample
            {
                public class A
                {
                    public B Value { get; set; }
                }

                public class B
                {
                    public A Value { get; set; }
                }
            }
            """;
        var classes = await fixture.ParseWalkUpAsync(source, "A", maxDepth: 10);

        Assert.Equal(new[] { "A", "B" }, classes.Select(classInfo => classInfo.Name).OrderBy(name => name));
    }

    [Fact]
    public async Task ParseWithWalkUp_RespectsRelationshipFilter()
    {
        const string source = """
            namespace Sample
            {
                public class A
                {
                }

                public class B : A
                {
                }

                public class C
                {
                    public A Value { get; set; }
                }
            }
            """;
        var options = new ClassGraphTraversalOptions(new[] { RelationshipType.Inherits })
        {
            MaxDepth = 1
        };

        var classes = await fixture.ParseWalkUpAsync(source, "A", options);

        Assert.Equal(new[] { "A", "B" }, classes.Select(classInfo => classInfo.Name).OrderBy(name => name));
    }

    [Fact]
    public async Task ParseWithWalkDown_WithDepthTwo_IncludesTransitiveSourceDependencies()
    {
        const string source = """
            namespace Sample
            {
                public class A
                {
                    public B Value { get; set; }
                }

                public class B
                {
                    public C Value { get; set; }
                }

                public class C
                {
                }
            }
            """;
        var classes = await fixture.ParseWalkDownAsync(source, "A", maxDepth: 2);

        Assert.Equal(new[] { "A", "B", "C" }, classes.Select(classInfo => classInfo.Name).OrderBy(name => name));
    }

    [Fact]
    public async Task ParseWithWalkDown_DoesNotFollowUsesRelationshipsBeyondDepthOne()
    {
        const string source = """
            namespace Sample
            {
                public class A
                {
                    public void Use(B value) { }
                }

                public class B
                {
                    public void Use(C value) { }
                }

                public class C
                {
                    public void Use(D value) { }
                }

                public class D
                {
                }
            }
            """;

        var classes = await fixture.ParseWalkDownAsync(source, "A", maxDepth: 3);

        Assert.Equal(new[] { "A", "B", "C" }, classes.Select(classInfo => classInfo.Name).OrderBy(name => name));
    }

    [Fact]
    public async Task ParseWithWalkDown_WithCycle_DoesNotLoopOrDuplicateClasses()
    {
        const string source = """
            namespace Sample
            {
                public class A
                {
                    public B Value { get; set; }
                }

                public class B
                {
                    public A Value { get; set; }
                }
            }
            """;
        var classes = await fixture.ParseWalkDownAsync(source, "A", maxDepth: 10);

        Assert.Equal(new[] { "A", "B" }, classes.Select(classInfo => classInfo.Name).OrderBy(name => name));
    }
}
