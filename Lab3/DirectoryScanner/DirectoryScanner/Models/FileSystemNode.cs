using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DirectoryScanner.Models
{
    public abstract class FileSystemNode : INotifyPropertyChanged
    {
        private long _size;
        private double _percentage;

        public string Name { get; set; }
        public string Path { get; set; }
        
        public long Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }

        public double Percentage
        {
            get => _percentage;
            set { _percentage = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}