using DiffMatchPatch;
using DiscordRPC;
using MajdataEdit.AutoSaveModule;
using MajdataEdit.ChartShare;
using MajdataEdit.SyntaxModule;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Semver;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Fx;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Extensions;
using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;
using DashStyle = System.Drawing.Drawing2D.DashStyle;
using Font = System.Drawing.Font;
using LinearGradientBrush = System.Drawing.Drawing2D.LinearGradientBrush;
using Pen = System.Drawing.Pen;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Windows.Point;
using Timer = System.Timers.Timer;

namespace MajdataEdit;

public partial class MainWindow : Window
{
    private bool ShareMode = false;
    private bool isHost = false;
    private string originMaidataDir = ""; //ShareMode && Host Only

    private const string majSettingFilename = "majSetting.json";
    private const string editorSettingFilename = "EditorSetting.json";
    public static readonly string MAJDATA_VERSION_STRING = $"v{Assembly.GetExecutingAssembly().GetName().Version!.ToString(3)}";
    public static readonly SemVersion MAJDATA_VERSION = SemVersion.Parse(MAJDATA_VERSION_STRING, SemVersionStyles.Any);

    public static string maidataDir = "";
    public static string audioDir = "";
    public static bool useOgg = false;

    //float[] wavedBs;
    private readonly short[][] waveRaws = new short[3][];
    public Timer chartChangeTimer = new(1000); // 谱面变更延迟解析
    private readonly Timer currentTimeRefreshTimer = new(100);

    public DiscordRpcClient DCRPCclient = new("1068882546932326481");

    private float deltatime = 4f;
    public EditorSetting? editorSetting;

    private bool fumenOverwriteMode; //谱面文本覆盖模式
    private float ghostCusorPositionTime;
    private bool isDrawing;
    private bool isLoading;
    private bool isReplaceConformed;

    private bool isSaved = true;
    private EditorControlMethod lastEditorState;
    private int findPosition;
    private int lastFindPosition;

    private double lastMousePointX; //Used for drag scroll

    private int selectedDifficulty = -1;
    private double songLength;

    private SoundSetting soundSetting = new();
    private bool UpdateCheckLock;

    public float originFreq = 44100f;

    //*UI DRAWING
    private readonly Timer visualEffectRefreshTimer = new(1);

    private WriteableBitmap? WaveBitmap;

    //Chart Share
    private HubConnection? _client;    // 客户端对象
    private bool _isRemoteUpdate = false; // 防死循环的锁
    private string _shadowText = ""; // 谱面文本同步缓冲
    private diff_match_patch _dmp = new();
    private readonly byte[] certBytes = new byte[] {
    48, 130, 2, 10, 2, 130, 2, 1, 0, 162, 108, 70, 147, 186, 10, 181,
    148, 40, 72, 232, 165, 67, 36, 174, 170, 138, 116, 59, 92, 233, 84, 241,
    49, 216, 212, 68, 156, 171, 157, 12, 59, 223, 168, 166, 169, 201, 231, 90,
    168, 64, 77, 121, 36, 195, 139, 44, 219, 231, 132, 213, 156, 181, 59, 109,
    251, 129, 55, 125, 135, 239, 156, 91, 62, 3, 9, 48, 201, 202, 138, 148,
    189, 125, 111, 121, 243, 8, 109, 45, 48, 192, 151, 25, 170, 181, 104, 215,
    26, 23, 136, 228, 162, 11, 122, 94, 27, 172, 168, 46, 159, 23, 183, 115,
    172, 222, 80, 196, 48, 54, 206, 188, 202, 133, 149, 196, 92, 203, 204, 188,
    117, 221, 23, 229, 162, 179, 96, 194, 249, 244, 85, 108, 97, 12, 150, 13,
    6, 9, 212, 110, 114, 78, 139, 23, 172, 93, 238, 197, 24, 151, 165, 190,
    52, 100, 168, 36, 174, 205, 96, 224, 251, 30, 254, 237, 112, 71, 191, 118,
    159, 159, 4, 98, 35, 229, 39, 252, 184, 15, 225, 252, 4, 148, 117, 125,
    128, 3, 65, 116, 39, 253, 248, 137, 45, 52, 247, 162, 30, 213, 144, 157,
    108, 32, 78, 28, 195, 143, 241, 223, 215, 97, 40, 80, 117, 75, 187, 108,
    108, 80, 116, 90, 199, 215, 201, 153, 10, 83, 0, 166, 40, 15, 12, 98,
    140, 162, 23, 33, 83, 81, 40, 15, 69, 144, 106, 201, 118, 202, 9, 227,
    242, 16, 128, 196, 123, 81, 187, 86, 43, 206, 138, 1, 230, 112, 6, 54,
    237, 147, 5, 192, 42, 30, 162, 189, 125, 227, 74, 81, 37, 47, 12, 108,
    182, 237, 42, 95, 11, 211, 252, 43, 124, 95, 75, 35, 27, 49, 209, 219,
    165, 191, 217, 99, 195, 97, 77, 102, 243, 5, 100, 39, 249, 65, 71, 181,
    162, 27, 208, 112, 84, 41, 243, 232, 164, 153, 211, 167, 121, 93, 172, 210,
    9, 132, 47, 235, 114, 216, 116, 103, 117, 232, 97, 60, 132, 12, 11, 83,
    191, 220, 174, 6, 106, 158, 107, 43, 143, 206, 125, 75, 161, 26, 254, 249,
    149, 94, 194, 54, 98, 230, 73, 194, 15, 160, 186, 52, 118, 224, 9, 137,
    43, 212, 252, 47, 171, 160, 156, 90, 96, 222, 79, 234, 211, 10, 115, 55,
    249, 124, 187, 4, 133, 170, 184, 58, 31, 50, 179, 156, 40, 156, 65, 210,
    203, 176, 152, 41, 67, 10, 164, 79, 194, 12, 133, 9, 199, 82, 21, 86,
    239, 42, 46, 244, 14, 82, 8, 125, 222, 31, 208, 229, 102, 147, 194, 29,
    22, 111, 79, 28, 90, 218, 150, 24, 154, 38, 17, 160, 29, 180, 74, 57,
    203, 161, 86, 240, 84, 41, 63, 106, 8, 163, 23, 55, 143, 65, 192, 4,
    150, 161, 20, 51, 103, 55, 162, 250, 184, 38, 134, 4, 253, 50, 223, 101,
    90, 153, 38, 20, 40, 110, 201, 51, 178, 34, 39, 188, 47, 210, 250, 31,
    212, 43, 205, 157, 251, 63, 121, 199, 5, 2, 3, 1, 0, 1};
    private Dictionary<string, RemoteCursor> _cursors = new();

    private IProgress<int> progressIndicator;
    private Action<int> updateprog;

    //*TEXT CONTROL
    //内部完全用不含\r的文本来处理，牺牲一点性能换取牺牲一点可读性（bushi

    private string GetRawFumenText() => FumenContent.Text.Replace("\r", "");
    private void SetRawFumenText(string content) => FumenContent.Text = content.Replace("\r", "");
    //不触发TextChanged
    private void LoadRawFumenText(string content)
    {
        isLoading = true;

        if (content == null)
        {
            isLoading = false;
            FumenContent.Text = "";
            return;
        }

        FumenContent.Text = content.Replace("\r", "");

        isLoading = false;
    }
    private int GetRawFumenPosition() => ToRawFumenPosition(FumenContent.CaretIndex);
    private void SetRawFumenPosition(int position) => FumenContent.Select(ToUiIndex(position), 0);

    private void SetRawFumenPosition(int positionX, int positionY)
    {
        if (positionX < 0 || positionY < 0)
        {
            FumenContent.CaretIndex = FumenContent.Text.Length - 1;
            return;
        }
        var text = FumenContent.Text;
        int currentLine = 0;
        int currentIndex = 0;
        int length = text.Length;

        while (currentLine < positionY && currentIndex <= length)
        {
            if (text[currentIndex] == '\n')
                currentLine++;
            currentIndex++;
        }

        int finalIndex = currentIndex + positionX;

        FumenContent.CaretIndex = finalIndex;
    }

    private int ToRawFumenPosition(int uiIndex)
    {
        string text = FumenContent.Text;

        if (string.IsNullOrEmpty(text)) return 0;
        if (uiIndex > text.Length) uiIndex = text.Length;
        if (uiIndex < 0) return 0;

        int rCount = 0;
        // 遍历到当前光标位置，统计有多少个 \r
        for (int i = 0; i < uiIndex; i++)
        {
            if (text[i] == '\r')
            {
                rCount++;
            }
        }

        return uiIndex - rCount;
    }

    private int ToUiIndex(int rawFumenPosition)
    {
        string text = FumenContent.Text;

        if (string.IsNullOrEmpty(text) || rawFumenPosition < 0) return 0;

        int currentRawCount = 0;
        int uiIndex = 0;

        while (uiIndex < text.Length && currentRawCount < rawFumenPosition)
        {
            // 如果遇到 \r，只增加 UI 索引（跳过它），逻辑计数不变
            if (text[uiIndex] == '\r')
                uiIndex++;
            else
            {
                // 普通字符（包括 \n）：UI 走一步，逻辑计数也走一步
                uiIndex++;
                currentRawCount++;
            }
        }

        return uiIndex;
    }

    private void SeekTextFromTime()
    {
        //Console.WriteLine("SeekText");
        var time = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
        var timingList = new List<SimaiTimingPoint>();
        timingList.AddRange(SimaiProcess.timinglist);
        var noteList = SimaiProcess.notelist;
        if (SimaiProcess.timinglist.Count <= 0) return;
        timingList.Sort((x, y) => Math.Abs(time - x.time).CompareTo(Math.Abs(time - y.time)));
        var theNote = timingList[0];
        timingList.Clear();
        timingList.AddRange(SimaiProcess.timinglist);
        //var indexOfTheNote = timingList.IndexOf(theNote);
        SetRawFumenPosition(theNote.rawTextPositionX - 1, theNote.rawTextPositionY);
    }

