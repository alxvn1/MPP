using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestsGenerator.Library;

public class TestGenerator
{
    private readonly int _maxRead, _maxGen, _maxWrite;

    public TestGenerator(int maxRead, int maxGen, int maxWrite)
    {
        _maxRead = maxRead;
        _maxGen = maxGen;
        _maxWrite = maxWrite;
    }

    public TestGenerator() : this(Environment.ProcessorCount, Environment.ProcessorCount, Environment.ProcessorCount) { }

    public async Task GenerateTestsAsync(IEnumerable<string> inputFiles, string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);
        
        var readBlock = new TransformBlock<string, string>(async path => 
        {
            try {
                return await File.ReadAllTextAsync(path);
            } catch (Exception ex) {
                Console.WriteLine($"[Error] Ошибка чтения {path}: {ex.Message}");
                return null!; // Игнорируем файл
            }
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _maxRead });

        
        var generateBlock = new TransformManyBlock<string, TestFile>(sourceCode => 
        {
            if (string.IsNullOrWhiteSpace(sourceCode)) return Enumerable.Empty<TestFile>();
            
            try {
                return GenerateTestFilesLogic(sourceCode);
            } catch (Exception ex) {
                Console.WriteLine($"[Error] Ошибка парсинга Roslyn: {ex.Message}");
                return Enumerable.Empty<TestFile>();
            }
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _maxGen });

        
        var writeBlock = new ActionBlock<TestFile>(async file => 
        {
            try {
                await File.WriteAllTextAsync(Path.Combine(outputFolder, file.FileName), file.Content);
            } catch (Exception ex) {
                Console.WriteLine($"[Error] Не удалось записать файл {file.FileName}: {ex.Message}");
            }
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _maxWrite });

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true }; //
        readBlock.LinkTo(generateBlock, linkOptions);
        generateBlock.LinkTo(writeBlock, linkOptions);

        foreach (var file in inputFiles) readBlock.Post(file);

        readBlock.Complete();
        await writeBlock.Completion;
    }

    private IEnumerable<TestFile> GenerateTestFilesLogic(string sourceCode)
    {
        var root = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();
        //ищем классы по аст. ост только паблик 
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)));

        foreach (var cls in classes)
        {
            string testCode = GenerateSingleTestClass(cls);
            yield return new TestFile { 
                FileName = $"{cls.Identifier.Text}Tests.cs", 
                Content = testCode 
            };
        }
    }

    private string GenerateSingleTestClass(ClassDeclarationSyntax cls)
    {
        string className = cls.Identifier.Text;
        
        // Достаем публичные методы тестируемого класса
        var methods = cls.Members.OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)))
            .ToList();

        // ген-я моков фейковых реал интерфейсов 
        var mockFields = new List<string>();
        var mockInitializations = new List<string>();
        var constructorArgs = new List<string>();

        // Ищем конструктор с наибольшим числом параметров
        var ctor = cls.Members.OfType<ConstructorDeclarationSyntax>()
            .OrderByDescending(c => c.ParameterList.Parameters.Count)
            .FirstOrDefault();

        if (ctor != null)
        {
            foreach (var param in ctor.ParameterList.Parameters)
            {
                string paramType = param.Type.ToString();
                string paramName = param.Identifier.Text;
                
                // инт если начинается на I и след буква большая
                bool isInterface = paramType.Length > 1 && paramType[0] == 'I' && char.IsUpper(paramType[1]);

                if (isInterface)
                {
                    // "logger" превращается в "Logger" + потом в "_mockLogger"
                    string capitalizeParam = char.ToUpper(paramName[0]) + paramName.Substring(1);
                    string mockName = $"_mock{capitalizeParam}";
                    
                    mockFields.Add($"        private Mock<{paramType}> {mockName};");
                    mockInitializations.Add($"            {mockName} = new Mock<{paramType}>();");
                    constructorArgs.Add($"{mockName}.Object"); // Передаем Mock object
                }
                else
                {
                    // Обычные типы вроде int, string передаем как default
                    constructorArgs.Add($"default({paramType})");
                }
            }
        }

        // ======= ПЕРЕГРУЖЕННЫЕ МЕТОДЫ =======
        var methodCounts = new Dictionary<string, int>();
        var testMethods = new List<string>();

        foreach (var method in methods)
        {
            string name = method.Identifier.Text;
            
            if (!methodCounts.ContainsKey(name)) methodCounts[name] = 1;
            else methodCounts[name]++;

            // Проверяем наличие перегрузок (больше 1 метода с таким именем)
            bool isOverloaded = methods.Count(m => m.Identifier.Text == name) > 1;
            // Если перегружен - добавляем номер
            string testName = isOverloaded ? $"{name}{methodCounts[name]}Test" : $"{name}Test";

            var arrange = method.ParameterList.Parameters.Select(p => 
                $"            {p.Type} {p.Identifier.Text} = default;");

            string args = string.Join(", ", method.ParameterList.Parameters.Select(p => p.Identifier.Text));
            string act = method.ReturnType.ToString() == "void" 
                ? $"            _testClass.{name}({args});" 
                : $"            var actual = _testClass.{name}({args});";

            string assert = method.ReturnType.ToString() == "void"
                ? "            Assert.True(true);"
                : "            var expected = default;\n            Assert.Equal(expected, actual);";

            testMethods.Add($@"
        [Fact]
        public void {testName}()
        {{
            // Arrange
{string.Join("\n", arrange)}

            // Act
{act}

            // Assert
{assert}
            Assert.True(false, ""autogenerated"");
        }}");
        }


        string testClassInstantiate = constructorArgs.Count > 0 
            ? $"_testClass = new {className}({string.Join(", ", constructorArgs)});" 
            : $"_testClass = new {className}();";

        return $@"using System;
using Xunit;
using Moq;

namespace GeneratedTests
{{
    public class {className}Tests
    {{
        private {className} _testClass;
{string.Join("\n", mockFields)}

        public {className}Tests()
        {{
{string.Join("\n", mockInitializations)}
            {testClassInstantiate}
        }}
{string.Join("\n", testMethods)}
    }}
}}";
    }
}