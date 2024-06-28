using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Flags.ToolModes.BannerPattern;

public class PatternToolMode
{
    public string Pattern { get; set; }
    public bool Linebreak { get; set; }
    public Unlockable[] Unlockable { get; set; } = null;

    public SkillItem GetToolMode(ICoreClientAPI capi, ItemSlot slot)
    {
        ItemStack newStack = slot.Itemstack.Clone();
        SetPattern(newStack);
        bool unlocked = IsUnlocked(slot) || capi.World.Player.IsCreative();

        SkillItem toolMode = new SkillItem()
        {
            Linebreak = Linebreak,
            Name = GetName(unlocked)
        };

        if (unlocked)
        {
            toolMode.RenderHandler = newStack.RenderItemStack(capi, showStackSize: false);
        }
        else
        {
            toolMode.Texture = ObjectCacheUtil.TryGet<LoadedTexture>(capi, cacheKeyQuestionTexture);
        }

        return toolMode;
    }

    private string GetName(bool unlocked)
    {
        if (unlocked)
        {
            return $"{langCodePattern}{Pattern}".Localize();
        }
        else
        {
            return langCodePatternLocked.Localize();
        }
    }

    public static SkillItem[] GetToolModes(ICoreClientAPI capi, ItemSlot slot, IEnumerable<PatternToolMode> modes)
    {
        return modes?.Select(x => x.GetToolMode(capi, slot)).ToArray();
    }

    public void SetPattern(ItemStack stack)
    {
        PatternProperties props = PatternProperties.FromStack(stack);
        props.SetType(Pattern);
        props.ToTreeAttribute(stack.Attributes);
    }

    public bool TryUnlock(ItemSlot slot, ItemSlot mouseSlot, bool skipStack = false)
    {
        PatternProperties props = PatternProperties.FromStack(slot.Itemstack);
        if ((skipStack && props.Type == Pattern) || Unlockable.Any(x => x.Matches(mouseSlot?.Itemstack)))
        {
            props.SetUnlockedTypes(Pattern);
            props.ToTreeAttribute(slot.Itemstack.Attributes);
            slot.MarkDirty();
            return true;
        }
        else if (mouseSlot?.Itemstack?.Collectible is ItemBannerPattern)
        {
            PatternProperties otherProps = PatternProperties.FromStack(mouseSlot.Itemstack);
            otherProps.MergeTypes(props);
            props.MergeTypes(otherProps);
            props.ToTreeAttribute(slot.Itemstack.Attributes);
            otherProps.ToTreeAttribute(mouseSlot.Itemstack.Attributes);
            slot.MarkDirty();
            mouseSlot.MarkDirty();
            return true;
        }
        return false;
    }

    public static bool TryUnlockAll(IEnumerable<PatternToolMode> modes, ItemSlot slot, ItemSlot mouseSlot, bool skipStack = false)
    {
        bool any = false;
        foreach (PatternToolMode toolMode in modes.Where(toolMode => !toolMode.IsUnlocked(slot)))
        {
            if (toolMode.TryUnlock(slot, mouseSlot, skipStack))
            {
                any = true;
            }
        }
        return any;
    }

    public bool IsUnlocked(ItemSlot slot)
    {
        return Unlockable == null || (Unlockable != null && PatternProperties.FromStack(slot.Itemstack).IsUnlocked(Pattern));
    }
}

public class Unlockable
{
    public string Code { get; set; }
    public EnumItemClass Type { get; set; }
    public Dictionary<string, string> HasAttributes { get; set; } = new();

    public bool Matches(ItemStack stack)
    {
        return stack != null && !string.IsNullOrEmpty(Code)
            && AssetLocation.Create(Code).Equals(stack.Collectible.Code)
            && Type == stack.Class
            && (HasAttributes == null || (HasAttributes != null && HasAttributes.All(x => stack.Attributes.GetAsString(x.Key) == x.Value)));
    }
}