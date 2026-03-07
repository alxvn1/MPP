using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirectoryScanner.Models;

namespace DirectoryScanner.Services
{
    public class ScannerService
    {
        private readonly SemaphoreSlim _semaphore;

        public ScannerService(int maxThreads = 8)
        {
            _semaphore = new SemaphoreSlim(maxThreads);
        }

        public async Task<DirectoryNode> ScanAsync(string path, CancellationToken ct)
        {
            var root = new DirectoryNode { Name = Path.GetFileName(path), Path = path };
            await ProcessDirectoryAsync(root, ct);
            CalculateTotalSizes(root);
            CalculatePercentages(root, root.Size); 
            return root;
        }

        private async Task ProcessDirectoryAsync(DirectoryNode node, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;

            try
            {
                var di = new DirectoryInfo(node.Path);
                if (di.Attributes.HasFlag(FileAttributes.ReparsePoint)) return; 

                var files = di.GetFiles();
                foreach (var file in files)
                {
                    if (ct.IsCancellationRequested) break;
                    if (file.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;

                    var fileNode = new FileNode { Name = file.Name, Path = file.FullName, Size = file.Length };
                    lock (node.Children) node.Children.Add(fileNode);
                    node.Size += file.Length;
                }

                var subDirs = di.GetDirectories();
                var tasks = subDirs.Select(async sub =>
                {
                    if (sub.Attributes.HasFlag(FileAttributes.ReparsePoint)) return;

                    var subNode = new DirectoryNode { Name = sub.Name, Path = sub.FullName };
                    lock (node.Children) node.Children.Add(subNode);

                    await _semaphore.WaitAsync(ct);
                    try
                    {
                        await Task.Run(() => ProcessDirectoryAsync(subNode, ct), ct);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (UnauthorizedAccessException) { }
        }

        private long CalculateTotalSizes(DirectoryNode node)
        {
            long total = node.Size;
            foreach (var child in node.Children)
            {
                if (child is DirectoryNode subDir)
                    total += CalculateTotalSizes(subDir);
            }
            node.Size = total;
            return total;
        }

        private void CalculatePercentages(DirectoryNode node, long parentSize)
        {
            foreach (var child in node.Children)
            {
                child.Percentage = parentSize > 0 ? (double)child.Size / parentSize * 100 : 0;
                if (child is DirectoryNode subDir)
                    CalculatePercentages(subDir, node.Size);
            }
        }
    }
}