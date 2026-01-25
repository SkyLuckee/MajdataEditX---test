using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace MajdataEdit;

public static class ThemeConfig
{
    public static Color LabelForeground { get; private set; }
    public static Color ButtonForeground { get; private set; }
    public static Color HelperForeground { get; private set; }
    public static Color WindowBackground { get; private set; }
    public static Color ButtonsBackground { get; private set; }

    private const string ConfigPath = "ThemeConfig.json";

    static ThemeConfig()
    {
        Load();
    }

    public static void Load()
    {
        try
        {
            var json = File.ReadAllText(ConfigPath);
            var cfg = JsonSerializer.Deserialize<ColorConfig>(json);

#pragma warning disable CS8602 // 解引用可能出现空引用。
            LabelForeground = ParseColorOrDefault(cfg.LabelForeground, Default.LabelForeground);
            ButtonForeground = ParseColorOrDefault(cfg.ButtonForeground, Default.ButtonForeground);
            HelperForeground = ParseColorOrDefault(cfg.HelperForeground, Default.HelperForeground);
            WindowBackground = ParseColorOrDefault(cfg.WindowBackground, Default.WindowBackground);
            ButtonsBackground = ParseColorOrDefault(cfg.ButtonsBackground, Default.ButtonsBackground);
#pragma warning restore CS8602
        }
        catch
        {
            WriteDefaultConfig();
            ApplyDefaultColors();
        }
    }

    // 用于反序列化
    public class ColorConfig
    {
        public string? LabelForeground { get; set; }
        public string? ButtonForeground { get; set; }
        public string? HelperForeground { get; set; }
        public string? WindowBackground { get; set; }
        public string? ButtonsBackground { get; set; }
    }

    // 默认颜色
    private static class Default
    {
        public const string LabelForeground = "#FFBDBDBD";
        public const string ButtonForeground = "#FFEDEDED";
        public const string HelperForeground = "#FF569CD6";
        public const string WindowBackground = "#FF1F1F1F";
        public const string ButtonsBackground = "#99303030";
    }

    // 写入默认配置文件
    private static void WriteDefaultConfig()
    {
        var cfg = new ColorConfig
        {
            LabelForeground = Default.LabelForeground,
            ButtonForeground = Default.ButtonForeground,
            HelperForeground = Default.HelperForeground,
            WindowBackground = Default.WindowBackground,
            ButtonsBackground = Default.ButtonsBackground
        };

        var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    // 把默认值应用到静态属性
    private static void ApplyDefaultColors()
    {
        LabelForeground = (Color)ColorConverter.ConvertFromString(Default.LabelForeground);
        ButtonForeground = (Color)ColorConverter.ConvertFromString(Default.ButtonForeground);
        HelperForeground = (Color)ColorConverter.ConvertFromString(Default.HelperForeground);
        WindowBackground = (Color)ColorConverter.ConvertFromString(Default.WindowBackground);
        ButtonsBackground = (Color)ColorConverter.ConvertFromString(Default.ButtonsBackground);
    }

    private static Color ParseColorOrDefault(string input, string fallback)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(input);
        }
        catch
        {
            return (Color)ColorConverter.ConvertFromString(fallback);
        }
    }
}
