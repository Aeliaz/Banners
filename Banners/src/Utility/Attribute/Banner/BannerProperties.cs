using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Flags;

public class BannerProperties
{
    public string Name { get; protected set; }
    public string Placement { get; protected set; }

    public BannerModes Modes { get; protected set; } = new();

    public Patterns Patterns { get; protected set; } = new();
    public Cutouts Cutouts { get; protected set; } = new();

    public void GetDescription(BlockBanner blockBanner, IPlayer forPlayer, StringBuilder dsc, bool withDebugInfo = false)
    {
        if (forPlayer.Entity.Api is ICoreClientAPI && Hotkeys.DisplayBannerExtraInfo)
        {
            dsc.AppendLine(ModHotkey.BannerExtraInfoDesc.Localize());
            dsc.AppendLine(langCodePatternGroups.Localize(string.Join(commaSeparator, blockBanner.PatternGroups.Select(group => $"{langCodePatternGroup}{group}".Localize()))));
            Patterns.GetDescription(dsc, withDebugInfo);
            Cutouts.GetDescription(dsc, withDebugInfo);
            Modes.GetDescription(dsc, withDebugInfo);
        }
    }

    public BannerProperties FromTreeAttribute(ITreeAttribute tree, string defaultType, Dictionary<string, string> defaultModes)
    {
        Modes.FromTreeAttribute(tree, defaultModes);

        ITreeAttribute bannerTree = GetBannerTree(tree);
        Patterns.FromTreeAttribute(bannerTree);
        Cutouts.FromTreeAttribute(bannerTree);
        Name = bannerTree.GetString(attributeName, Name);
        Placement = bannerTree.GetString(attributePlacement, Placement);

        if (!string.IsNullOrEmpty(defaultType))
        {
            Placement ??= defaultType;
        }
        return this;
    }

    public void ToTreeAttribute(ITreeAttribute tree, bool setPlacement = true)
    {
        Modes.ToTreeAttribute(tree);

        ITreeAttribute bannerTree = GetBannerTree(tree);
        Patterns.ToTreeAttribute(bannerTree);
        Cutouts.ToTreeAttribute(bannerTree);
        if (!string.IsNullOrEmpty(Name))
        {
            bannerTree.SetString(attributeName, Name);
        }
        if (setPlacement)
        {
            SetPlacement(tree, Placement);
        }
    }

    public static BannerProperties FromStack(ItemStack stack)
    {
        return new BannerProperties().FromTreeAttribute(stack.Attributes,
            defaultType: (stack.Collectible as BlockBanner).DefaultPlacement,
            defaultModes: (stack.Collectible as BlockBanner).DefaultModes);
    }

    public void ToStack(ItemStack stack)
    {
        ToTreeAttribute(stack.Attributes, false);
    }

    public bool CopyFrom(ItemStack fromStack, bool copyLayers = false, bool copyCutouts = false)
    {
        bool layersSuccess = copyLayers;
        bool cutoutsSuccess = copyCutouts;

        if (copyLayers) layersSuccess = Patterns.CanCopyFrom(fromStack);
        if (copyCutouts) cutoutsSuccess = Cutouts.CanCopyFrom(fromStack);

        if (layersSuccess || cutoutsSuccess)
        {
            if (layersSuccess) Patterns.CopyFrom(fromStack);
            if (cutoutsSuccess) Cutouts.CopyFrom(fromStack);
            FromTreeAttribute(fromStack.Attributes,
                defaultType: (fromStack.Collectible as BlockBanner).DefaultPlacement,
                defaultModes: (fromStack.Collectible as BlockBanner).DefaultModes);
            return true;
        }
        return false;
    }

    public bool CopyTo(ItemStack toStack, bool copyLayers = false, bool copyCutouts = false)
    {
        bool layersSuccess = copyLayers;
        bool cutoutsSuccess = copyCutouts;

        if (copyLayers) layersSuccess = Patterns.CanCopyTo(toStack);
        if (copyCutouts) cutoutsSuccess = Cutouts.CanCopyTo(toStack);

        if (layersSuccess || cutoutsSuccess)
        {
            if (layersSuccess) Patterns.CopyTo(toStack);
            if (cutoutsSuccess) Cutouts.CopyTo(toStack);
            ToTreeAttribute(toStack.Attributes);
            return true;
        }
        return false;
    }

    public void SetPlacement(string placement)
    {
        Placement = placement;
    }

    public void SetName(string name)
    {
        Name = name;
    }

    public static void SetPlacement(ITreeAttribute tree, string placement)
    {
        GetBannerTree(tree).SetString(attributePlacement, placement);
    }

    public static void ClearPlacement(ITreeAttribute tree)
    {
        GetBannerTree(tree).RemoveAttribute(attributePlacement);
    }

    public static ITreeAttribute GetBannerTree(ITreeAttribute tree) => tree.GetOrAddTreeAttribute(attributeBanner);

    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        result.Append(Name);
        result.Append('-');
        result.Append(Placement);
        result.Append('-');
        result.Append(Patterns.ToString());
        result.Append('-');
        result.Append(Cutouts.ToString());
        result.Append('-');
        result.Append(Modes.ToString());
        return result.ToString();
    }
}