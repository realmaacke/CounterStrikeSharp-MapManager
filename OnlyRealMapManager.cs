using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

namespace OnlyRealMapManager;

public class OnlyRealMapManager : BasePlugin
{
    public override string ModuleName => "OnlyRealMapManager";
    public override string ModuleVersion => "1.0.0";

    private readonly string _prefix = $"[{ChatColors.Red}Only{ChatColors.Grey}Real{ChatColors.White}] - ";
    private readonly string _collection = "3315477547";
    private readonly string _mapFile = "mapcycle.txt";

    private string? _pendingMap;
    public required Voting Voting;

    private Dictionary<string, string> _maps = new(StringComparer.OrdinalIgnoreCase);

    public override void Load(bool hotReload)
    {
        _maps = MapManagerUtils.LoadMapCycle(this._mapFile, _prefix);
        Voting = new Voting(this, _maps, _prefix);
        Task.Run(async () =>
        {
            await Task.Delay(5000);
            Server.NextFrame(() =>
            {
                Console.WriteLine($"{_prefix} Startup delay done, loading map");
                Server.ExecuteCommand($"host_workshop_collection {_collection}");
            });
        });
    }

    [ConsoleCommand("css_rtv", "rock the vote")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnRtv(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null || controller.IsBot || controller.IsHLTV) return;
        
        this.Voting.OnVote(controller, command, winner =>
        {
            // invoke a round end hook
            _pendingMap = winner;
            Server.PrintToChatAll($"{_prefix} Next map: {ChatColors.Red}{winner}");
        });
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (_pendingMap == null) return HookResult.Continue;
        
        var newMap = _pendingMap;
        _pendingMap = null;
        
        Server.PrintToChatAll($"{_prefix} Changing map to: {ChatColors.Red}{newMap}{ChatColors.White}...");
        AddTimer(3f, () =>
        {
            Server.ExecuteCommand($"host_workshop_map {_maps[newMap]}");
        });
        return HookResult.Continue;
    }
    

    [ConsoleCommand("css_nominate", "nominate a map")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnNominate(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null || controller.IsBot || controller.IsHLTV) return;

        if (command.ArgCount > 1)
        {
            if (!this._maps.ContainsKey(command.GetArg(1)))
            {
                command.ReplyToCommand($"{_prefix} No map named: " +
                    $"{ChatColors.Red}{command.GetArg(1)}{ChatColors.White} exists.");
                return;
            }
            this.Voting.OnNominate(controller, command, command.GetArg(1));
            return;
        }
        
        var menu = new CenterHtmlMenu("Nominate a map", this);

        foreach (var element in _maps)
        {
            menu.AddMenuOption(element.Key, (_, _) =>
            {
                this.Voting.OnNominate(controller, command, element.Key);
                MenuManager.CloseActiveMenu(controller);
            });
        }
        
        MenuManager.OpenCenterHtmlMenu(this, controller, menu);
    }
}
