using System.Collections.ObjectModel;

namespace DirectoryScanner.Models
{
    public class DirectoryNode : FileSystemNode
    {
        public ObservableCollection<FileSystemNode> Children { get; } = new ObservableCollection<FileSystemNode>();
    }
}