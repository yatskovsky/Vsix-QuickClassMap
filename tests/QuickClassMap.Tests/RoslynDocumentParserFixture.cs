using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Basic.Reference.Assemblies;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using QuickClassMap.Core.Domain;
using QuickClassMap.Core.Roslyn;
using QuickClassMap.Core.Roslyn.Traversal;

namespace QuickClassMap.Tests;

public sealed class RoslynDocumentParserFixture
{
    public async Task<List<ClassInfo>> ParseWalkDownAsync(
        IReadOnlyDictionary<string, string> sourceFiles,
        string seedFileName,
        int maxDepth)
    {
        using var workspace = CreateWorkspace(sourceFiles);
        var parser = new RoslynDocumentParser(workspace);

        return await parser.ParseWithWalkDownAsync(
            new List<string> { GetFilePath(seedFileName) },
            new ClassGraphTraversalOptions { MaxDepth = maxDepth },
            progressAction: null,
            CancellationToken.None);
    }

    public async Task<List<ClassInfo>> ParseWalkUpAsync(
        IReadOnlyDictionary<string, string> sourceFiles,
        string seedFileName,
        int maxDepth)
    {
        using var workspace = CreateWorkspace(sourceFiles);
        var parser = new RoslynDocumentParser(workspace);

        return await parser.ParseWithWalkUpAsync(
            new List<string> { GetFilePath(seedFileName) },
            new ClassGraphTraversalOptions { MaxDepth = maxDepth },
            progressAction: null,
            CancellationToken.None);
    }

    private static AdhocWorkspace CreateWorkspace(IReadOnlyDictionary<string, string> sourceFiles)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        workspace.AddProject(ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "TestProject",
            "TestProject",
            LanguageNames.CSharp,
            filePath: GetFilePath("TestProject.csproj"),
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: Net472.References.All));

        foreach (var sourceFile in sourceFiles)
        {
            var filePath = GetFilePath(sourceFile.Key);
            workspace.AddDocument(DocumentInfo.Create(
                DocumentId.CreateNewId(projectId),
                sourceFile.Key,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(sourceFile.Value), VersionStamp.Create())),
                filePath: filePath));
        }

        return workspace;
    }

    private static string GetFilePath(string fileName)
    {
        return Path.Combine(Path.GetTempPath(), "QuickClassMapTests", fileName);
    }
}
