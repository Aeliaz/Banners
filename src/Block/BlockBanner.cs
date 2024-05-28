using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Flags;

public class BlockBanner : Block
{
    public List<string> PatternGroups { get; protected set; } = new();

    public Dictionary<string, CompositeShape> CustomShapes { get; protected set; } = new();
    public Dictionary<string, CompositeTexture> CustomTextures { get; protected set; } = new();
    public Dictionary<string, List<string>> IgnoredTextureCodes { get; protected set; } = new();
    public List<string> TextureCodesForOverlays { get; protected set; } = new();

    public Dictionary<string, Cuboidf[]> CustomSelectionBoxes { get; protected set; } = new();
    public Dictionary<string, Cuboidf[]> CustomCollisionBoxes { get; protected set; } = new();

    public string DefaultPlacement { get; protected set; }
    public string DefaultHorizontalPlacement { get; protected set; }
    public string DefaultVerticalPlacement { get; protected set; }

    public bool ShowDebugInfo { get; protected set; }

    public string TopTexturePrefix { get; protected set; }
    public List<string> Colors { get; protected set; } = new();
    public List<string> IgnoreForGeneratingTextures { get; protected set; } = new();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        LoadTypes();
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);
        PatternGroups.Clear();
        CustomShapes.Clear();
        CustomTextures.Clear();
        IgnoredTextureCodes.Clear();
        TextureCodesForOverlays.Clear();
        Colors.Clear();
        IgnoreForGeneratingTextures.Clear();
        CustomSelectionBoxes.Clear();
        CustomCollisionBoxes.Clear();
    }

    public void LoadTypes()
    {
        PatternGroups = Attributes[attributePatternGroups].AsObject<List<string>>();

        CustomShapes = Attributes[attributeShapes].AsObject<Dictionary<string, CompositeShape>>();
        CustomTextures = Attributes[attributeTextures].AsObject<Dictionary<string, CompositeTexture>>();
        IgnoredTextureCodes = Attributes[attributeIgnoredTextureCodesForOverlays].AsObject<Dictionary<string, List<string>>>();
        TextureCodesForOverlays = Attributes[attributeTextureCodesForOverlays].AsObject<List<string>>();

        CustomSelectionBoxes = Attributes[attributeSelectionBoxes].AsObject<Dictionary<string, Cuboidf[]>>();
        CustomCollisionBoxes = Attributes[attributeCollisionBoxes].AsObject<Dictionary<string, Cuboidf[]>>();

        DefaultPlacement = Attributes[attributeDefaultPlacement].AsString();
        DefaultHorizontalPlacement = Attributes[attributeDefaultHorizontalPlacement].AsString();
        DefaultVerticalPlacement = Attributes[attributeDefaultVerticalPlacement].AsString();

        ShowDebugInfo = Attributes[attributeShowDebugInfo].AsBool();

        TopTexturePrefix = Attributes[attributeTopTexturePrefix].AsString();
        Colors = Attributes[attributeColors].AsObject<List<string>>();
        IgnoreForGeneratingTextures = Attributes[attributeIgnoredTextureCodesForGeneratingTextures].AsObject<List<string>>();
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder sb, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, sb, world, withDebugInfo);
        sb.AppendLine(langCodePatternGroups.Localize(string.Join(", ", PatternGroups.Select(group => group))));
        BannerProperties.FromStack(inSlot.Itemstack, this).GetDescription(sb, ShowDebugInfo);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
    {
        return new ItemStack[1] { OnPickBlock(world, pos) };
    }

    public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
    {
        BlockDropItemStack[] drops = base.GetDropsForHandbook(handbookStack, forPlayer);
        drops[0] = drops[0].Clone();
        drops[0].ResolvedItemstack.SetFrom(handbookStack);
        return drops;
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return blockAccessor.GetBlockEntity(pos) is BlockEntityBanner be ? be.GetOrCreateSelectionBoxes() : base.GetSelectionBoxes(blockAccessor, pos);
    }

    public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return blockAccessor.GetBlockEntity(pos) is BlockEntityBanner be ? be.GetOrCreateCollisionBoxes() : base.GetCollisionBoxes(blockAccessor, pos);
    }

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
    {
        bool place = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        if (place && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBanner be)
        {
            IRotatableBanner rotatableBanner = GetInterface<IRotatableBanner>(world, blockSel.Position);
            rotatableBanner?.RotateWhenPlaced(world, byPlayer, blockSel, byItemStack);
            if (blockSel.Face.IsHorizontal)
            {
                BannerProperties.SetPlacement(byItemStack.Attributes, DefaultHorizontalPlacement);
            }
            else if (blockSel.Face.IsVertical)
            {
                BannerProperties.SetPlacement(byItemStack.Attributes, DefaultVerticalPlacement);
            }
            be.OnBlockPlaced(byItemStack);
        }
        return place;
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        ItemStack stack = base.OnPickBlock(world, pos);
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBanner be)
        {
            be.BannerProps.ToStack(stack);
        }
        return stack;
    }

    public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBanner be)
        {
            MeshData decalMesh = this.GetOrCreateMesh(api, be.BannerProps, decalTexSource);
            MeshData blockMesh = this.GetOrCreateMesh(api, be.BannerProps);
            IRotatableBanner rotatableBanner = GetInterface<IRotatableBanner>(world, pos);
            decalModelData = rotatableBanner?.RotatedMesh(decalMesh) ?? decalMesh;
            blockModelData = rotatableBanner?.RotatedMesh(blockMesh) ?? blockMesh;
        }
        else
        {
            base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
        }
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        this.GetInventoryMesh(capi, itemstack, renderinfo);
    }
}