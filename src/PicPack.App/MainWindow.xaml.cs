using PicPack.App.Models;
using PicPack.App.Services;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace PicPack.App;

public partial class MainWindow : Window
{
    private readonly FolderDistributorService _folderDistributorService = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private int _previewVersion;
    private bool _isInitialized;
    private bool _isRunning;

    public MainWindow()
    {
        InitializeComponent();
        _isInitialized = true;
        _ = RefreshPreviewAsync();
    }

    private void SelectInputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedPath = SelectFolder("入力フォルダを選択", InputFolderTextBox.Text);
        if (selectedPath is null)
        {
            return;
        }

        InputFolderTextBox.Text = selectedPath;
        _ = RefreshPreviewAsync();
    }

    private void SelectOutputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedPath = SelectFolder("出力フォルダを選択", OutputFolderTextBox.Text);
        if (selectedPath is null)
        {
            return;
        }

        OutputFolderTextBox.Text = selectedPath;
        _ = RefreshPreviewAsync();
    }

    private async void OptionsChanged(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        await RefreshPreviewAsync();
    }

    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            return;
        }

        if (!TryCreateOptions(out var options))
        {
            return;
        }

        LogTextBox.Clear();
        ResultTextBlock.Text = "確認中です";
        AppendLog("対象ファイル検索開始");
        IReadOnlyList<FileInfo> images;
        try
        {
            images = await Task.Run(() => new ImageFileFinder().FindImages(options.InputFolder));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show(this, exception.Message, "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (images.Count == 0)
        {
            MessageBox.Show(this, "対象画像がありません。", "確認", MessageBoxButton.OK, MessageBoxImage.Information);
            TargetImageCountTextBlock.Text = "0";
            PlannedFolderCountTextBlock.Text = "0";
            return;
        }

        if (IsSameFolder(options.InputFolder, options.OutputFolder))
        {
            var sameFolderAnswer = MessageBox.Show(
                this,
                "入力フォルダと出力フォルダが同じです。続行しますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (sameFolderAnswer != MessageBoxResult.Yes)
            {
                return;
            }
        }

        var plannedFolderCount = CalculatePlannedFolderCount(images.Count, options.FilesPerFolder);
        var confirmationMessage =
            $"対象画像枚数: {images.Count}\n" +
            $"作成予定フォルダ数: {plannedFolderCount}\n" +
            $"1フォルダあたりの枚数: {options.FilesPerFolder}\n" +
            $"処理方式: {GetModeLabel(options.Mode)}\n" +
            $"入力フォルダ: {options.InputFolder}\n" +
            $"出力フォルダ: {options.OutputFolder}";

        var confirmation = MessageBox.Show(
            this,
            confirmationMessage,
            "実行前確認",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.OK)
        {
            return;
        }

        await RunDistributionAsync(options, images.Count);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        AppendLog("キャンセル要求を受け付けました");
    }

    private async Task RunDistributionAsync(DistributionOptions options, int totalCount)
    {
        SetRunningState(isRunning: true);
        ResultTextBlock.Text = "処理中です";
        ProgressBar.Maximum = Math.Max(totalCount, 1);
        ProgressBar.Value = 0;

        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            var progress = new Progress<DistributionProgress>(progress =>
            {
                ProgressBar.Maximum = Math.Max(progress.TotalCount, 1);
                ProgressBar.Value = Math.Min(progress.ProcessedCount, ProgressBar.Maximum);
                AppendLog(progress.Message);
            });

            var result = await _folderDistributorService.DistributeAsync(
                options,
                progress,
                _cancellationTokenSource.Token);

            ProgressBar.Value = Math.Min(result.ProcessedFiles + result.SkippedFiles, ProgressBar.Maximum);

            if (result.IsCanceled)
            {
                ResultTextBlock.Text =
                    $"キャンセルされました\n処理済み: {result.ProcessedFiles} 件\nスキップ: {result.SkippedFiles} 件";
                StatusTextBlock.Text = "キャンセルされました";
            }
            else
            {
                ResultTextBlock.Text =
                    $"完了しました\n対象画像: {result.TotalFiles} 件\n成功: {result.ProcessedFiles} 件\nスキップ: {result.SkippedFiles} 件\n作成フォルダ: {result.CreatedFolderCount} 件";
                StatusTextBlock.Text = "完了しました";
            }

            if (result.Errors.Count > 0)
            {
                AppendLog("エラー一覧");
                foreach (var error in result.Errors)
                {
                    AppendLog($"{Path.GetFileName(error.SourcePath)}: {error.Message}");
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            ResultTextBlock.Text = "処理できませんでした";
            AppendLog($"エラー: {exception.Message}");
            MessageBox.Show(this, exception.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            SetRunningState(isRunning: false);
            await RefreshPreviewAsync();
        }
    }

    private async Task RefreshPreviewAsync()
    {
        var version = ++_previewVersion;
        SelectedModeTextBlock.Text = GetModeLabel(GetSelectedMode());

        if (_isRunning)
        {
            return;
        }

        if (!Directory.Exists(InputFolderTextBox.Text) || !TryReadFilesPerFolder(out var filesPerFolder))
        {
            TargetImageCountTextBlock.Text = "-";
            PlannedFolderCountTextBlock.Text = "-";
            return;
        }

        try
        {
            var count = await Task.Run(() => _folderDistributorService.CountTargetImages(InputFolderTextBox.Text));
            if (version != _previewVersion)
            {
                return;
            }

            TargetImageCountTextBlock.Text = count.ToString();
            PlannedFolderCountTextBlock.Text = CalculatePlannedFolderCount(count, filesPerFolder).ToString();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            if (version != _previewVersion)
            {
                return;
            }

            TargetImageCountTextBlock.Text = "-";
            PlannedFolderCountTextBlock.Text = "-";
            StatusTextBlock.Text = exception.Message;
        }
    }

    private bool TryCreateOptions(out DistributionOptions options)
    {
        options = new DistributionOptions
        {
            InputFolder = InputFolderTextBox.Text,
            OutputFolder = OutputFolderTextBox.Text,
            FilesPerFolder = 1,
            Mode = GetSelectedMode()
        };

        if (string.IsNullOrWhiteSpace(InputFolderTextBox.Text) || !Directory.Exists(InputFolderTextBox.Text))
        {
            MessageBox.Show(this, "入力フォルダを選択してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(OutputFolderTextBox.Text) || !Directory.Exists(OutputFolderTextBox.Text))
        {
            MessageBox.Show(this, "出力フォルダを選択してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!TryReadFilesPerFolder(out var filesPerFolder))
        {
            MessageBox.Show(this, "1フォルダあたりの枚数は1以上の整数で入力してください。", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        options = new DistributionOptions
        {
            InputFolder = InputFolderTextBox.Text,
            OutputFolder = OutputFolderTextBox.Text,
            FilesPerFolder = filesPerFolder,
            Mode = GetSelectedMode()
        };
        return true;
    }

    private bool TryReadFilesPerFolder(out int filesPerFolder)
    {
        return int.TryParse(FilesPerFolderTextBox.Text, out filesPerFolder) && filesPerFolder >= 1;
    }

    private DistributionMode GetSelectedMode()
    {
        return MoveRadioButton.IsChecked == true ? DistributionMode.Move : DistributionMode.Copy;
    }

    private static int CalculatePlannedFolderCount(int imageCount, int filesPerFolder)
    {
        if (imageCount <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(imageCount / (double)filesPerFolder);
    }

    private static string GetModeLabel(DistributionMode mode)
    {
        return mode == DistributionMode.Copy ? "コピー" : "移動";
    }

    private static string? SelectFolder(string description, string currentPath)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true
        };

        if (Directory.Exists(currentPath))
        {
            dialog.SelectedPath = currentPath;
        }

        return dialog.ShowDialog() == Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    private void SetRunningState(bool isRunning)
    {
        _isRunning = isRunning;
        RunButton.IsEnabled = !isRunning;
        CancelButton.IsEnabled = isRunning;
        StatusTextBlock.Text = isRunning ? "処理中です" : StatusTextBlock.Text;
    }

    private void AppendLog(string message)
    {
        LogTextBox.AppendText($"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
    }

    private static bool IsSameFolder(string firstPath, string secondPath)
    {
        var first = Path.GetFullPath(firstPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var second = Path.GetFullPath(secondPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }
}
