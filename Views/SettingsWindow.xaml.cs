using System.Windows;
using Raffe.ViewModels;
using Microsoft.Win32;
using Raffe.Models;
using Raffe.Services;

namespace Raffe.Views;

public partial class SettingsWindow : Window
{
    private readonly DataService _dataService;
    private readonly ExcelImportService _excelService;
    private readonly SettingsViewModel _vm;

    public SettingsWindow(DataService dataService, ExcelImportService excelService)
    {
        InitializeComponent();
        _dataService = dataService;
        _excelService = excelService;
        _vm = new SettingsViewModel(dataService, excelService);
        DataContext = _vm;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        _vm.Save();
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ClearDefaultMusic(object sender, RoutedEventArgs e)  => _vm.DefaultMusicPath  = "";
    private void ClearSpinningMusic(object sender, RoutedEventArgs e) => _vm.SpinningMusicPath = "";
    private void ClearWinnerMusic(object sender, RoutedEventArgs e)   => _vm.WinnerMusicPath   = "";
}