    private void SeekTextFromIndex(int noteGroupIndex)
    {
        if (SimaiProcess.notelist.Count > noteGroupIndex + 1 && noteGroupIndex >= 0)
        {
            var theNote = SimaiProcess.notelist[noteGroupIndex];
            SetRawFumenPosition(theNote.rawTextPositionX - 1, theNote.rawTextPositionY);
        }
    }

    public async void ScrollToFumenContentSelection(int positionX, int positionY)
    {
        // 这玩意用于其他窗口来滚动Scroll 因为涉及到好多变量都是private的
        SetRawFumenPosition(positionX, positionY);
        FumenContent.Focus();
        Focus();

        if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING && (bool)FollowPlayCheck.IsChecked!)
            return;
        var time = await SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition());
        SetBgmPosition(time);
        //Console.WriteLine("SelectionChanged");
        SimaiProcess.ClearNoteListPlayedState();
        ghostCusorPositionTime = (float)time;
    }

    //*FIND AND REPLACE
    private void Find_icon_MouseDown(object? sender, MouseButtonEventArgs e)
    {
        FindAndScroll();
    }

    private void Replace_icon_MouseDown(object? sender, MouseButtonEventArgs e)
    {
        if (!isReplaceConformed)
        {
            FindAndScroll();
            return;
        }

        if (FumenContent.SelectionStart == lastFindPosition)
        {
            FumenContent.SelectedText = ReplaceText.Text;
            FindAndScroll();
        }
        else
        {
            isReplaceConformed = false;
        }
    }

    public void FindAndScroll()
    {
        string content = FumenContent.Text; //这里完全依赖UI元素，不管Raw是什么了
        string keyword = InputText.Text;

        // 为空
        if (string.IsNullOrEmpty(keyword)) return;
        // 防止 lastFindPosition 越界（比如文本被删除变短了）
        if (findPosition >= content.Length) findPosition = 0;
        // 下一个
        int position = content.IndexOf(keyword, findPosition);
        // 没有下一个了
        if (position == -1 && findPosition > 0) position = content.IndexOf(keyword, 0);
        //彻底没找到
        if (position == -1)
        {
            isReplaceConformed = false;
            findPosition = 0; 
            return;
        }

        FumenContent.Select(position, keyword.Length);
        lastFindPosition = position;
        findPosition = position + keyword.Length;
        FumenContent.Focus();

        isReplaceConformed = true;
    }

    //*FILE CONTROL

    private async void ClearWindow()
    {
        if (!isSaved)
            if (!AskSave())
            {
                return;
            }

        if (Cover.Visibility != Visibility.Visible)
            ((Storyboard)Resources["CoverShow"]).Begin();

        if (ShareMode) await ToggleChartShare();

        SaveSetting();

        soundSetting?.Close();
        audioDir = "";
        maidataDir = "";
        //SetRawFumenText("");
        FumenContent.Clear();
        SimaiProcess.ClearData();
        LevelSelector.SelectedItem = "";
        OffsetTextBox.Text = "";

        // Stop
        Op_Button.IsEnabled = true;
        isPlaying = false;
        isPlan2Stop = false;
        FumenContent.Focus();
        PlayAndPauseButton.Content = "▶";
        Bass.BASS_ChannelStop(bgmStream);
        Bass.BASS_ChannelStop(holdRiserStream);
        //soundEffectTimer.Stop();
        waveStopMonitorTimer.Stop();
        visualEffectRefreshTimer.Stop();
        sendRequestStop();

        //Cover.Visibility = Visibility.Visible;
        MenuEdit.IsEnabled = false;
        VolumnSetting.IsEnabled = false;
        MenuMuriCheck.IsEnabled = false;
        Menu_ExportRender.IsEnabled = false;
        SyntaxCheckButton.IsEnabled = false;
        MaiMuriDX.IsEnabled = false;
        AutoSaveManager.Of().SetAutoSaveEnable(false);
        SetSavedState(true);
        TheWindow.Title = GetWindowsTitleString();
    }
    private async Task InitFromFile(string path) //file name should not be included in path
    {
        updateprog(0);

        if (ChartServer.App != null) _ = ToggleChartShare(); //不管了自生自灭吧

        if (soundSetting != null) soundSetting.Close();
        if (editorSetting == null) ReadEditorSetting();

        useOgg = File.Exists(path + "/track.ogg");

        var audioPath = path + "/track" + (useOgg ? ".ogg" : ".mp3");
        audioDir = audioPath;
        var dataPath = path + "/maidata.txt";
        if (!File.Exists(audioPath))
        {
            MessageBox.Show(GetLocalizedString("NoTrack"), GetLocalizedString("Error"));
            return;
        }

        if (!File.Exists(dataPath))
        {
            MessageBox.Show(GetLocalizedString("NoMaidata_txt"), GetLocalizedString("Error"));
            return;
        }

        updateprog(10);

        maidataDir = path;
        SafeTerminationDetector.Of().ChangePath(maidataDir);
        LoadRawFumenText("");
        if (bgmStream != -1024)
        {
            Bass.BASS_ChannelStop(bgmStream);
            Bass.BASS_StreamFree(bgmStream);
        }

        //soundSetting.Close();
        var decodeStream = Bass.BASS_StreamCreateFile(audioPath, 0L, 0L, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_STREAM_PRESCAN);
        bgmStream = BassFx.BASS_FX_TempoCreate(decodeStream, BASSFlag.BASS_FX_FREESOURCE);
        Bass.BASS_ChannelGetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_FREQ, ref originFreq);
        //Bass.BASS_StreamCreateFile(audioPath, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);

        Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_BGM_Level);
        Bass.BASS_ChannelSetAttribute(trackStartStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_BGM_Level);
        Bass.BASS_ChannelSetAttribute(allperfectStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_BGM_Level);
        Bass.BASS_ChannelSetAttribute(fanfareStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_BGM_Level);
        Bass.BASS_ChannelSetAttribute(clockStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_BGM_Level);
        Bass.BASS_ChannelSetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Answer_Level);
        Bass.BASS_ChannelSetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Judge_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakStream, BASSAttribute.BASS_ATTRIB_VOL,
            editorSetting!.Default_Break_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakSlideStream, BASSAttribute.BASS_ATTRIB_VOL,
            editorSetting!.Default_Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStartStream, BASSAttribute.BASS_ATTRIB_VOL,
            editorSetting!.Default_Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Break_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStream, BASSAttribute.BASS_ATTRIB_VOL,
            editorSetting!.Default_Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Ex_Level);
        Bass.BASS_ChannelSetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Touch_Level);
        Bass.BASS_ChannelSetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, editorSetting!.Default_Hanabi_Level);
        Bass.BASS_ChannelSetAttribute(holdRiserStream, BASSAttribute.BASS_ATTRIB_VOL,
            editorSetting!.Default_Hanabi_Level);
        var info = Bass.BASS_ChannelGetInfo(bgmStream);
        if (info.freq != 44100) MessageBox.Show(GetLocalizedString("Warn44100Hz"), GetLocalizedString("Attention"));
        ReadWaveFromFile();
        SimaiProcess.ClearData();

        updateprog(30);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

        if (!SimaiProcess.ReadData(dataPath)) return;

        updateprog(40);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

        if (Cover.Visibility == Visibility.Visible)
            ((Storyboard)Resources["CoverHide"]).Begin();

        LevelSelector.SelectedItem = LevelSelector.Items[0];
        ReadSetting();
        LoadRawFumenText(SimaiProcess.fumens[selectedDifficulty]);
        SeekTextFromTime();
        await SimaiProcess.Serialize(GetRawFumenText());

        FumenContent.Focus();
        DrawWave();

        updateprog(90);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

        OffsetTextBox.Text = SimaiProcess.first.ToString();

        //Cover.Visibility = Visibility.Collapsed;
        MenuEdit.IsEnabled = true;
        VolumnSetting.IsEnabled = true;
        MenuMuriCheck.IsEnabled = true;
        Menu_ExportRender.IsEnabled = true;
        SyntaxCheckButton.IsEnabled = true;
        MaiMuriDX.IsEnabled = true;
        AutoSaveManager.Of().SetAutoSaveEnable(true);
        SetSavedState(true);
        SyntaxCheck();

        SetShareMode(false);

        updateprog(100);
    }

    private async Task InitFromShare(string fileUrl, GuestInitDto data)
    {
        updateprog(20);

        if (soundSetting != null) soundSetting.Close();
        if (editorSetting == null) ReadEditorSetting();

        var basePath = Environment.CurrentDirectory + "/Sharing";
        Directory.CreateDirectory(basePath); //防止没有Sharing文件夹

        updateprog(25);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

        useOgg = data.UseOgg;

        HttpClient httpClient = new(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (cert == null) return false;
                return cert.GetPublicKey().SequenceEqual(certBytes);
            }
        });

        // 下载音频
        var trackName = "track" + (useOgg ? ".ogg" : ".mp3");
        string localAudioPath = Path.Combine(basePath, trackName);
        using (var stream = await httpClient.GetStreamAsync(fileUrl + "/" + trackName))
        using (var fs = new FileStream(localAudioPath, FileMode.Create))
        {
            await stream.CopyToAsync(fs);
        }

        updateprog(35);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

        //下载MajSettings
        string localSettingPath = Path.Combine(basePath, majSettingFilename);
        byte[] settingBytes = await httpClient.GetByteArrayAsync(fileUrl + "/" + majSettingFilename);
        await File.WriteAllBytesAsync(localSettingPath, settingBytes);

        updateprog(45);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

        if (isHost) originMaidataDir = maidataDir;
        maidataDir = basePath;
        audioDir = localAudioPath;
        LoadRawFumenText("");
        if (bgmStream != -1024)
        {
            Bass.BASS_ChannelStop(bgmStream);
            Bass.BASS_StreamFree(bgmStream);
        }

        //soundSetting.Close();
        var decodeStream = Bass.BASS_StreamCreateFile(audioDir, 0L, 0L, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_STREAM_PRESCAN);
        bgmStream = BassFx.BASS_FX_TempoCreate(decodeStream, BASSFlag.BASS_FX_FREESOURCE);
        Bass.BASS_ChannelGetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_FREQ, ref originFreq);
        //Bass.BASS_StreamCreateFile(audioPath, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT);

        var info = Bass.BASS_ChannelGetInfo(bgmStream);
        if (info.freq != 44100) MessageBox.Show(GetLocalizedString("Warn44100Hz"), GetLocalizedString("Attention"));
        ReadWaveFromFile();
        SimaiProcess.ClearData();

        updateprog(60);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

        //if (!SimaiProcess.ReadData(dataPath)) return;
        SimaiProcess.title = data.Name;
        SimaiProcess.first = data.Offset;
        selectedDifficulty = data.Diff;
        SimaiProcess.levels[selectedDifficulty] = data.Level;
        SimaiProcess.fumens[selectedDifficulty] = data.Text;

        LevelSelector.SelectedIndex = selectedDifficulty;

        //ReadSetting();
        var setting = JsonConvert.DeserializeObject<MajSetting>(File.ReadAllText(localSettingPath));
        SetBgmPosition(setting!.lastEditTime);
        Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(trackStartStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(allperfectStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(fanfareStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(clockStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Answer_Level);
        Bass.BASS_ChannelSetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Judge_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakSlideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStartStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Ex_Level);
        Bass.BASS_ChannelSetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Touch_Level);
        Bass.BASS_ChannelSetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Hanabi_Level);
        Bass.BASS_ChannelSetAttribute(holdRiserStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Hanabi_Level);

        LoadRawFumenText(SimaiProcess.fumens[selectedDifficulty]);
        SeekTextFromTime();

        updateprog(65);

        await SimaiProcess.Serialize(GetRawFumenText());
        FumenContent.Focus();
        if (Cover.Visibility == Visibility.Visible)
            ((Storyboard)Resources["CoverHide"]).Begin();
        DrawWave();

        updateprog(80);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

        OffsetTextBox.Text = SimaiProcess.first.ToString();

        Cover.Visibility = Visibility.Collapsed;
        MenuEdit.IsEnabled = true;
        VolumnSetting.IsEnabled = true;
        MenuMuriCheck.IsEnabled = true;
        Menu_ExportRender.IsEnabled = true;
        SyntaxCheckButton.IsEnabled = true;
        MaiMuriDX.IsEnabled = true;
        AutoSaveManager.Of().SetAutoSaveEnable(false);
        isSaved = true;
        SyntaxCheck();
        _shadowText = FumenContent.Text; // 影子文本和UI直接挂钩，没必要用不带\r的

        SetShareMode(true);

        updateprog(100);
    }

    private void SetShareMode(bool state)
    {
        MapInfo.IsEnabled = !state;
        LevelSelector.IsEnabled = !state;
        LevelTextBox.IsEnabled = !state;
        OffsetTextBox.IsEnabled = !state;
        MapInfo.IsEnabled = !state;
        if (state)
        {
            TheWindow.Title = GetWindowsTitleString(SimaiProcess.title! + " Share");
        }
        ShareMode = state;
    }

    internal async void SyntaxCheck()
    {
#if DEBUG
        await SyntaxChecker.ScanAsync(GetRawFumenText());
        SetErrCount(SyntaxChecker.GetErrorCount());
#else
        try
        {
            await SyntaxChecker.ScanAsync(GetRawFumenText());
            SetErrCount(SyntaxChecker.ErrorList.Count);
        }
        catch
        {
            SetErrCount(GetLocalizedString("InternalErr"));
        }
#endif
    }
    void SetErrCount<T>(T eCount) => Dispatcher.Invoke(() => ErrCount.Content = $"{eCount}");
    private void ReadWaveFromFile()
    {
        var useOgg = File.Exists(maidataDir + "/track.ogg");
        var bgmDecode = Bass.BASS_StreamCreateFile(maidataDir + "/track" + (useOgg ? ".ogg" : ".mp3"), 0L, 0L, BASSFlag.BASS_STREAM_DECODE);
        try
        {
            songLength = Bass.BASS_ChannelBytes2Seconds(bgmDecode,
                Bass.BASS_ChannelGetLength(bgmDecode, BASSMode.BASS_POS_BYTE));
/*                int sampleNumber = (int)((songLength * 1000) / (0.02f * 1000));
                wavedBs = new float[sampleNumber];
                for (int i = 0; i < sampleNumber; i++)
                {
                    wavedBs[i] = Bass.BASS_ChannelGetLevels(bgmDecode, 0.02f, BASSLevel.BASS_LEVEL_MONO)[0];
                }*/
            Bass.BASS_StreamFree(bgmDecode);
            var bgmSample = Bass.BASS_SampleLoad(maidataDir + "/track" + (useOgg ? ".ogg" : ".mp3"), 0, 0, 1, BASSFlag.BASS_DEFAULT);
            var bgmInfo = Bass.BASS_SampleGetInfo(bgmSample);
            var freq = bgmInfo.freq;
            var sampleCount = (long)(songLength * freq * 2);
            var bgmRAW = new short[sampleCount];
            Bass.BASS_SampleGetData(bgmSample, bgmRAW);

            waveRaws[0] = new short[sampleCount / 20 + 1];
            for (var i = 0; i < sampleCount; i = i + 20) waveRaws[0][i / 20] = bgmRAW[i];
            waveRaws[1] = new short[sampleCount / 50 + 1];
            for (var i = 0; i < sampleCount; i = i + 50) waveRaws[1][i / 50] = bgmRAW[i];
            waveRaws[2] = new short[sampleCount / 100 + 1];
            for (var i = 0; i < sampleCount; i = i + 100) waveRaws[2][i / 100] = bgmRAW[i];
        }
        catch (Exception e)
        {
            MessageBox.Show("mp3/ogg解码失败。\nMP3/OGG Decode fail.\n" + e.Message + Bass.BASS_ErrorGetCode());
            Bass.BASS_StreamFree(bgmDecode);
            Process.Start("https://github.com/LingFeng-bbben/MajdataEdit/issues/26");
        }
    }

    private void SetSavedState(bool state)
    {
        if (ShareMode)
        {
            if (state)
            {
                isSaved = true;
                TheWindow.Title = GetWindowsTitleString(SimaiProcess.title! + " Share");
            }
            else
            {
                isSaved = false;
                TheWindow.Title = GetWindowsTitleString(GetLocalizedString("Unsaved") + SimaiProcess.title! + " Share");
            }
        }
        else
        {
            if (state)
            {
                isSaved = true;
                LevelSelector.IsEnabled = true;
                TheWindow.Title = GetWindowsTitleString(SimaiProcess.title!);
            }
            else
            {
                isSaved = false;
                LevelSelector.IsEnabled = false;
                TheWindow.Title = GetWindowsTitleString(GetLocalizedString("Unsaved") + SimaiProcess.title!);
                AutoSaveManager.Of().SetFileChanged();
            }
        }
    }

    /// <summary>
    ///     Ask the user and save fumen.
    /// </summary>
    /// <returns>Return false if user cancel the action</returns>
    private bool AskSave()
    {
        var result = MessageBox.Show(GetLocalizedString("AskSave"), GetLocalizedString("Warning"),
            MessageBoxButton.YesNoCancel);
        if (result == MessageBoxResult.Yes)
        {
            SaveFumen(true);
            return true;
        }

        if (result == MessageBoxResult.Cancel) return false;
        return true;
    }

    private void SaveFumen(bool writeToDisk)
    {
        string _maidataDir = maidataDir;
        if (ShareMode)
        {
            if (isHost) _maidataDir = originMaidataDir;
            else 
            {
                _client.InvokeAsync(nameof(ChartHub.SaveFumen), false);
                return;
            }
            _client.InvokeAsync(nameof(ChartHub.SaveFumen), true);
        }

        if (selectedDifficulty == -1) return;
        
        SimaiProcess.fumens[selectedDifficulty] = GetRawFumenText();
        SimaiProcess.first = float.Parse(OffsetTextBox.Text);
        if (_maidataDir == "")
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "maidata.txt|maidata.txt",
                OverwritePrompt = true
            };
            if ((bool)saveDialog.ShowDialog()!) _maidataDir = new FileInfo(saveDialog.FileName).DirectoryName!;
        }

        SyntaxCheck();

        SimaiProcess.SaveData(_maidataDir + "/maidata.bak.txt");
        SaveSetting();
        if (writeToDisk)
        {
            SimaiProcess.SaveData(_maidataDir + "/maidata.txt");
            SetSavedState(true);
        }
    }

    private void SaveSetting()
    {
        if (maidataDir == "") return;
        var setting = new MajSetting
        {
            lastEditDiff = selectedDifficulty,
            lastEditTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream))
        };
        Bass.BASS_ChannelGetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.BGM_Level);
        Bass.BASS_ChannelGetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Answer_Level);
        Bass.BASS_ChannelGetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Judge_Level);
        Bass.BASS_ChannelGetAttribute(judgeBreakStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Break_Level);
        Bass.BASS_ChannelGetAttribute(breakSlideStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Break_Slide_Level);
        Bass.BASS_ChannelGetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Ex_Level);
        Bass.BASS_ChannelGetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Touch_Level);
        Bass.BASS_ChannelGetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Slide_Level);
        Bass.BASS_ChannelGetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, ref setting.Hanabi_Level);
        var json = JsonConvert.SerializeObject(setting);
        File.WriteAllText(maidataDir + "/" + majSettingFilename, json);
    }

    private void ReadSetting()
    {
        var path = maidataDir + "/" + majSettingFilename;
        if (!File.Exists(path)) return;
        var setting = JsonConvert.DeserializeObject<MajSetting>(File.ReadAllText(path));
        LevelSelector.SelectedIndex = setting!.lastEditDiff;
        selectedDifficulty = setting.lastEditDiff;
        SetBgmPosition(setting.lastEditTime);
        Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(trackStartStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(allperfectStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(fanfareStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(clockStream, BASSAttribute.BASS_ATTRIB_VOL, setting.BGM_Level);
        Bass.BASS_ChannelSetAttribute(answerStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Answer_Level);
        Bass.BASS_ChannelSetAttribute(judgeStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Judge_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Level);
        Bass.BASS_ChannelSetAttribute(judgeBreakSlideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(slideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStartStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Slide_Level);
        Bass.BASS_ChannelSetAttribute(breakStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Level);
        Bass.BASS_ChannelSetAttribute(breakSlideStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Break_Slide_Level);
        Bass.BASS_ChannelSetAttribute(judgeExStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Ex_Level);
        Bass.BASS_ChannelSetAttribute(touchStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Touch_Level);
        Bass.BASS_ChannelSetAttribute(hanabiStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Hanabi_Level);
        Bass.BASS_ChannelSetAttribute(holdRiserStream, BASSAttribute.BASS_ATTRIB_VOL, setting.Hanabi_Level);

        SaveSetting(); // 覆盖旧版本setting
    }

    private void CreateNewFumen(string path)
    {
        if (File.Exists(path + "/maidata.txt"))
            MessageBox.Show(GetLocalizedString("MaidataExist"));
        else
            File.WriteAllText(path + "/maidata.txt",
                "&title=" + GetLocalizedString("SetTitle") + "\n" +
                "&artist=" + GetLocalizedString("SetArtist") + "\n" +
                "&des=" + GetLocalizedString("SetDes") + "\n" +
                "&first=0\n");
    }

    private void CreateEditorSetting()
    {
        editorSetting = new EditorSetting
        {
            RenderMode =
            RenderOptions.ProcessRenderMode == RenderMode.SoftwareOnly ? 1 : 0 // 使用命令行指定强制软件渲染时，同步修改配置值
        };

        File.WriteAllText(editorSettingFilename, JsonConvert.SerializeObject(editorSetting, Formatting.Indented));

        var esp = new EditorSettingPanel(true)
        {
            Owner = this
        };
        esp.ShowDialog();
    }

    private void ReadEditorSetting()
    {
        if (!File.Exists(editorSettingFilename)) CreateEditorSetting();
        var json = File.ReadAllText(editorSettingFilename);
        editorSetting = JsonConvert.DeserializeObject<EditorSetting>(json)!;

        if (RenderOptions.ProcessRenderMode != RenderMode.SoftwareOnly)
            //如果没有通过命令行预先指定渲染模式，则使用设置项的渲染模式
            RenderOptions.ProcessRenderMode =
                editorSetting.RenderMode == 0 ? RenderMode.Default : RenderMode.SoftwareOnly;
        else
            //如果通过命令行指定了使用软件渲染模式，则覆盖设置项
            editorSetting.RenderMode = 1;

        LocalizeDictionary.Instance.Culture = new CultureInfo(editorSetting.Language);
        AddGesture(editorSetting.PlayPauseKey, "PlayAndPause");
        AddGesture(editorSetting.PlayStopKey, "StopPlaying");
        AddGesture(editorSetting.SaveKey, "SaveFile");
        AddGesture(editorSetting.SendViewerKey, "SendToView");
        AddGesture(editorSetting.IncreasePlaybackSpeedKey, "IncreasePlaybackSpeed");
        AddGesture(editorSetting.DecreasePlaybackSpeedKey, "DecreasePlaybackSpeed");
        AddGesture("Ctrl+f", "Find");
        AddGesture(editorSetting.MirrorLeftRightKey, "MirrorLR");
        AddGesture(editorSetting.MirrorUpDownKey, "MirrorUD");
        AddGesture(editorSetting.Mirror180Key, "Mirror180");
        AddGesture(editorSetting.Mirror45Key, "Mirror45");
        AddGesture(editorSetting.MirrorCcw45Key, "MirrorCcw45");
        FumenContent.FontSize = editorSetting.FontSize;

        ViewerCover.Content = editorSetting.backgroundCover.ToString();
        ViewerSpeed.Content = editorSetting.playSpeed.ToString("F1"); // 转化为形如"7.0", "9.5"这样的速度
        ViewerTouchSpeed.Content = editorSetting.touchSpeed.ToString("F1");

        chartChangeTimer.Interval = editorSetting.ChartRefreshDelay; // 设置更新延迟

        SaveEditorSetting(); // 覆盖旧版本setting
    }

    public void SaveEditorSetting()
    {
        File.WriteAllText(editorSettingFilename, JsonConvert.SerializeObject(editorSetting, Formatting.Indented));
    }

    private void AddGesture(string keyGusture, string command)
    {
        var gesture = (InputGesture) new KeyGestureConverter().ConvertFromString(keyGusture)!;
        var inputBinding = new InputBinding((ICommand)FumenContent.Resources[command], gesture);
        FumenContent.InputBindings.Add(inputBinding);
    }

    // This update very freqently to Draw FFT wave.
    private void VisualEffectRefreshTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            DrawFFT();
            DrawWave();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    // 谱面变更延迟解析
    private void ChartChangeTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        Console.WriteLine("TextChanged");
        //SyntaxCheck(); //不要进行定期检查（疑似快速修改谱面内容时莫名其妙卡死原因）
        //太快=>异步=>在另外线程调用的原因。。被自己蠢笑啦
        Dispatcher.Invoke(async () =>
            {
                SyntaxCheck();
                SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition());
                DrawWave();
                if (!ErrCount.Content.ToString()!.EndsWith("?"))
                    SetErrCount(ErrCount.Content.ToString() + "?");
            });
    }

    private void DrawFFT()
    {
        Dispatcher.InvokeAsync(() =>
        {
            //Scroll WaveView
            var currentTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            //MusicWave.Margin = new Thickness(-currentTime / sampleTime * zoominPower, Margin.Left, MusicWave.Margin.Right, Margin.Bottom);
            //MusicWaveCusor.Margin = new Thickness(-currentTime / sampleTime * zoominPower, Margin.Left, MusicWave.Margin.Right, Margin.Bottom);

            var writableBitmap = new WriteableBitmap(255, 255, 72, 72, PixelFormats.Pbgra32, null);
            FFTImage.Source = writableBitmap;
            writableBitmap.Lock();
            var backBitmap = new Bitmap(255, 255, writableBitmap.BackBufferStride,
                PixelFormat.Format32bppArgb, writableBitmap.BackBuffer);

            var graphics = Graphics.FromImage(backBitmap);
            graphics.Clear(Color.Transparent);

            var fft = new float[1024];
            Bass.BASS_ChannelGetData(bgmStream, fft, (int)BASSData.BASS_DATA_FFT1024);
            var points = new PointF[1024];
            for (var i = 0; i < fft.Length; i++)
                points[i] = new PointF((float)Math.Log10(i + 1) * 100f, 240 - fft[i] * 256); //semilog

            graphics.DrawCurve(new Pen(Color.LightSkyBlue, 1), points);


            //no please
            /*
            var isSuccess = new Visuals().CreateSpectrumWave(bgmStream, graphics, new System.Drawing.Rectangle(0, 0, 255, 255),
                System.Drawing.Color.White, System.Drawing.Color.Red,
                System.Drawing.Color.Black, 1,
                false, false, false);
            Console.WriteLine(isSuccess);
            */
            graphics.Flush();
            graphics.Dispose();
            backBitmap.Dispose();

            writableBitmap.AddDirtyRect(new Int32Rect(0, 0, 255, 255));
            writableBitmap.Unlock();
        });
    }

    private void InitWave()
    {
        var width = (int)Width - 2;
        var height = (int)MusicWave.Height;
        WaveBitmap = new WriteableBitmap(width, height, 72, 72, PixelFormats.Pbgra32, null);
        MusicWave.Source = WaveBitmap;
    }

    private void DrawWave()
    {
        if (isDrawing) return;
        if (WaveBitmap == null) return;

        Dispatcher.Invoke(() =>
        {
            isDrawing = true;
            var width = WaveBitmap.PixelWidth;
            var height = WaveBitmap.PixelHeight;

            if (waveRaws[0] == null)
            {
                isDrawing = false;
                return;
            }

            WaveBitmap.Lock();

            //the process starts
            var backBitmap = new Bitmap(width, height, WaveBitmap.BackBufferStride,
                PixelFormat.Format32bppArgb, WaveBitmap.BackBuffer);
            var graphics = Graphics.FromImage(backBitmap);
            var currentTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));

            graphics.Clear(Color.FromArgb(100, 0, 0, 0));

            var resample = (int)deltatime - 1;
            if (resample > 1 && resample <= 3) resample = 1;
            if (resample > 3) resample = 2;
            var waveLevels = waveRaws[resample];

            var step = songLength / waveLevels.Length;
            var startindex = (int)((currentTime - deltatime) / step);
            var stopindex = (int)((currentTime + deltatime) / step);
            var linewidth = backBitmap.Width / (float)(stopindex - startindex);
            var pen = new Pen(Color.Green, linewidth);
            var points = new List<PointF>();
            for (var i = startindex; i < stopindex; i = i + 1)
            {
                if (i < 0) i = 0;
                if (i >= waveLevels.Length - 1) break;

                var x = (i - startindex) * linewidth;
                var y = waveLevels[i] / 65535f * height + height / 2;

                points.Add(new PointF(x, y));
            }

            graphics.DrawLines(pen, points.ToArray());

            //Draw Bpm lines
            var lastbpm = -1f;
            var bpmChangeTimes = new List<double>(); //在什么时间变成什么值
            var bpmChangeValues = new List<float>();
            bpmChangeTimes.Clear();
            bpmChangeValues.Clear();
            foreach (var timing in SimaiProcess.timinglist)
                if (timing.currentBpm != lastbpm)
                {
                    bpmChangeTimes.Add(timing.time);
                    bpmChangeValues.Add(timing.currentBpm);
                    lastbpm = timing.currentBpm;
                }

            bpmChangeTimes.Add(Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetLength(bgmStream)));

            double time = SimaiProcess.first;
            var signature = 4; //预留拍号
            var currentBeat = 1;
            var timePerBeat = 0d;
            pen = new Pen(Color.Yellow, 1);
            var strongBeat = new List<double>();
            var weakBeat = new List<double>();
            for (var i = 1; i < bpmChangeTimes.Count; i++)
            {
                while (time - bpmChangeTimes[i] < -0.05) //在那个时间之前都是之前的bpm
                {
                    if (currentBeat > signature) currentBeat = 1;
                    timePerBeat = 1d / (bpmChangeValues[i - 1] / 60d);
                    if (currentBeat == 1)
                        strongBeat.Add(time);
                    else
                        weakBeat.Add(time);
                    currentBeat++;
                    time += timePerBeat;
                }

                time = bpmChangeTimes[i];
                currentBeat = 1;
            }

            foreach (var btime in strongBeat)
            {
                if (btime - currentTime > deltatime) continue;
                var x = ((float)(btime / step) - startindex) * linewidth;
                graphics.DrawLine(pen, x, 0, x, 75);
            }

            foreach (var btime in weakBeat)
            {
                if (btime - currentTime > deltatime) continue;
                var x = ((float)(btime / step) - startindex) * linewidth;
                graphics.DrawLine(pen, x, 0, x, 15);
            }

            //Draw timing lines
            pen = new Pen(Color.White, 1);
            foreach (var note in SimaiProcess.timinglist)
            {
                if (note == null) break;
                if (note.time - currentTime > deltatime) continue;
                var x = ((float)(note.time / step) - startindex) * linewidth;
                graphics.DrawLine(pen, x, 60, x, 75);
            }

            //Draw notes                    
            foreach (var note in SimaiProcess.notelist)
            {
                if (note == null) break;
                if (note.time - currentTime > deltatime) continue;
                var notes = note.getNotes();
                var isEach = notes.Count(o => !o.isSlideNoHead) > 1;

                var x = ((float)(note.time / step) - startindex) * linewidth;

                foreach (var noteD in notes)
                {
                    var y = noteD.startPosition * 6.875f + 8f; //与键位有关

                    if (noteD.isHanabi)
                    {
                        var xDeltaHanabi = (float)(1f / step) * linewidth; //Hanabi is 1s due to frame analyze
                        var rectangleF = new RectangleF(x, 0, xDeltaHanabi, 75);
                        if (noteD.noteType == SimaiNoteType.TouchHold)
                            rectangleF.X += (float)(noteD.holdTime / step) * linewidth;
                        var gradientBrush = new LinearGradientBrush(
                            rectangleF,
                            Color.FromArgb(100, 255, 0, 0),
                            Color.FromArgb(0, 255, 0, 0),
                            LinearGradientMode.Horizontal
                        );
                        graphics.FillRectangle(gradientBrush, rectangleF);
                    }

                    if (noteD.noteType == SimaiNoteType.Tap)
                    {
                        if (noteD.isForceStar)
                        {
                            pen.Width = 3;
                            if (noteD.isBreak)
                                pen.Color = Color.OrangeRed;
                            else if (isEach)
                                pen.Color = Color.Gold;
                            else
                                pen.Color = Color.DeepSkyBlue;
                            Brush brush = new SolidBrush(pen.Color);
                            graphics.DrawString("*", new Font("Consolas", 12, System.Drawing.FontStyle.Bold), brush,
                                new PointF(x - 7f, y - 7f));
                        }
                        else
                        {
                            pen.Width = 2;
                            if (noteD.isBreak)
                                pen.Color = Color.OrangeRed;
                            else if (isEach)
                                pen.Color = Color.Gold;
                            else
                                pen.Color = Color.LightPink;
                            graphics.DrawEllipse(pen, x - 2.5f, y - 2.5f, 5, 5);
                        }
                    }

                    if (noteD.noteType == SimaiNoteType.Touch)
                    {
                        pen.Width = 2;
                        pen.Color = isEach ? Color.Gold : Color.DeepSkyBlue;
                        graphics.DrawRectangle(pen, x - 2.5f, y - 2.5f, 5, 5);
                    }

                    if (noteD.noteType == SimaiNoteType.Hold)
                    {
                        pen.Width = 3;
                        if (noteD.isBreak)
                            pen.Color = Color.OrangeRed;
                        else if (isEach)
                            pen.Color = Color.Gold;
                        else
                            pen.Color = Color.LightPink;

                        var xRight = x + (float)(noteD.holdTime / step) * linewidth;

                        //1h[0:1]
                        if (!float.IsNormal(xRight)) xRight = ushort.MaxValue;
                        if (xRight - x < 1f) xRight = x + 5;
                        graphics.DrawLine(pen, x, y, xRight, y);

                    }

                    if (noteD.noteType == SimaiNoteType.TouchHold)
                    {
                        pen.Width = 3;
                        var xDelta = (float)(noteD.holdTime / step) * linewidth / 4f;
                        //Console.WriteLine("HoldPixel"+ xDelta);
                        if (!float.IsNormal(xDelta)) xDelta = ushort.MaxValue;
                        if (xDelta < 1f) xDelta = 1;

                        pen.Color = Color.FromArgb(200, 255, 75, 0);
                        graphics.DrawLine(pen, x, y, x + xDelta * 4f, y);
                        pen.Color = Color.FromArgb(200, 255, 241, 0);
                        graphics.DrawLine(pen, x, y, x + xDelta * 3f, y);
                        pen.Color = Color.FromArgb(200, 2, 165, 89);
                        graphics.DrawLine(pen, x, y, x + xDelta * 2f, y);
                        pen.Color = Color.FromArgb(200, 0, 140, 254);
                        graphics.DrawLine(pen, x, y, x + xDelta, y);
                    }

                    if (noteD.noteType == SimaiNoteType.Slide)
                    {
                        pen.Width = 3;
                        if (!noteD.isSlideNoHead)
                        {
                            if (noteD.isBreak)
                                pen.Color = Color.OrangeRed;
                            else if (isEach)
                                pen.Color = Color.Gold;
                            else
                                pen.Color = Color.DeepSkyBlue;
                            Brush brush = new SolidBrush(pen.Color);
                            graphics.DrawString("*", new Font("Consolas", 12, System.Drawing.FontStyle.Bold), brush,
                                new PointF(x - 7f, y - 7f));
                        }

                        if (noteD.isSlideBreak)
                            pen.Color = Color.OrangeRed;
                        else if (notes.Count(o => o.noteType == SimaiNoteType.Slide) >= 2)
                            pen.Color = Color.Gold;
                        else
                            pen.Color = Color.SkyBlue;
                        pen.DashStyle = DashStyle.Dot;
                        var xSlide = (float)(noteD.slideStartTime / step - startindex) * linewidth;
                        var xSlideRight = (float)(noteD.slideTime / step) * linewidth + xSlide;

                        if (!float.IsNormal(xSlideRight)) xSlideRight = ushort.MaxValue;
                        if (!float.IsNormal(xSlide)) xSlide = ushort.MaxValue;

                        graphics.DrawLine(pen, xSlide, y, xSlideRight, y);
                        pen.DashStyle = DashStyle.Solid;
                    }
                }
            }

            if (playStartTime - currentTime <= deltatime)
            {
                //Draw play Start time
                pen = new Pen(Color.Red, 5);
                var x1 = (float)(playStartTime / step - startindex) * linewidth;
                PointF[] tranglePoints = { new(x1 - 2, 0), new(x1 + 2, 0), new(x1, 3.46f) };
                graphics.DrawPolygon(pen, tranglePoints);
            }

            if (ghostCusorPositionTime - currentTime <= deltatime)
            {
                //Draw ghost cusor
                pen = new Pen(Color.Orange, 5);
                var x2 = (float)(ghostCusorPositionTime / step - startindex) * linewidth;
                PointF[] tranglePoints2 = { new(x2 - 2, 0), new(x2 + 2, 0), new(x2, 3.46f) };
                graphics.DrawPolygon(pen, tranglePoints2);
            }

            graphics.Flush();
            graphics.Dispose();
            backBitmap.Dispose();

            //MusicWave.Width = waveLevels.Length * zoominPower;
            WaveBitmap.AddDirtyRect(new Int32Rect(0, 0, WaveBitmap.PixelWidth, WaveBitmap.PixelHeight));
            WaveBitmap.Unlock();
            isDrawing = false;
        });
    }

    // This update less frequently. set the time text.
    private void CurrentTimeRefreshTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateTimeDisplay();
    }

    private void UpdateTimeDisplay()
    {
        var currentPlayTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
        var minute = (int)currentPlayTime / 60;
        double second = (int)(currentPlayTime - 60 * minute);
        Dispatcher.Invoke(() => { TimeLabel.Content = string.Format("{0}:{1:00}", minute, second); });
    }

    private void ScrollWave(double delta)
    {
        if (Bass.BASS_ChannelIsActive(bgmStream) == BASSActive.BASS_ACTIVE_PLAYING)
            TogglePause();
        delta = delta * deltatime / (Width / 2);
        var time = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
        SetBgmPosition(time + delta);
        SimaiProcess.ClearNoteListPlayedState();
        SeekTextFromTime();
        Task.Run(() => DrawWave());
    }

    public static string GetLocalizedString(string key, string resourceFileName = "Langs")
    {
        // Build up the fully-qualified name of the key

        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var fullKey = assemblyName + ":" + resourceFileName + ":" + key;
        var locExtension = new LocExtension(fullKey);
        locExtension.ResolveLocalizedValue(out string? localizedString);

        return localizedString ?? key;
    }

    private async void TogglePlay(PlayMethod playMethod = PlayMethod.Normal)
    {
        if (Op_Button.IsEnabled == false) return;

        if (lastEditorState == EditorControlMethod.Start || playMethod != PlayMethod.Normal)
            if (!sendRequestStop())
                return;

        FumenContent.Focus();
        SaveFumen(false);
        if (CheckAndStartView()) return;
        Op_Button.IsEnabled = false;
        isPlaying = true;
        isPlan2Stop = false;
        PlayAndPauseButton.Content = "  ▌▌ ";
        var CusorTime = await SimaiProcess.Serialize(GetRawFumenText(), GetRawFumenPosition()); //scan first

        //TODO: Moeying改一下你的generateSoundEffect然后把下面这行删了
        var isOpIncluded = playMethod == PlayMethod.Normal ? false : true;

        var startAt = DateTime.Now;
        switch (playMethod)
        {
            case PlayMethod.Record:
                Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_FREQ, originFreq * GetPlaybackSpeed());
                Bass.BASS_ChannelSetPosition(bgmStream, 0);
                startAt = DateTime.Now.AddSeconds(5d);
                //TODO: i18n
                MessageBox.Show(GetLocalizedString("AskRender"), GetLocalizedString("Attention"));
                InternalSwitchWindow(false);
                generateSoundEffectList(0.0, isOpIncluded);
                var task = new Task(() => renderSoundEffect(5d));
                try
                {
                    task.Start();
                    task.Wait();
                }
                catch (AggregateException)
                {
                    MessageBox.Show(task.Exception!.InnerException!.Message + "\n" +
                                    task.Exception.InnerException.StackTrace);
                    return;
                }

                if (!sendRequestRun(startAt, playMethod)) return;
                break;
            case PlayMethod.Op:
                generateSoundEffectList(0.0, isOpIncluded);
                InternalSwitchWindow(false);
                Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_FREQ, originFreq * GetPlaybackSpeed());
                Bass.BASS_ChannelSetPosition(bgmStream, 0);
                startAt = DateTime.Now.AddSeconds(5d);
                Bass.BASS_ChannelPlay(trackStartStream, true);
                Task.Run(() =>
                {
                    if (!sendRequestRun(startAt, playMethod)) return;
                    while (DateTime.Now.Ticks < startAt.Ticks)
                        if (lastEditorState != EditorControlMethod.Start)
                            return;
                    Dispatcher.Invoke(() =>
                    {
                        playStartTime =
                            Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                        SimaiProcess.ClearNoteListPlayedState();
                        StartSELoop();
                        //soundEffectTimer.Start();
                        waveStopMonitorTimer.Start();
                        visualEffectRefreshTimer.Start();
                        Bass.BASS_ChannelPlay(bgmStream, false);
                    });
                });
                break;
            case PlayMethod.Normal:
                playStartTime = Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
                generateSoundEffectList(playStartTime, isOpIncluded);
                SimaiProcess.ClearNoteListPlayedState();
                StartSELoop();
                //soundEffectTimer.Start();
                waveStopMonitorTimer.Start();
                visualEffectRefreshTimer.Start();
                startAt = DateTime.Now;

                Bass.BASS_ChannelSetAttribute(bgmStream, BASSAttribute.BASS_ATTRIB_FREQ, originFreq * GetPlaybackSpeed());
                Bass.BASS_ChannelPlay(bgmStream, false);
                Task.Run(() =>
                {
                    if (lastEditorState == EditorControlMethod.Pause)
                    {
                        if (!sendRequestContinue(startAt)) return;
                    }
                    else
                    {
                        if (!sendRequestRun(startAt, playMethod)) return;
                    }
                });
                break;
        }

        ghostCusorPositionTime = (float)CusorTime;
        DrawWave();
    }

    private void TogglePause()
    {
        Op_Button.IsEnabled = true;
        isPlaying = false;
        isPlan2Stop = false;

        FumenContent.Focus();
        PlayAndPauseButton.Content = "▶";
        Bass.BASS_ChannelStop(bgmStream);
        Bass.BASS_ChannelStop(holdRiserStream);
        //soundEffectTimer.Stop();
        waveStopMonitorTimer.Stop();
        visualEffectRefreshTimer.Stop();
        sendRequestPause();
        DrawWave();
    }

    private void ToggleStop()
    {
        Op_Button.IsEnabled = true;
        isPlaying = false;
        isPlan2Stop = false;

        FumenContent.Focus();
        PlayAndPauseButton.Content = "▶";
        Bass.BASS_ChannelStop(bgmStream);
        Bass.BASS_ChannelStop(holdRiserStream);
        //soundEffectTimer.Stop();
        waveStopMonitorTimer.Stop();
        visualEffectRefreshTimer.Stop();
        sendRequestStop();
        Bass.BASS_ChannelSetPosition(bgmStream, playStartTime);
        DrawWave();
    }

    private void TogglePlayAndPause(PlayMethod playMethod = PlayMethod.Normal)
    {
        if (isPlaying)
            TogglePause();
        else 
            TogglePlay(playMethod);
    }

    private void TogglePlayAndStop(PlayMethod playMethod = PlayMethod.Normal)
    {
        if (isPlaying)
            ToggleStop();
        else
            TogglePlay(playMethod);
    }

    private void SetPlaybackSpeed(int speedItem)
    {
        speedItem = Math.Max(0, speedItem);
        speedItem = Math.Min(PlayBackSpeedSelector.Items.Count - 1, speedItem);
        PlayBackSpeedSelector.SelectedIndex = speedItem;
    }

    private void SetPlaybackSpeedDiff(int speedItemDiff) => SetPlaybackSpeed(PlayBackSpeedSelector.SelectedIndex + speedItemDiff);

    private float GetPlaybackSpeed()
    {
        //var speed = PlayBackSpeedSelector.SelectedItem switch
        //{
        //    ComboBoxItem { Content: "0.10x" } => -90,
        //    ComboBoxItem { Content: "0.25x" } => -75,
        //    ComboBoxItem { Content: "0.50x" } => -50,
        //    ComboBoxItem { Content: "0.75x" } => -25,
        //    ComboBoxItem { Content: "1.00x" } => 0,
        //    ComboBoxItem { Content: "1.50x" } => 50,
        //    ComboBoxItem { Content: "1.75x" } => 75,
        //    ComboBoxItem { Content: "2.00x" } => 100,
        //    _ => 0
        //};
        int speed = 0;
        this.Dispatcher.Invoke(() =>
        {
            speed = PlayBackSpeedSelector.SelectedIndex switch
            {
                0 => -90,
                1 => -75,
                2 => -50,
                3 => -25,
                4 => 0,
                5 => 50,
                6 => 75,
                7 => 100,
                _ => 0
            };
        });

        return speed / 100f + 1f;
    }

    private void SetBgmPosition(double time)
    {
        if (lastEditorState == EditorControlMethod.Pause) sendRequestStop();
        Bass.BASS_ChannelSetPosition(bgmStream, time);
    }


    //*VIEW COMMUNICATION
    private bool sendRequestStop()
    {
        var requestStop = new EditRequestjson
        {
            control = EditorControlMethod.Stop
        };
        var json = JsonConvert.SerializeObject(requestStop);
        var response = WebControl.RequestPOST("http://localhost:8013/", json);
        if (response == "ERROR")
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        lastEditorState = EditorControlMethod.Stop;
        return true;
    }

    private bool sendRequestPause()
    {
        var requestStop = new EditRequestjson
        {
            control = EditorControlMethod.Pause
        };
        var json = JsonConvert.SerializeObject(requestStop);
        var response = WebControl.RequestPOST("http://localhost:8013/", json);
        if (response == "ERROR")
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        lastEditorState = EditorControlMethod.Pause;
        return true;
    }

    private bool sendRequestContinue(DateTime StartAt)
    {
        var request = new EditRequestjson
        {
            control = EditorControlMethod.Continue,
            startAt = StartAt.Ticks,
            startTime = (float)Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream)),
            audioSpeed = GetPlaybackSpeed(),
            editorPlayMethod = editorSetting.editorPlayMethod
        };
        var json = JsonConvert.SerializeObject(request);
        var response = WebControl.RequestPOST("http://localhost:8013/", json);
        if (response == "ERROR")
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        lastEditorState = EditorControlMethod.Start;
        return true;
    }

    private bool sendRequestRun(DateTime StartAt, PlayMethod playMethod)
    {
        var jsonStruct = new Majson();
        foreach (var note in SimaiProcess.notelist)
        {
            note.noteList = note.getNotes();
            jsonStruct.timingList.Add(note);
        }

        jsonStruct.title = SimaiProcess.title!;
        jsonStruct.artist = SimaiProcess.artist!;
        jsonStruct.level = SimaiProcess.levels[selectedDifficulty];
        jsonStruct.designer = SimaiProcess.designer!;
        jsonStruct.difficulty = SimaiProcess.GetDifficultyText(selectedDifficulty);
        jsonStruct.diffNum = selectedDifficulty;

        var json = JsonConvert.SerializeObject(jsonStruct);
        var path = maidataDir + "/majdata.json";
        File.WriteAllText(path, json);

        var request = new EditRequestjson();
        if (playMethod == PlayMethod.Op)
            request.control = EditorControlMethod.OpStart;
        else if (playMethod == PlayMethod.Normal)
            request.control = EditorControlMethod.Start;
        else
            request.control = EditorControlMethod.Record;

        Dispatcher.Invoke(() =>
        {
            request.jsonPath = path;
            request.startAt = StartAt.Ticks;
            request.startTime =
                (float)Bass.BASS_ChannelBytes2Seconds(bgmStream, Bass.BASS_ChannelGetPosition(bgmStream));
            // request.playSpeed = float.Parse(ViewerSpeed.Text);
            // 将maimaiDX速度换算为View中的单位速度 MajSpeed = 107.25 / (71.4184491 * (MaiSpeed + 0.9975) ^ -0.985558604)
            request.noteSpeed = editorSetting!.playSpeed;
            request.touchSpeed = editorSetting!.touchSpeed;
            request.backgroundCover = editorSetting!.backgroundCover;
            request.comboStatusType = editorSetting!.comboStatusType;
            request.audioSpeed = GetPlaybackSpeed();
            request.smoothSlideAnime = editorSetting!.SmoothSlideAnime;
            request.editorPlayMethod = editorSetting.editorPlayMethod;
        });

        json = JsonConvert.SerializeObject(request);
        var response = WebControl.RequestPOST("http://localhost:8013/", json);
        if (response == "ERROR")
        {
            MessageBox.Show(GetLocalizedString("PortClear"));
            return false;
        }

        lastEditorState = EditorControlMethod.Start;
        return true;
    }

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", EntryPoint = "MoveWindow")]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

    private bool CheckAndStartView()
    {
        if (Process.GetProcessesByName("MajdataView").Length == 0 && Process.GetProcessesByName("Unity").Length == 0)
        {
            try
            {
                var viewProcess = Process.Start("MajdataView.exe");
                var setWindowPosTimer = new Timer(2000)
                {
                    AutoReset = false
                };
                setWindowPosTimer.Elapsed += SetWindowPosTimer_Elapsed;
                setWindowPosTimer.Start();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        return false;
    }

    private string GetViewerWorkingDirectory()
    {
        return Environment.CurrentDirectory + "/MajdataView_Data/StreamingAssets";
        /*string tempPath = "";
        Process baseProc;
        Process[] viewProcs;
        viewProcs = Process.GetProcessesByName("MajdataView");
        // Prioritize Majdata First
        if (viewProcs.Length > 0)
        {
            baseProc = viewProcs.First();
            string pwd;
            pwd = baseProc.StartInfo.WorkingDirectory.TrimEnd('/');
            if (pwd.Length == 0) pwd = ".";
            tempPath = pwd + "/MajdataView_Data/StreamingAssets";
        }
        else
        {
            viewProcs = Process.GetProcessesByName("Unity");
        }
        if (viewProcs.Length <= 0)
            throw new Exception("Unable to find MajdataView instance!");

        return (tempPath.Length == 0) ?
            Environment.CurrentDirectory + "/SFX" :
            tempPath;*/
    }

    private void InternalSwitchWindow(bool moveToPlace = true)
    {
        var windowPtr = FindWindow(null, "MajdataView");
        //var thisWindow = FindWindow(null, this.Title);
        ShowWindow(windowPtr, 5); //还原窗口
        SwitchToThisWindow(windowPtr, true);
        //SwitchToThisWindow(thisWindow, true);
        if (moveToPlace) InternalMoveWindow();
    }

    private void InternalMoveWindow()
    {
        var windowPtr = FindWindow(null, "MajdataView");
        var source = PresentationSource.FromVisual(this);

        double dpiX = 1, dpiY = 1;
        if (source != null)
        {
            dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
            dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
        }

        //Console.WriteLine(dpiX+" "+dpiY);
        dpiX /= 96d;
        dpiY /= 96d;

        var Height = this.Height * dpiY;
        var Left = this.Left * dpiX;
        var Top = this.Top * dpiY;
        MoveWindow(windowPtr,
            (int)(Left - Height + 20),
            (int)Top,
            (int)Height - 20,
            (int)Height, true);
    }

    private void SetWindowGoldenPosition()
    {
        // 属于你的独享黄金位置
        var ScreenWidth = SystemParameters.PrimaryScreenWidth;
        var ScreenHeight = SystemParameters.PrimaryScreenHeight;

        Left = (ScreenWidth - Width + Height) / 2 - 10;
        Top = (ScreenHeight - Height) / 2;
    }

    private void SwitchFumenOverwriteMode()
    {
        fumenOverwriteMode = !fumenOverwriteMode;

        // 修改覆盖模式启用状态
        // fetch TextEditor from FumenContent
        var textEditorProperty =
            typeof(TextBox).GetProperty("TextEditor", BindingFlags.NonPublic | BindingFlags.Instance);
        var textEditor = textEditorProperty!.GetValue(FumenContent, null);

        // set _OvertypeMode on the TextEditor
        var overtypeModeProperty = textEditor!.GetType()
            .GetProperty("_OvertypeMode", BindingFlags.NonPublic | BindingFlags.Instance)!;
        overtypeModeProperty!.SetValue(textEditor, fumenOverwriteMode, null);

        //修改提示弹窗可见性
        OverrideModeTipsPopup.Visibility = fumenOverwriteMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task CheckUpdate(bool onStart = false)
    {
        if (UpdateCheckLock) return;
        UpdateCheckLock = true;

        #region 子函数

        SemVersion oldVersionCompatible(string versionString)
        {
            var result = SemVersion.Parse("v0.0.0", SemVersionStyles.Any);
            try
            {
                // 尝试解析版本号，解析失败说明是旧版本格式
                result = SemVersion.Parse(versionString, SemVersionStyles.Any);
            }
            catch (FormatException)
            {
                if (versionString.Contains("Back2Root"))
                {
                    // back to root特别版本
                    result = SemVersion.Parse("v0.0.0", SemVersionStyles.Any);
                }
                else if (versionString.Contains("Early Access"))
                {
                    // EA版本
                    result = SemVersion.Parse("v0.0.1", SemVersionStyles.Any);
                }
                else if (versionString.Contains("Alpha"))
                {
                    // 旧版本格式 Alpha<MainVersion>.<SubVersion>[.<ModifiedVersion>]
                    // 从4.0开始，结束于6.4
                    // 在原版本号基础上增加 0. 主版本前缀，并增加 -alpha 后缀
                    var startPos = versionString.IndexOfAny("0123456789".ToArray());
                    versionString = "0." + versionString[startPos..];
                    if (versionString.Count(c => { return c == '.'; }) > 2)
                        versionString = versionString[..versionString.LastIndexOf('.')];
                    versionString += "-alpha";
                    result = SemVersion.Parse(versionString, SemVersionStyles.Any);
                }
                else if (versionString.Contains("Beta"))
                {
                    // 旧版本格式 Beta<MainVersion>.<SubVersion>[.<ModifiedVersion>]
                    // 从1.0开始，结束于3.1。后续的语义化版本号继承该版本号进度，从4.0开始
                    // 增加 -beta 后缀
                    var startPos = versionString.IndexOfAny("0123456789".ToArray());
                    versionString = versionString[startPos..];
                    if (versionString.Contains(' '))
                        versionString = versionString[..versionString.IndexOf(' ')];
                    versionString += "-beta";
                    result = SemVersion.Parse(versionString, SemVersionStyles.Any);
                }
                else
                {
                    // 其他无法识别的版本，均设置为v0.0.1-unknown
                    result = SemVersion.Parse("v0.0.1-unknown", SemVersionStyles.Any);
                }
            }

            return result;
        }

        void requestHandler(string response)
        {
            try
            {
                UpdateCheckLock = false;
                if (response == "ERROR")
                {
                    // 网络请求失败
                    if (!onStart) MessageBox.Show(GetLocalizedString("RequestFail"), GetLocalizedString("CheckUpdate"));
                    return;
                }

                var resJson = JsonConvert.DeserializeObject<JObject>(response)!;
                var latestVersionString = resJson["tag_name"]!.ToString();
                var releaseUrl = resJson["html_url"]!.ToString();

                var latestVersion = oldVersionCompatible(latestVersionString);

                if (latestVersion.ComparePrecedenceTo(MAJDATA_VERSION) > 0)
                {
                    // 版本不同，需要更新
                    var msgboxText = string.Format(GetLocalizedString("NewVersionDetected"), latestVersionString,
                        MAJDATA_VERSION_STRING);
                    if (onStart) msgboxText += "\n\n" + GetLocalizedString("AutoUpdateCheckTip");

                    var result = MessageBox.Show(
                        msgboxText,
                        GetLocalizedString("CheckUpdate"),
                        MessageBoxButton.YesNo);
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            var startInfo = new ProcessStartInfo(releaseUrl)
                            {
                                UseShellExecute = true
                            };
                            Process.Start(startInfo);
                            break;
                        case MessageBoxResult.No:
                            break;
                    }
                }
                else
                {
                    // 没有新版本，可以不用更新
                    if (!onStart) MessageBox.Show(GetLocalizedString("NoNewVersion"), GetLocalizedString("CheckUpdate"));
                }
            }
            catch (Exception)
            {
                if (!onStart) MessageBox.Show(GetLocalizedString("RequestFail"), GetLocalizedString("CheckUpdate"));
            }
        }

        #endregion

        // 检查是否需要更新软件

        requestHandler(await WebControl.RequestGETAsync("http://api.github.com/repos/re-poem/MajdataViewX/releases/latest"));
    }

    public string GetWindowsTitleString()
    {
        return $"MajdataEdit ({MAJDATA_VERSION_STRING})";
    }

    public string GetWindowsTitleString(string info)
    {
        try
        {
            var details = "Editing: " + SimaiProcess.title;
            if (details.Length > 50)
                details = details[..50];
            DCRPCclient.SetPresence(new RichPresence
            {
                Details = details,
                State = "With note count of " + SimaiProcess.notelist.Count,
                Assets = new Assets
                {
                    LargeImageKey = "salt",
                    LargeImageText = "Majdata",
                    SmallImageKey = "None"
                }
            });
        }
        catch
        {
        }

        return GetWindowsTitleString() + " - " + info;
    }

    public async Task OpenFile(string path)
    {
        await InitFromFile(path);
    }

    private async Task ToggleChartShare(bool initIfClose = false)
    {
        if (ChartServer.App != null)
        {
            SetShareMode(false);

            SaveFumen(true);
            isHost = false;
            
            if (_client != null)
            {
                await _client!.StopAsync();
                _client = null;
            }
            await ChartServer.StopAsync();

            TheWindow.Height -= 20;
            Global_Grid.RowDefinitions[2].Height = new GridLength(0); //hide status bar
            ShareStatus.DataContext = null;
            Menu_ToggleChartShare.Header = GetLocalizedString("StartChartShare");
            Menu_ConnectChartShare.Header = GetLocalizedString("ConnectChartShare");
            Menu_ConnectChartShare.IsEnabled = true;

            if (initIfClose) await InitFromFile(originMaidataDir);

            return;
        }

        var text = GetRawFumenText();
        if (text == "")
        {
            MessageBox.Show(GetLocalizedString("ShareEmpty"), GetLocalizedString("Error"));
            return;
        }
        isHost = true;
        _shadowText = text;
        var cds = new HubDataService(
            SimaiProcess.title!,
            selectedDifficulty,
            SimaiProcess.levels[selectedDifficulty],
            text,
            SimaiProcess.first,
            useOgg);
        await ChartServer.StartAsync(cds, maidataDir);
        await ConnectToChartServer("127.0.0.1", 8014);

        TheWindow.Height += 20;
        Global_Grid.RowDefinitions[2].Height = new GridLength(20); //show status bar
        ShareStatus.Text = string.Format(GetLocalizedString("ShareModeServer"), GetLocalIPAddress());
        Menu_ToggleChartShare.Header = GetLocalizedString("StopChartShare");
        Menu_ConnectChartShare.Header = GetLocalizedString("DisconnectChartShare");
        Menu_ConnectChartShare.IsEnabled = false; //房主不能自己断掉与自己的连接
    }
    // 返回是否连接成功
    private async Task<bool> ConnectToChartServer(string ip, int port)
    {
        try
        {
            if (_client != null)
            {
                await _client.StartAsync();
                return false;
            }

            string hubUrl = $"https://{ip}:{port}/chartHub";
            string fileUrl = $"https://{ip}:{port}/chartFiles";

            updateprog(0);

            _client = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.HttpMessageHandlerFactory = (handler) =>
                    {
                        if (handler is HttpClientHandler clientHandler)
                        {
                            clientHandler.ServerCertificateCustomValidationCallback =
                                (message, cert, chain, errors) =>
                                {
                                    if (cert == null) return false;
                                    return cert.GetPublicKey().SequenceEqual(certBytes);
                                };
                        }
                        return handler;
                    };

                    options.WebSocketConfiguration = sockets =>
                    {
                        sockets.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
                        {
                            if (cert == null) return false;
                            return cert.GetPublicKey().SequenceEqual(certBytes);
                        };
                    };
                })
                .Build();


            // 收到初始化数据 (文本、音频)
            _client.On<GuestInitDto>(nameof(IEditorClient.OnJoined), (data) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    await InitFromShare(fileUrl, data);
                });
            });

            // 有人加入了
            _client.On<ClientConnectDto>(nameof(IEditorClient.OnUserJoined), async (user) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowStatusMessage(string.Format(GetLocalizedString("UserJoined"), user.UserName));
                });
            });

            // 有人离开了
            _client.On<ClientConnectDto, string>(nameof(IEditorClient.OnUserLeft), async (user, message) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ShowStatusMessage(string.Format(GetLocalizedString("UserLeft"), user.UserName, message));
                });
            });

            // 收到远程用户的编辑操作
            _client.On<string>(nameof(IEditorClient.OnTyping), async (patchText) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    _isRemoteUpdate = true; //防止死循环

                    string currentUiText = GetRawFumenText();
                    var patches = _dmp.patch_fromText(patchText);
                    var result = _dmp.patch_apply(patches, currentUiText);
                    string newText = (string)result[0];

                    var cursor = GetRawFumenPosition();

                    foreach (var patch in patches)
                    {
                        var patchStart = patch.start1;
                        var patchEnd = patchStart + patch.length1;
                        var lengthDiff = patch.length2 - patch.length1;

                        // 补丁块完全在光标之后
                        //（由于EQUAL diff，几个字符内会误判到覆盖光标，但是这样至少能快点）
                        if (patchStart > cursor)
                        {
                            continue;
                        }

                        // 补丁块完全在光标之前
                        if (patchEnd <= cursor)
                        {
                            cursor += lengthDiff;
                            continue; // 下一个补丁
                        }

                        // 补丁块覆盖了光标
                        var currentPos = patchStart;
                        foreach (var diff in patch.diffs)
                        {
                            int len = diff.text.Length;
                            if (diff.operation == Operation.EQUAL)
                            {
                                currentPos += len;
                            }
                            else if (diff.operation == Operation.DELETE)
                            {
                                // 如果删除内容在光标后面就不用管（前文误判因素）
                                if (currentPos > cursor) break;

                                // 如果光标在被删除的区间内，回退到删除点起点
                                if (currentPos + len > cursor) cursor = currentPos;
                                else if (currentPos + len <= cursor) cursor -= len;
                                currentPos += len;
                            }
                            else if (diff.operation == Operation.INSERT)
                            {
                                // 如果插入点在光标前，光标后移
                                if (currentPos <= cursor) cursor += len;
                            }
                        }
                    }

                    SetRawFumenText(newText); // 这里的光标变化会被拦截不同步
                    _shadowText = newText;

                    FumenContent.Focus();

                    _isRemoteUpdate = false;

                    SetRawFumenPosition(cursor); // 这里的光标处理需要同步
                });
            });

            //光标移动信息
            _client.On<Dictionary<string, RemoteCursor>>(nameof(IEditorClient.OnSyncCursors), async (cursors) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    _cursors = cursors.Where(c => c.Key != _client.ConnectionId).ToDictionary(kv => kv.Key, kv => kv.Value);
                    RenderAllCursors();
                });
            });

            //接收到保存信息
            _client.On<string>(nameof(IEditorClient.OnSaveFumen), async (text) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (text == "") SetSavedState(true); //为空表示只是状态更新
                    else SaveFumen(true);
                });
            });

            //客户端关闭
            _client.Closed += async (exception) =>
            {
                await Dispatcher.Invoke(async () =>
                {
                    if (exception != null)
                    {
                        MessageBox.Show(string.Format(GetLocalizedString("ConnectionClosed"), exception.Message + exception.InnerException?.Message), GetLocalizedString("Error"));
                    }

                    _client = null;
                    await DisconnectToChartServer();
                });
            };

            updateprog(5);

            await _client.StartAsync();
            updateprog(10);

            await _client.SendAsync(nameof(ChartHub.GuestInit), new ClientConnectDto()
            {
                UserName = editorSetting!.ShareUserName,
                ColorHex = editorSetting!.ShareColorHex,
                isHost = isHost
            });
            if (!isHost) Menu_ToggleChartShare.IsEnabled = false; //非房主不能套娃开房
            Menu_AutosaveRecover.IsEnabled = false;
            updateprog(20);
            return true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(string.Format(GetLocalizedString("ConnectFail"), exception.Message + exception.InnerException?.Message), GetLocalizedString("Error"));
            _client = null;
            updateprog(100);
            return false;
        }
    }

    private async Task DisconnectToChartServer()
    {
        if (_client != null)
        {
            if (_client.State == HubConnectionState.Connected)
            {
                SaveFumen(true);
                await _client.StopAsync();
            }
            _client = null;
        }
        SetShareMode(false);
        //if (!isHost)
        Menu_ToggleChartShare.IsEnabled = true; //非房主不能套娃开房-恢复
        Menu_AutosaveRecover.IsEnabled = true;
        _cursors.Clear();
        ClearWindow();
    }

    private async Task SyncChartServer()
    {
        if (_isRemoteUpdate || !ShareMode) return;
        if (_client!.State != HubConnectionState.Connected) return;

        string currentUiText = GetRawFumenText();

        var diffs = _dmp.diff_main(_shadowText, currentUiText);
        if (diffs.Count == 0) return;
        var patches = _dmp.patch_make(_shadowText, diffs);
        var patchText = _dmp.patch_toText(patches);

        _shadowText = currentUiText;

        await _client.InvokeAsync(nameof(ChartHub.Typing), patchText);
    }

    private async void ShowStatusMessage(string message, int durationMs = 3000)
    {
        ShareStatus.Text = message;
        await Task.Delay(durationMs);
        if (ShareStatus.Text == message)
        {
            ShareStatus.Text = string.Format(GetLocalizedString("ShareModeServer"), GetLocalIPAddress());
        }
    }

    private void RenderAllCursors()
    {
        FumenContent.UpdateLayout();
        CursorOverlay.Children.Clear();

        foreach (var kvp in _cursors)
        {
            var cursor = kvp.Value;

            Rect rect = FumenContent.GetRectFromCharacterIndex(ToUiIndex(cursor.Index));
            Point screenPos = FumenContent.TranslatePoint(rect.TopLeft, CursorOverlay);
            var brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(cursor.ColorHex));

            Border cursorLine = new()
            {
                Width = 1,
                // 如果是空行，rect.Height 可能会很小，给个保底值 18
                Height = rect.Height > 18 ? rect.Height : 18,
                Background = brush,
                IsHitTestVisible = false
            };

            // 名字标签
            var nameTag = new TextBlock
            {
                Text = cursor.UserName,
                FontSize = 10,
                Foreground = System.Windows.Media.Brushes.WhiteSmoke,
                Padding = new Thickness(2, 0, 2, 0),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(cursorLine, screenPos.X);
            Canvas.SetTop(cursorLine, screenPos.Y);
            Canvas.SetLeft(nameTag, screenPos.X + 0.5);
            Canvas.SetTop(nameTag, screenPos.Y - 7);

            CursorOverlay.Children.Add(cursorLine);
            CursorOverlay.Children.Add(nameTag);
        }
    }

    private void ApplyMirror(Mirror.HandleType handleType)
    {
        var result = Mirror.NoteMirrorHandle(FumenContent.SelectedText, handleType);
        FumenContent.SelectedText = result;
    }

    // 获取本机局域网IP
    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    public void ToggleFindGrid()
    {
        if (FindGrid.Visibility == Visibility.Collapsed)
        {
            FindGrid.Visibility = Visibility.Visible;
            InputText.Text = FumenContent.SelectedText;
            InputText.Focus();
        }
        else
        {
            FindGrid.Visibility = Visibility.Collapsed;
        }
    }



    //*PLAY CONTROL

    private enum PlayMethod
    {
        Normal,
        Op,
        Record
    }
}
