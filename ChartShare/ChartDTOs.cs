namespace MajdataEdit.ChartShare
{
    // 客户端接收的强类型接口定义（避免魔法字符串）
    public interface IEditorClient
    {
        Task OnJoined(GuestInitDto data);
        Task OnTyping(string patchText);
        Task OnSyncCursors(Dictionary<string, RemoteCursor> cursors);
        Task OnUserJoined(ClientConnectDto data);
        Task OnUserLeft(ClientConnectDto data, string message);
        Task OnSaveFumen(string text);
        Task OnSaveStateChange(bool state);
    }

    // --- 数据包定义 ---

    public class ClientConnectDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ColorHex { get; set; } // 光标颜色
        public bool isHost { get; set; }
    }

    // 初始化数据包
    public class GuestInitDto
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public int Diff { get; set; }
        public string Level { get; set; }
        public string Text { get; set; }
        public float Offset { get; set; }
        public bool UseOgg { get; set; }
    }

    public class RemoteCursor
    {
        public string UserName { get; set; }
        public string ColorHex { get; set; }
        public int Index { get; set; }
    }
}
