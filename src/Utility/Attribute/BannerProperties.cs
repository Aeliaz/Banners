using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Flags;

public class BannerProperties
{
    public string Name { get; protected set; }
    public string Placement { get; protected set; }

    public Patterns Patterns { get; protected set; } = new();
    public Cutouts Cutouts { get; protected set; } = new();

    public BannerProperties(string defaultPlacement)
    {
        if (!string.IsNullOrEmpty(defaultPlacement))
        {
            Placement ??= defaultPlacement;
        }
    }

    public void GetDescription(StringBuilder dsc, bool withDebugInfo = false)
    {
        Patterns.GetDescription(dsc, withDebugInfo);
        Cutouts.GetDescription(dsc, withDebugInfo);
    }

    public BannerProperties FromTreeAttribute(ITreeAttribute tree)
    {
        ITreeAttribute bannerTree = GetBannerTree(tree);
        Patterns.FromTreeAttribute(bannerTree);
        Cutouts.FromTreeAttribute(bannerTree);
        Name = bannerTree.GetString(attributeName, Name);
        Placement = bannerTree.GetString(attributePlacement, Placement);
        return this;
    }

    public void ToTreeAttribute(ITreeAttribute tree, bool setPlacement = true)
    {
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

    public static BannerProperties FromStack(ItemStack stack, BlockBanner blockBanner = null)
    {
        string defaultType = blockBanner?.DefaultPlacement ?? (stack.Collectible as BlockBanner)?.DefaultPlacement;
        return new BannerProperties(defaultType).FromTreeAttribute(stack.Attributes);
    }

    public void ToStack(ItemStack stack)
    {
        ToTreeAttribute(stack.Attributes, false);
    }

    public bool CopyFrom(ItemStack fromStack, bool copyLayers = false, bool copyCutouts = false)
    {
        bool layersSuccess = copyLayers;
        bool cutoutsSuccess = copyCutouts;

        if (copyLayers) layersSuccess = Patterns.CopyFrom(fromStack);
        if (copyCutouts) cutoutsSuccess = Cutouts.CopyFrom(fromStack);

        if (layersSuccess || cutoutsSuccess)
        {
            FromTreeAttribute(fromStack.Attributes);
        }

        return layersSuccess || cutoutsSuccess;
    }

    public bool CopyTo(ItemStack toStack, bool copyLayers = false, bool copyCutouts = false)
    {
        bool layersSuccess = copyLayers;
        bool cutoutsSuccess = copyCutouts;

        if (copyLayers) layersSuccess = Patterns.CopyTo(toStack);
        if (copyCutouts) cutoutsSuccess = Cutouts.CopyTo(toStack);

        if (layersSuccess || cutoutsSuccess)
        {
            ToTreeAttribute(toStack.Attributes);
        }

        return layersSuccess || cutoutsSuccess;
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
        tree.GetOrAddTreeAttribute(attributeBanner).SetString(attributePlacement, placement);
    }

    public static void ClearPlacement(ITreeAttribute tree)
    {
        tree.GetOrAddTreeAttribute(attributeBanner).RemoveAttribute(attributePlacement);
    }

    public static ITreeAttribute GetBannerTree(ITreeAttribute tree) => tree.GetOrAddTreeAttribute(attributeBanner);

    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        result.Append(Name);
        result.Append('-');
        result.Append(Placement);
        result.Append(Patterns.ToString());
        result.Append(Cutouts.ToString());
        return result.ToString();
    }
}