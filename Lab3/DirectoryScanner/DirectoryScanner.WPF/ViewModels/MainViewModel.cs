using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DirectoryScanner.Models;
using DirectoryScanner.Services;
using System.Windows.Forms;

namespace DirectoryScanner.WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private DirectoryNode _root;
        private bool _isScanning;
        private CancellationTokenSource _cts;
        private readonly ScannerService _scannerService;

        public DirectoryNode Root
        {
            get => _root;
            set { _root = value; OnPropertyChanged(); }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set { _isScanning = value; OnPropertyChanged(); }
        }

        public ICommand SelectFolderCommand { get; }
        public ICommand CancelCommand { get; }

        public MainViewModel()
        {
            _scannerService = new ScannerService(10);
            SelectFolderCommand = new RelayCommand(async param => await StartScan());
            CancelCommand = new RelayCommand(param => _cts?.Cancel(), param => IsScanning);
        }

        private async Task StartScan()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    IsScanning = true;
                    _cts = new CancellationTokenSource();
                    try
                    {
                        Root = await _scannerService.ScanAsync(dialog.SelectedPath, _cts.Token);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        IsScanning = false;
                    }
                }
            }
        }
    }
}