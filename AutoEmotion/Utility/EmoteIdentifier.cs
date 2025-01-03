using System;
using System.Collections.Generic;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace AutoEmotion.Utility;

public unsafe record EmoteIdentifier([property: JsonProperty("e")] uint EmoteID)
{
    private static readonly short ExpressionID = 3;

    public static Lazy<List<EmoteIdentifier>> EmoteList = new(() =>
    {
        var list = new List<EmoteIdentifier>
        {
            new EmoteIdentifier(0)
        };
        foreach (var emote in Svc.Data.GetExcelSheet<Emote>()!)
        {
            if (emote.TextCommand.IsValid == false || emote.EmoteCategory.Value.RowId == ExpressionID) continue;
            for (byte i = 0; i < emote.RowId switch { 1 => 1, 2 => 1, 3 => 1, _ => 1 }; i++)
            {
                list.Add(new EmoteIdentifier(emote.RowId));
            }
        }
        return list;
    });

    public static Lazy<List<EmoteIdentifier>> ExpressionList = new(() =>
    {
        var list = new List<EmoteIdentifier>
        {
            new EmoteIdentifier(0)
        };
        foreach (var emote in Svc.Data.GetExcelSheet<Emote>()!)
        {
            if (emote.TextCommand.IsValid == false || emote.EmoteCategory.Value.RowId != ExpressionID) continue;
            list.Add(new EmoteIdentifier(emote.RowId));
        }
        return list;
    });

    public static IReadOnlyList<EmoteIdentifier> ListEmote => EmoteList.Value;
    public static IReadOnlyList<EmoteIdentifier> ListExpression => ExpressionList.Value;

    private static readonly Dictionary<uint, string> Names = new() { { 0, "Nothing" } };
    private static readonly Dictionary<uint, uint> Icons = new() { { 0, 0 } };
    private static readonly Dictionary<uint, string> Commands = new() { { 0, string.Empty } };

    public static string FetchName(uint emoteID)
    {
        var emote = Svc.Data.GetExcelSheet<Emote>()?.GetRow(emoteID);
        if (emote == null) return $"Emote#{emoteID}";
        return emote.Value.Name.ToDalamudString().TextValue;
    }

    public static uint FetchIcon(uint emoteID)
    {
        var emote = Svc.Data.GetExcelSheet<Emote>()?.GetRow(emoteID);
        if (emote == null) return 0;
        return emote.Value.Icon;
    }

    public static string FetchCommand(uint emoteID)
    {
        var emote = Svc.Data.GetExcelSheet<Emote>()?.GetRow(emoteID);
        if (emote == null) return $"Emote#{emoteID}";
        var command = emote.Value.TextCommand.Value;
        if (command.Equals(null)) return $"EmoteCommand#{emoteID}";
        return command.Command.ToDalamudString().TextValue;
    }

    [Newtonsoft.Json.JsonIgnore]
    public string Name
    {
        get
        {
            if (Names.TryGetValue(EmoteID, out var name)) return name;

            name = FetchName(EmoteID);
            Names.TryAdd(EmoteID, name);

            return name;
        }
    }

    [Newtonsoft.Json.JsonIgnore]
    public uint Icon
    {
        get
        {
            if (Icons.TryGetValue(EmoteID, out var icon)) return icon;

            icon = FetchIcon(EmoteID);
            Icons.TryAdd(EmoteID, icon);

            return icon;
        }
    }

    [Newtonsoft.Json.JsonIgnore]
    public string EmoteCommand
    {
        get
        {
            if (Commands.TryGetValue(EmoteID, out var command)) return command;

            command = FetchCommand(EmoteID);
            Commands.TryAdd(EmoteID, command);

            return command;
        }
    }
    public static EmoteIdentifier? Get(Character* character, bool loopEmote = false)
    {
        if (character == null) return null;
        if (loopEmote)
        {
            if (character->Mode is not (CharacterModes.InPositionLoop or CharacterModes.EmoteLoop)) return null;
        }
        else
        {
            if (character->Mode is (CharacterModes.InPositionLoop or CharacterModes.EmoteLoop)) return null;
        }
        return new EmoteIdentifier(character->EmoteController.EmoteId);
    }
}
