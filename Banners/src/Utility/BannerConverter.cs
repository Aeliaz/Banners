
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Flags.Converter;

public enum EnumConverterInteraction
{
    Hand = 0,
    Block = 1
}

public class BannerConverter
{
    public Dictionary<string, string> BaseColorsToColors { get; set; } = new();
    public Dictionary<string, string> IdsToColors { get; set; } = new();
    public Dictionary<string, string> Patterns { get; set; } = new();

    public TextCommandResult TryGenerateBanner(TextCommandCallingArgs args)
    {
        IPlayer byPlayer = args.Caller.Player;
        ItemSlot slot = byPlayer.Entity.RightHandItemSlot;

        EnumConverterInteraction? interactionType = null;
        if (slot?.Itemstack?.Collectible is BlockBanner)
        {
            interactionType = EnumConverterInteraction.Hand;
        }
        else if (byPlayer?.CurrentBlockSelection?.Block is BlockBanner)
        {
            interactionType = EnumConverterInteraction.Block;
        }
        else
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

        if (!BaseColorsToColors.TryGetValueOrWildcard(mcBaseColor, out string vsBaseColor))
        {
            return TextCommandResult.Error($"{modDomain}:command-genbannermc-invalidsyntax".Localize());
        }

        BannerProperties bannerProperties = new BannerProperties();
        bannerProperties.Patterns.TryAdd(BannerLayer.FromLayer("0-b").WithColor(vsBaseColor), byPlayer.Entity.World);
        foreach (BannerLayer layer in fromObject["BlockEntityTag"]["Patterns"].AsArray().Select((pattern, index) => BannerLayer
            .FromLayer(layer: $"{index}-{GetPattern(pattern)}")
            .WithColor(color: GetColor(pattern))))
        {
            bannerProperties.Patterns.TryAdd(layer, byPlayer.Entity.World, byPlayer);
        }
        switch (interactionType)
        {
            case EnumConverterInteraction.Hand:
                slot.Itemstack.Attributes.RemoveAttribute(attributeBanner);
                bannerProperties.ToStack(slot.Itemstack);
                slot.MarkDirty();
                break;
            case EnumConverterInteraction.Block:
                BlockEntityBanner blockEntity = byPlayer.Entity.World.BlockAccessor.GetBlockEntity(byPlayer.CurrentBlockSelection.Position) as BlockEntityBanner;
                bannerProperties.SetPlacement(blockEntity.BannerProps.Placement);
                blockEntity?.ReplaceProperties(bannerProperties);
                break;
        }
        return TextCommandResult.Success($"{modDomain}:command-genbannermc".Localize());
    }

    private string GetPattern(JsonObject pattern)
    {
        _ = Patterns.TryGetValueOrWildcard(pattern["Pattern"].AsString(), out string value);
        return value;
    }

    private string GetColor(JsonObject pattern)
    {
        _ = IdsToColors.TryGetValueOrWildcard(pattern["Color"].AsInt().ToString(), out string value);
        return value;
    }
}