using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;

namespace TestsGenerator.Library;

public class TestGenerator
{
    private readonly int _maxFileRead;
    private readonly int _maxTestGeneration;
    private readonly int _maxFileWrite;

    public TestGenerator(int maxFileRead, int maxTestGeneration, int maxFileWrite)
    {
        _maxFileRead = maxFileRead;
        _maxTestGeneration = maxTestGeneration;
        _maxFileWrite = maxFileWrite;
    }

    public TestGenerator() : this(Environment.ProcessorCount, Environment.ProcessorCount, Environment.ProcessorCount)
    { }

    public Task GenerateTestsAsync(IEnumerable<string> inputFiles, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);

        var readBlock = new TransformBlock<string, (string filePath, string content)>(async filePath =>
        {
            string content = await File.ReadAllTextAsync(filePath);
            return (filePath, content);
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _maxFileRead });

        var generateBlock = new TransformManyBlock<(string filePath, string content), TestFile>(
            input => GenerateTestFiles(input.content),
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _maxTestGeneration });

        var writeBlock = new ActionBlock<TestFile>(async testFile =>
        {
            await File.WriteAllTextAsync(Path.Combine(outputFolder, testFile.FileName), testFile.Content);
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _maxFileWrite });

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        readBlock.LinkTo(generateBlock, linkOptions);
        generateBlock.LinkTo(writeBlock, linkOptions);

        foreach (var file in inputFiles)
        {
            readBlock.Post(file);
        }
        readBlock.Complete();

        return writeBlock.Completion;
    }

    private IEnumerable<TestFile> GenerateTestFiles(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = syntaxTree.GetRoot();

        var classes = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(cls => cls.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)));

        foreach (var classDecl in classes)
        {
            var namespaceDecl = classDecl.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
            string namespaceName = namespaceDecl != null ? namespaceDecl.Name.ToString() : "GlobalNamespace";

            string testClassName = classDecl.Identifier.Text + "Tests";

            // Обработка конструктора с зависимостями
            var ctor = classDecl.Members.OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)));

            var mockFields = new List<string>();
            var ctorArgs = new List<string>();

            if (ctor != null)
            {
                foreach (var param in ctor.ParameterList.Parameters)
                {
                    string type = param.Type?.ToString() ?? "object";
                    string name = param.Identifier.Text;

                    if (type.StartsWith("I")) // интерфейс
                    {
                        mockFields.Add($"private Mock<{type}> _{name};");
                        ctorArgs.Add($"_{name}.Object");
                    }
                    else
                    {
                        ctorArgs.Add($"default({type})");
                    }
                }
            }

            string setupMethod = "";
            if (mockFields.Count > 0)
            {
                setupMethod = $@"
        public {testClassName}()
        {{
            {string.Join("\n            ", mockFields.Select(f => f + $" = new Mock<{f.Split('<')[1].TrimEnd('>')}>();"))}
            _testClass = new {classDecl.Identifier.Text}({string.Join(", ", ctorArgs)});
        }}";
            }
            else
            {
                setupMethod = $@"
        public {testClassName}()
        {{
            _testClass = new {classDecl.Identifier.Text}();
        }}";
            }

            string testClassContent = $@"
        using System;
        using Xunit;
        using Moq;
        using {namespaceName};

        namespace {namespaceName}.Tests
        {{
            public class {testClassName}
            {{
             private {classDecl.Identifier.Text} _testClass;
                {string.Join("\n        ", mockFields)}

             {setupMethod}

             {string.Join("\n\n        ", GetClassMethods(classDecl))}
         }}
        }}";

            yield return new TestFile
            {
                FileName = testClassName + ".cs",
                Content = testClassContent
            };
        }
    }

    private List<string> GetClassMethods(ClassDeclarationSyntax @class)
    {
        var methods = @class.Members.OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)))
            .ToList();

        var methodCounts = new Dictionary<string, int>();
        foreach (var method in methods)
        {
            if (!methodCounts.ContainsKey(method.Identifier.Text))
                methodCounts[method.Identifier.Text] = 0;

            methodCounts[method.Identifier.Text]++;
        }

        var overloadIndices = new Dictionary<string, int>();
        var testMethods = new List<string>();

        foreach (var method in methods)
        {
            string methodName = method.Identifier.Text;
            int count = methodCounts[methodName];

            if (!overloadIndices.ContainsKey(methodName))
                overloadIndices[methodName] = 0;

            overloadIndices[methodName]++;
            int index = overloadIndices[methodName];

            string testMethodName = count > 1
                ? $"{methodName}{index}Test"
                : $"{methodName}Test";

            var parameters = method.ParameterList.Parameters;
            var arrange = new List<string>();
            var paramNames = new List<string>();

            foreach (var param in parameters)
            {
                string type = param.Type?.ToString() ?? "object";
                string name = param.Identifier.Text;
                paramNames.Add(name);

                string defaultValue = type switch
                {
                    "int" => "0",
                    "string" => "\"\"",
                    "double" => "0.0",
                    "bool" => "false",
                    _ => "null"
                };
                arrange.Add($"{type} {name} = {defaultValue};");
            }

            string actCall = paramNames.Count > 0 ? string.Join(", ", paramNames) : "";
            string returnType = method.ReturnType.ToString();

            string act;
            string assert;

            if (returnType == "void")
            {
                act = $"_testClass.{methodName}({actCall});";
                assert = "Assert.True(false, \"autogenerated\");";
            }
            else
            {
                act = $"{returnType} actual = _testClass.{methodName}({actCall});";
                assert = $@"
{returnType} expected = default;
Assert.Equal(expected, actual);
Assert.True(false, ""autogenerated"");";
            }

            string testMethodCode = $@"
[Fact]
public void {testMethodName}()
{{
    // Arrange
    {string.Join("\n    ", arrange)}

    // Act
    {act}

    // Assert
    {assert}
}}";

            testMethods.Add(testMethodCode);
        }

        return testMethods;
    }
}