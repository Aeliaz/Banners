
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Flags;

public class BannerConverter
{
    public Dictionary<string, string> BaseColorsToColors { get; set; } = new();
    public Dictionary<string, string> IdsToColors { get; set; } = new();
    public Dictionary<string, string> Patterns { get; set; } = new();

    public TextCommandResult TryGenerateBanner(TextCommandCallingArgs args)
    {
        IPlayer byPlayer = args.Caller.Player;
        ItemSlot slot = byPlayer.Entity.RightHandItemSlot;

        if (slot?.Itemstack?.Collectible is not BlockBanner blockBanner)
        {
            return TextCommandResult.Error($"{modDomain}:command-nobanner".Localize());
        }

        string mcBaseColor = args.RawArgs.PopUntil('{').Replace("minecraft:", string.Empty).Replace("_banner", string.Empty);
        string bannerAttributes = args.RawArgs.PopAll();

        JsonObject fromObject;
        try
        {
            bannerAttributes = Regex.Replace(bannerAttributes, @"(?<!\\)\""([a-zA-Z0-9_]+)\""(?=\:)", "\"$1\"");
            bannerAttributes = Regex.Replace(bannerAttributes, @"(?<!\\)\b([a-zA-Z_]+)\b(?=\s*[\,\}])", "\"$1\"");
            fromObject = JsonObject.FromJson(bannerAttributes);
        }
        catch (Exception)
        {
            fromObject = null;
        }

        if ((fromObject?.KeyExists("BlockEntityTag")) != true || !fromObject["BlockEntityTag"].KeyExists("Patterns") || !fromObject["BlockEntityTag"]["Patterns"].AsArray().Any())
        {
            return TextCommandResult.Error($"{modDomain}:command-genbannermc-invalidsyntax".Localize());
        }

        if (!BaseColorsToColors.TryGetValue(mcBaseColor, out string vsBaseColor) && !BaseColorsToColors.TryGetValue(Wildcard, out vsBaseColor))
        {
            return TextCommandResult.Error($"{modDomain}:command-genbannermc-invalidsyntax".Localize());
        }

        BannerProperties bannerProperties = new BannerProperties(blockBanner.DefaultPlacement);
        bannerProperties.AddLayer(new BannerLayer("0-b", vsBaseColor), byPlayer.Entity.World);
        foreach (BannerLayer layer in fromObject["BlockEntityTag"]["Patterns"].AsArray().Select((pattern, index) => new BannerLayer(
            layer: $"{index}-{GetPattern(pattern)}",
            color: GetColor(pattern)
            )))
        {
            bannerProperties.AddLayer(layer, byPlayer.Entity.World, byPlayer);
        }

        slot.Itemstack.Attributes.RemoveAttribute(attributeBanner);
        bannerProperties.ToStack(slot.Itemstack);
        slot.MarkDirty();
        return TextCommandResult.Success($"{modDomain}:command-genbannermc".Localize());
    }

    private string GetPattern(JsonObject pattern)
    {
        _ = Patterns.TryGetValue(pattern["Pattern"].AsString(), out string value) || Patterns.TryGetValue(Wildcard, out value);
        return value;
    }

    private string GetColor(JsonObject pattern)
    {
        _ = IdsToColors.TryGetValue(pattern["Color"].AsInt().ToString(), out string value) || IdsToColors.TryGetValue(Wildcard, out value);
        return value;
    }
}