using DiscordRPC.Logging;
using MajdataEdit.AutoSaveModule;
using MajdataEdit.ChartShare;
using MajdataEdit.SyntaxModule;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
using Python.Runtime;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Un4seen.Bass;
using Timer = System.Timers.Timer;

namespace MajdataEdit;

/// <summary>
///     MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    ErrorList errorListWindow;
    public MainWindow()
    {
        InitializeComponent();
        if (Environment.GetCommandLineArgs().Contains("--ForceSoftwareRender"))
        {
            MessageBox.Show("正在以软件渲染模式运行\nソフトウェア・レンダリング・モードで動作\nBooting as software rendering mode.");
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }
        errorListWindow = new();
        progressIndicator = new Progress<int>(value =>
        {
            Menu_ProcessStatus.Value = value;
            Debug.WriteLine(value);
            if (value >= 100) Menu_ProcessStatus.Visibility = Visibility.Collapsed;
            else Menu_ProcessStatus.Visibility = Visibility.Visible;
        });
        updateprog = progressIndicator.Report;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CheckAndStartView();

        TheWindow.Title = GetWindowsTitleString();

        SetWindowGoldenPosition();

        DCRPCclient.Logger = new ConsoleLogger { Level = LogLevel.Warning };
        DCRPCclient.Initialize();

        var handle = new WindowInteropHelper(this).Handle;
        Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_CPSPEAKERS, handle);
        InitWave();

        ReadSoundEffect();
        ReadEditorSetting();

        chartChangeTimer.Elapsed += ChartChangeTimer_Elapsed;
        chartChangeTimer.AutoReset = false;
        currentTimeRefreshTimer.Elapsed += CurrentTimeRefreshTimer_Elapsed;
        currentTimeRefreshTimer.Start();
        visualEffectRefreshTimer.Elapsed += VisualEffectRefreshTimer_Elapsed;
        waveStopMonitorTimer.Elapsed += WaveStopMonitorTimer_Elapsed;

        if (editorSetting!.AutoCheckUpdate) await CheckUpdate(true);

        //errorListWindow.ErrorListView.Items.Add(new Error(ErrorType.Info, new Position(3, 5), "666", "三个6"));
        errorListWindow.Owner = this;
        //errorListWindow.Show();

        #region 异常退出处理

        if (!SafeTerminationDetector.Of().IsLastTerminationSafe())
        {
            // 若上次异常退出，则询问打开恢复窗口
            var result = MessageBox.Show(GetLocalizedString("AbnormalTerminationInformation"),
                GetLocalizedString("Attention"), MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                var lastEditPath = File.ReadAllText(SafeTerminationDetector.Of().RecordPath).Trim();
                if (lastEditPath.Length != 0)
                    // 尝试打开上次未正常关闭的谱面 然后再打开恢复页面
                    try
                    {
                        await InitFromFile(lastEditPath);
                    }
                    catch (Exception error)
                    {
                        Console.WriteLine(error.StackTrace);
                    }

                Menu_AutosaveRecover_Click(new object(), new RoutedEventArgs());
            }
        }

        SafeTerminationDetector.Of().RecordProgramClose();

        #endregion
    }


    //start the view and wait for boot, then set window pos
    private void SetWindowPosTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        var setWindowPosTimer = (Timer)sender!;
        Dispatcher.Invoke(() => { InternalSwitchWindow(); });
        setWindowPosTimer.Stop();
        setWindowPosTimer.Dispose();
    }

    //Window events
    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!isSaved)
            if (!AskSave())
            {
                e.Cancel = true;
                return;
            }

        var process = Process.GetProcessesByName("MajdataView");
        if (process.Length > 0)
        {
            var result = MessageBox.Show(GetLocalizedString("AskCloseView"), GetLocalizedString("Attention"),
                MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
                process[0].Kill();
        }

        currentTimeRefreshTimer.Stop();
        visualEffectRefreshTimer.Stop();

        soundSetting.Close();
        //if (bpmtap != null) { bpmtap.Close(); }
        //if (muriCheck != null) { muriCheck.Close(); }
        SaveSetting();

        Bass.BASS_ChannelStop(bgmStream);
        Bass.BASS_StreamFree(bgmStream);
        Bass.BASS_ChannelStop(answerStream);
        Bass.BASS_StreamFree(answerStream);
        Bass.BASS_ChannelStop(breakStream);
        Bass.BASS_StreamFree(breakStream);
        Bass.BASS_ChannelStop(judgeExStream);
        Bass.BASS_StreamFree(judgeExStream);
        Bass.BASS_ChannelStop(hanabiStream);
        Bass.BASS_StreamFree(hanabiStream);
        Bass.BASS_Stop();
        Bass.BASS_Free();

        // 正常退出
        SafeTerminationDetector.Of().RecordProgramClose();
    }

    //Window grid events
    private void Grid_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Move;
    }

    private async void Grid_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            //Console.WriteLine(e.Data.GetData(DataFormats.FileDrop).ToString());
            if (e.Data.GetData(DataFormats.FileDrop).ToString() == "System.String[]")
            {
                var path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                if (path.ToLower().Contains("maidata.txt"))
                {
                    if (!isSaved)
                        if (!AskSave())
                            return;
                    var fileInfo = new FileInfo(path);
                    await InitFromFile(fileInfo.DirectoryName!);
                }
            }
    }

    private void FindClose_MouseDown(object sender, MouseButtonEventArgs e)
    {
        FindGrid.Visibility = Visibility.Collapsed;
        FumenContent.Focus();
    }

    #region MENU BARS

    private async void Menu_New_Click(object sender, RoutedEventArgs e)
    {
        if (!isSaved)
            if (!AskSave())
                return;
        var openFileDialog = new OpenFileDialog
        {
            Filter = "track.mp3, track.ogg|track.mp3;track.ogg"
        };
        if ((bool)openFileDialog.ShowDialog()!)
        {
            var fileInfo = new FileInfo(openFileDialog.FileName);
            CreateNewFumen(fileInfo.DirectoryName!);
            await InitFromFile(fileInfo.DirectoryName!);
        }
    }

    private async void Menu_Open_Click(object sender, RoutedEventArgs e)
    {
        if (!isSaved)
            if (!AskSave())
                return;
        var openFileDialog = new OpenFileDialog
        {
            Filter = "maidata.txt|maidata.txt"
        };
        if ((bool)openFileDialog.ShowDialog()!)
        {
            var fileInfo = new FileInfo(openFileDialog.FileName);
            await InitFromFile(fileInfo.DirectoryName!);
        }
    }

    private void Menu_Save_Click(object sender, RoutedEventArgs e)
    {
        SaveFumen(true);
        SystemSounds.Beep.Play();
    }

    private void Menu_SaveAs_Click(object sender, RoutedEventArgs e)
    {
    }

    private void Menu_ExportRender_Click(object sender, RoutedEventArgs e)
    {
        TogglePlayAndPause(PlayMethod.Record);
    }
    private async void Menu_ToggleChartShare_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ToggleChartShare(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(GetLocalizedString("ToggleShareFail"), ex.Message + ex.InnerException?.Message), GetLocalizedString("Error"));
            _client = null;
            return;
        }
    }
    private async void Menu_ConnectChartShare_Click(object sender, RoutedEventArgs e)
    {
        if (ShareMode)
        {
            await DisconnectToChartServer();
            Menu_ConnectChartShare.Header = GetLocalizedString("ConnectChartShare");
        }
        else
        {
            var window = new ConnectShare(async (ip, port) =>
            {
                if (await ConnectToChartServer(ip, port))
                    await Dispatcher.InvokeAsync(() =>
                        Menu_ConnectChartShare.Header = GetLocalizedString("DisconnectChartShare"));
                else return;
            })
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };
            window.ShowDialog();
        }
    }

    private void Menu_CloseChart_Click(object sender, RoutedEventArgs e)
    {
        ClearWindow();
    }

    private void MirrorLeftRight_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        ApplyMirror(Mirror.HandleType.LRMirror);
    }

    private void MirrorUpDown_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        ApplyMirror(Mirror.HandleType.UDMirror);
    }

    private void Mirror180_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        ApplyMirror(Mirror.HandleType.HalfRotation);
    }

    private void Mirror45_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        ApplyMirror(Mirror.HandleType.Rotation45);
    }

    private void MirrorCcw45_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        ApplyMirror(Mirror.HandleType.CcwRotation45);
    }

    private void BPMtap_MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var tap = new BPMtap();
        tap.Owner = this;
        tap.Show();
    }

    private void MenuItem_InfomationEdit_Click(object? sender, RoutedEventArgs e)
    {
        var infoWindow = new Infomation();
        SetSavedState(false);
        infoWindow.ShowDialog();
        TheWindow.Title = GetWindowsTitleString(SimaiProcess.title!);
    }

    private void MenuItem_Majnet_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://majdata.net", UseShellExecute = true });
        //maidata.txtの譜面書式
    }

    private void MenuItem_GitHub_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo() { FileName = "https://github.com/re-poem/MajdataViewX", UseShellExecute = true });
    }

    private void MenuItem_SoundSetting_Click(object? sender, RoutedEventArgs e)
    {
        soundSetting = new SoundSetting
        {
            Owner = this
        };
        soundSetting.ShowDialog();
    }

    private void MuriCheck_Click_1(object? sender, RoutedEventArgs e)
    {
        var muriCheck = new MuriCheck
        {
            Owner = this
        };
        muriCheck.Show();
    }
    private void SyntaxCheckButton_Click(object sender, RoutedEventArgs e)
    {
#if DEBUG
        SyntaxChecker.ScanAsync(GetRawFumenText()).ContinueWith(t =>
        {
            SetErrCount(SyntaxChecker.GetErrorCount());
            Dispatcher.Invoke(() => { ShowSyntaxError(); });
        });
#else
        try
        {
            SyntaxChecker.ScanAsync(GetRawFumenText()).ContinueWith(t =>
            {
                SetErrCount(SyntaxChecker.GetErrorCount());
                Dispatcher.Invoke(() => { ShowSyntaxError(); });
            });
        }
        catch
        {
            SetErrCount(GetLocalizedString("InternalErr"));
        }
#endif
    }
    private void MaiMuriDXButton_Click(object sender, RoutedEventArgs e)
    {
        LaunchMaiMuriDX window = new(new RunArg(GetRawFumenText(), float.Parse(OffsetTextBox.Text), audioDir, false));
        window.Owner = this;
        window.Show();
    }
    public void ShowMuriDXError(LaunchMaiMuriDX lmmdWindow)
    {
        SyntaxCheck();
        for (int i = errorListWindow.ErrorListView.Items.Count - 1; i >= 0; i--)
        {
            if ((errorListWindow.ErrorListView.Items[i] as Error)!.Type is ErrorType.MuriDXS or ErrorType.MuriDXD)
            {
                errorListWindow.ErrorListView.Items.RemoveAt(i);
            }
        }
        var errList = lmmdWindow.ErrorList;
        errList.ForEach(e =>
        {
            errorListWindow.ErrorListView.Items.Add(e);
        });
        if (errorListWindow.IsVisible) errorListWindow.Activate();
        else errorListWindow.Show();
        if (errList.Count >= 100 && editorSetting!.Language == "zh-CN")
        {
            MessageBox.Show("我将删除你的Majdata。");
        }
    }
    void ShowSyntaxError()
    {
        SyntaxCheck();
        for (int i = errorListWindow.ErrorListView.Items.Count - 1; i >= 0; i--)
        {
            if ((errorListWindow.ErrorListView.Items[i] as Error)!.Type == ErrorType.Syntax)
            {
                errorListWindow.ErrorListView.Items.RemoveAt(i);
            }
        }
        var errListCopy = SyntaxChecker.ErrorList.ToList();
        errListCopy.ForEach(e =>
        {
            errorListWindow.ErrorListView.Items.Add(e);
        });
        if (errorListWindow.IsVisible) errorListWindow.Activate();
        else errorListWindow.Show();
    }
    private void SyntaxCheckButton_Click(object sender, MouseButtonEventArgs e)
    {
#if DEBUG
        SyntaxChecker.ScanAsync(GetRawFumenText()).ContinueWith(t =>
        {
            SetErrCount(SyntaxChecker.GetErrorCount());
            Dispatcher.Invoke(() => { ShowSyntaxError(); });
        });
#else
        try
        {
            SyntaxChecker.ScanAsync(GetRawFumenText()).ContinueWith(t =>
            {
                SetErrCount(SyntaxChecker.GetErrorCount());
                Dispatcher.Invoke(() => { ShowSyntaxError(); });
            });
        }
        catch
        {
            SetErrCount(GetLocalizedString("InternalErr"));
        }
#endif
    }
    private void MenuItem_EditorSetting_Click(object? sender, RoutedEventArgs e)
    {
        var esp = new EditorSettingPanel
        {
            Owner = this
        };
        esp.ShowDialog();
    }

    private void Menu_ResetViewWindow(object? sender, RoutedEventArgs e)
    {
        if (CheckAndStartView()) return;
        InternalSwitchWindow();
    }

    private void MenuFind_Click(object? sender, RoutedEventArgs e)
    {
        ToggleFindGrid();
    }

    private async void CheckUpdate_Click(object? sender, RoutedEventArgs e)
    {
        await CheckUpdate();
    }

    private void Menu_AutosaveRecover_Click(object? sender, RoutedEventArgs e)
    {
        var asr = new AutoSaveRecover
        {
            Owner = this
        };
        asr.ShowDialog();
    }

    #endregion

    #region 快捷键

    private void PlayAndPause_CanExecute(object? sender, CanExecuteRoutedEventArgs e) //快捷键
    {
        TogglePlayAndStop();
    }

    private void StopPlaying_CanExecute(object? sender, CanExecuteRoutedEventArgs e) //快捷键
    {
        TogglePlayAndPause();
    }

    private void SaveFile_Command_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        SaveFumen(true);
        SystemSounds.Beep.Play();
    }

    private void SendToView_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        TogglePlayAndStop(PlayMethod.Op);
    }

    private void IncreasePlaybackSpeed_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        SetPlaybackSpeedDiff(1);
    }

    private void DecreasePlaybackSpeed_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        SetPlaybackSpeedDiff(-1);
    }

    private void FindCommand_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        ToggleFindGrid();
    }

    private void MirrorLRCommand_CanExecute(object? sender, CanExecuteRoutedEventArgs e)
    {
        ApplyMirror(Mirror.HandleType.LRMirror);
    }

    private void MirrorUDCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        ApplyMirror(Mirror.HandleType.UDMirror);
    }

    private void Mirror180Command_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        ApplyMirror(Mirror.HandleType.HalfRotation);
    }

    private void Mirror45Command_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        ApplyMirror(Mirror.HandleType.Rotation45);
    }

    private void MirrorCcw45Command_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        ApplyMirror(Mirror.HandleType.CcwRotation45);
    }

    #endregion

    #region Left componients

    private void PlayAndPauseButton_Click(object sender, RoutedEventArgs e)
    {
        TogglePlayAndPause();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleStop();
    }

    private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var i = LevelSelector.SelectedIndex;
        LoadRawFumenText(SimaiProcess.fumens[i]);
        selectedDifficulty = i;
        LevelTextBox.Text = SimaiProcess.levels[selectedDifficulty];
        SetSavedState(true);
        await SimaiProcess.Serialize(GetRawFumenText());
        DrawWave();
        SyntaxCheck();
    }

    private void LevelTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SetSavedState(false);
        if (selectedDifficulty == -1) return;
        SimaiProcess.levels[selectedDifficulty] = LevelTextBox.Text;
    }

    private async void OffsetTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SetSavedState(false);
        try
        {
            SimaiProcess.first = float.Parse(OffsetTextBox.Text);
            await SimaiProcess.Serialize(GetRawFumenText());
            DrawWave();
        }
        catch
        {
            SimaiProcess.first = 0f;
        }
    }

    private void OffsetTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var offset = float.Parse(OffsetTextBox.Text);
        offset += e.Delta > 0 ? 0.01f : -0.01f;
        OffsetTextBox.Text = offset.ToString();
    }

    private void FollowPlayCheck_Click(object sender, RoutedEventArgs e)
    {
        FumenContent.Focus();
    }

    private void Op_Button_Click(object sender, RoutedEventArgs e)
    {
        TogglePlayAndStop(PlayMethod.Op);
    }

    private void SettingLabel_MouseUp(object sender, MouseButtonEventArgs e)
    {
        // 单击设置的时候也可以进入设置界面
        var esp = new EditorSettingPanel();
        esp.Owner = this;
        esp.ShowDialog();
    }
    #endregion

    #region RichTextbox events

    private async void FumenContent_SelectionChanged(object sender, RoutedEventArgs e)
    {
        NoteNowText.Content = 
            (FumenContent.Text[..FumenContent.CaretIndex] //.Replace("\r", "") //没区别
                                      .Count(o => o == '\n') + 1) + " 行";
        if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING && (bool)FollowPlayCheck.IsChecked!)
            return;
        //TODO:这个应该换成用fumen text position来在已经serialized的timinglist里面找。。 然后直接去掉这个double的返回和position的入参。。。
        var time = await SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition());

        //按住Ctrl，同时按下鼠标左键/上下左右方向键时，才改变进度，其他包含Ctrl的组合键不影响进度。
        if (Keyboard.Modifiers == ModifierKeys.Control && (
                Mouse.LeftButton == MouseButtonState.Pressed ||
                Keyboard.IsKeyDown(Key.Left) ||
                Keyboard.IsKeyDown(Key.Right) ||
                Keyboard.IsKeyDown(Key.Up) ||
                Keyboard.IsKeyDown(Key.Down)
            ))
        {
            if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING)
                TogglePause();
            SetBgmPosition(time);
        }

        //Console.WriteLine("SelectionChanged: " + GetRawFumenPosition());
        SimaiProcess.ClearNoteListPlayedState();
        ghostCusorPositionTime = (float)time;
        if (!isPlaying) DrawWave();
        findPosition = FumenContent.CaretIndex; //点击时刷新一下

        if (ShareMode && !_isRemoteUpdate)
        {
            await _client!.InvokeAsync(nameof(ChartHub.Moving), GetRawFumenPosition());
        }
    }

    private async void FumenContent_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (GetRawFumenText() == "" || isLoading) return;
        SetSavedState(false);
        await SyncChartServer(); //立马同步，用了diff的原因，没那么卡

        //间隔太小了不用管 话说为什么是33。
        //if (chartChangeTimer.Interval < 33)
        //{
        //    SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition());
        //    DrawWave();
        //    return;
        //}

        //私以为没必要 真的有人注意过铺面刷新延迟吗。
        chartChangeTimer.Stop();
        chartChangeTimer.Start();
    }

    private void FumenContent_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // 按下Insert键，同时未按下任何组合键，切换覆盖模式
        if (e.Key == Key.Insert && Keyboard.Modifiers == ModifierKeys.None)
        {
            SwitchFumenOverwriteMode();
            e.Handled = true;
        }
    }

    private void FumenContent_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        RenderAllCursors();
    }

    #endregion

    #region Wave displayer

    private void WaveViewZoomIn_Click(object sender, RoutedEventArgs e)
    {
        if (deltatime > 1)
            deltatime -= 1;
        DrawWave();
        FumenContent.Focus();
    }

    private void WaveViewZoomOut_Click(object sender, RoutedEventArgs e)
    {
        if (deltatime < 10)
            deltatime += 1;
        DrawWave();
        FumenContent.Focus();
    }

    private void MusicWave_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        ScrollWave(-e.Delta);
    }

    private void MusicWave_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        lastMousePointX = e.GetPosition(this).X;
    }

    private void MusicWave_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var delta = e.GetPosition(this).X - lastMousePointX;
            lastMousePointX = e.GetPosition(this).X;
            ScrollWave(-delta);
        }

        lastMousePointX = e.GetPosition(this).X;
    }

    private void MusicWave_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        InitWave();
        DrawWave();
    }



    #endregion
}