using Microsoft.AspNetCore.SignalR;

namespace MajdataEdit.ChartShare;

public class ChartHub : Hub<IEditorClient>
{
    private readonly HubDataService dataService;

    public ChartHub(HubDataService dataService)
    {
        this.dataService = dataService;
    }

    public async Task Typing(string patchText)
    {
        dataService.ApplyPatch(patchText);
        await Clients.Others.OnTyping(patchText);
    }

    public async Task Moving(int index)
    {
        dataService.UserCursors[Context.ConnectionId].Index = index;

        await Clients.Others.OnSyncCursors(new Dictionary<string, RemoteCursor>(dataService.UserCursors));
    }

    public async Task GuestInit(ClientConnectDto data)
    {
        data.UserId = Context.ConnectionId;

        dataService.ConnectedUsers.Add(data);
        if (data.isHost)
        {
            dataService.HostId = data.UserId;
        }

        await Clients.Caller.OnJoined(new GuestInitDto()
        {
            UserId = data.UserId,
            Name = dataService.Name,
            Diff = dataService.Diff,
            Level = dataService.Level,
            Text = dataService.CurrentText,
            Offset = dataService.Offset,
            UseOgg = dataService.UseOgg
        });

        dataService.UserCursors.AddOrUpdate(Context.ConnectionId, new RemoteCursor()
        {
            UserName = data.UserName,
            ColorHex = data.ColorHex,
            Index = 0
        }, (key, oldValue) => oldValue);

        await Clients.Others.OnUserJoined(data);
    }

    public override async Task<Task> OnDisconnectedAsync(Exception? exception)
    {
        var user = dataService.ConnectedUsers.FirstOrDefault(u => u.UserId == Context.ConnectionId);
        await Clients.Others.OnUserLeft(user!, exception == null ? "" : exception.Message);
        dataService.ConnectedUsers.Remove(user!);
        dataService.UserCursors.TryRemove(Context.ConnectionId, out _);

        await Clients.All.OnSyncCursors(new Dictionary<string, RemoteCursor>(dataService.UserCursors));

        return base.OnDisconnectedAsync(exception);
    }

    public async Task SaveFumen()
    {
        await Clients.Client(dataService.HostId).OnSaveFumen(dataService.CurrentText);
    }

    public async Task ChangeSaveState(bool state)
    {
        await Clients.Others.OnSaveStateChange(state);
    }
}