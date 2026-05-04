using TestsGenerator.Library;

Console.WriteLine("Enter your path to directory with *.cs files: ");
string inputFolder = Console.ReadLine()!;

if (string.IsNullOrWhiteSpace(inputFolder) || !Directory.Exists(inputFolder))
{
    Console.WriteLine("Error: input folder doesn't exist.");
    PauseAndExit();
    return;
}

Console.WriteLine("Enter your path to output directory: ");
string outputFolder = Console.ReadLine()!;

if (string.IsNullOrWhiteSpace(outputFolder))
{
    Console.WriteLine("Error: output directory path can't be null or empty.");
    PauseAndExit();
    return;
}

var inputFiles = Directory.GetFiles(inputFolder, "*.cs", SearchOption.AllDirectories);
if (inputFiles.Length == 0)
{
    Console.WriteLine("No input files found.");
    PauseAndExit();
    return;
}

Console.WriteLine($"{inputFiles.Length} files were found! Start generating tests...");



var generator = new TestGenerator(maxRead: 2, maxGen: 4, maxWrite: 2);

await generator.GenerateTestsAsync(inputFiles, outputFolder);

Console.WriteLine("Test generation completed.");
PauseAndExit();

static void PauseAndExit()
{
    Console.WriteLine("Enter any key to exit...");
    Console.ReadKey();
}