using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

namespace OnlyRealMapManager;

public class Voting(
    BasePlugin plugin,
    Dictionary<string, string> dict,
    string prefix
)
{   
    private readonly BasePlugin plugin = plugin;
    private readonly Dictionary<string, string> _map = dict;
    private readonly string _prefix = prefix;

    private List<ulong> wantsToVote = [];
    private Dictionary<ulong, string> nominations = new();
    private Dictionary<ulong, string> votes = new();

    private float _voting_percentage = 0.6f;
    private float _voting_duration = 20f;

    public void OnVote(CCSPlayerController controller, CommandInfo command, Action<string> onWinner)
    {
        // add to registry
        if (!wantsToVote.Contains(controller.SteamID))
        {
            wantsToVote.Add(controller.SteamID);
        }
        
        if (!this.Canvote())
        {
            var playerCount = Utilities.GetPlayers()
                .Where(p => !p.IsBot || !p.IsHLTV).ToList().Count;
            var needed = (int)Math.Ceiling(_voting_percentage * playerCount);
            
            Server.PrintToChatAll(
                $"{_prefix}" +
                $" {ChatColors.Red}{controller.PlayerName} {ChatColors.White}" +
                $"wants to rock the vote! " +
                $"[{ChatColors.Red}{wantsToVote.Count}{ChatColors.White} voted, " +
                $"{ChatColors.Red}{needed}{ChatColors.White} needed]");
            
            Server.PrintToChatAll(
                $"{_prefix} Use {ChatColors.Red}!rtv{ChatColors.White} to cast your vote!"
            );
            return;
        }

        var options = nominations.Values
            .Where(m => !m.Equals(Server.MapName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(_ => Random.Shared.Next())
            .Take(9)
            .ToList();

        if (options.Count < 9)
        {
            var filler = _map.Keys
                .Where(m => !m.Equals(Server.MapName, StringComparison.OrdinalIgnoreCase))
                .Except(options, StringComparer.OrdinalIgnoreCase)
                .OrderBy(_ => Random.Shared.Next())
                .Take(9 - options.Count);

            options.AddRange(filler);
        }
        
        var menu = new CenterHtmlMenu("Rock the vote", plugin);

        foreach (var item in options)
        {
            menu.AddMenuOption(item, (player, _) =>
            {
                votes[player.SteamID] = item;
                MenuManager.CloseActiveMenu(player);
                player.PrintToChat($"{_prefix} You voted for: {ChatColors.Red}{item}");
            });
        }

        foreach (var p in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot && !p.IsHLTV))
        {
            MenuManager.OpenCenterHtmlMenu(plugin, p, menu);
        }
        
        plugin.AddTimer(_voting_duration, () =>
        {
            var winner = votes.Values
                .GroupBy(v => v)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
            
            if (winner == null) return;
            
            nominations.Clear();
            votes.Clear();
            wantsToVote.Clear();
            
            onWinner(winner);
        });
    }
    

    public bool Canvote()
    {
        int playerCount = Utilities.GetPlayers()
            .Where(p => !p.IsBot || !p.IsHLTV).ToList().Count;
        
        var needed = (int)Math.Ceiling(_voting_percentage * playerCount);
        
        return wantsToVote.Count >= needed;
    }

    public void OnNominate(CCSPlayerController controller, CommandInfo command, string mapAlias)
    {
        if (!nominations.TryAdd(controller.SteamID, mapAlias))
        {
            nominations[controller.SteamID] = mapAlias;
            command.ReplyToCommand($"{_prefix} nomination changed to: {ChatColors.Red}{mapAlias}");
            return;
        }
        command.ReplyToCommand($"{_prefix} You have nominated: {ChatColors.Red}{mapAlias}");
    }
}