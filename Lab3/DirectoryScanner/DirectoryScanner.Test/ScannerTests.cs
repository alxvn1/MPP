using Microsoft.VisualStudio.TestTools.UnitTesting;
using DirectoryScanner.Services;
using DirectoryScanner.Models;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace DirectoryScanner.Test
{
    [TestClass]
    public class ScannerTests
    {
        [TestMethod]
        public async Task Scan_EmptyDirectory_ReturnsZeroSize()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempPath);
            var service = new ScannerService();

            try
            {
                var result = await service.ScanAsync(tempPath, CancellationToken.None);
                
                Assert.IsNotNull(result);
                Assert.AreEqual(0, result.Size);
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath);
            }
        }

        [TestMethod]
        public async Task Scan_DirectoryWithFiles_CalculatesCorrectSize()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempPath);
            var service = new ScannerService();
            
            byte[] data = new byte[100];
            File.WriteAllBytes(Path.Combine(tempPath, "file1.bin"), data);
            File.WriteAllBytes(Path.Combine(tempPath, "file2.bin"), data);

            try
            {
                var result = await service.ScanAsync(tempPath, CancellationToken.None);
                
                Assert.AreEqual(200, result.Size);
                Assert.AreEqual(2, result.Children.Count);
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }
        }

        [TestMethod]
        public async Task Scan_NestedDirectories_CalculatesTotalSize()
        {

            string rootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string subPath = Path.Combine(rootPath, "SubDir");
            Directory.CreateDirectory(subPath);
            var service = new ScannerService();

            File.WriteAllBytes(Path.Combine(rootPath, "root.bin"), new byte[50]);
            File.WriteAllBytes(Path.Combine(subPath, "sub.bin"), new byte[150]);

            try
            {

                var result = await service.ScanAsync(rootPath, CancellationToken.None);
                
                Assert.AreEqual(200, result.Size);
                
                var subDirNode = result.Children.OfType<DirectoryNode>().FirstOrDefault(d => d.Name == "SubDir");
                Assert.IsNotNull(subDirNode, "Подпапка SubDir не найдена в результатах");
                Assert.AreEqual(150, subDirNode.Size);
            }
            finally
            {
                if (Directory.Exists(rootPath))
                    Directory.Delete(rootPath, true);
            }
        }
    }
}