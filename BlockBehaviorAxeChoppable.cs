using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace ImmersiveWoodchopping
{
    public class BlockBehaviorAxeChoppable : BlockBehavior
    {
        public static Dictionary<string, List<AssetLocation>> VariantsByType = new Dictionary<string, List<AssetLocation>>();
        
        bool hideInteractionHelpInSurvival;
        public AssetLocation drop;
        
        
        
        private static List<ItemStack> axeItems = new List<ItemStack>();

        public BlockBehaviorAxeChoppable(Block block) : base (block)
        {
            
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
        {
            if(hideInteractionHelpInSurvival && forPlayer?.WorldData.CurrentGameMode == EnumGameMode.Survival)return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling);
            handling = EnumHandling.PassThrough;
            if (axeItems.Count == 0)   // This is a potentially rather slow wildcard search of all items (especially if mods add many items) therefore we want to run this only once per game
            {
                Item[] axes = world.SearchItems(new AssetLocation("axe-felling-*"));
                foreach (Item item in axes) axeItems.Add(new ItemStack(item));
                axeItems.Add(new ItemStack(world.GetItem(new AssetLocation("axe-scrap-scrap"))));
            }

            bool notProtected = true;

            if (world.Claims != null && world is IClientWorldAccessor clientWorld && clientWorld.Player?.WorldData.CurrentGameMode == EnumGameMode.Survival)
            {
                EnumWorldAccessResponse resp = world.Claims.TestAccess(clientWorld.Player, selection.Position, EnumBlockAccessFlags.BuildOrBreak);
                if (resp != EnumWorldAccessResponse.Granted) notProtected = false;
            }

            if (axeItems.Count > 0 && notProtected)
            {
                return new WorldInteraction[] { new WorldInteraction()
                {
                    ActionLangCode = Constants.ModId + ":" + "blockinteract-chop",
                    Itemstacks = axeItems.ToArray(),
                    MouseButton = EnumMouseButton.Right
                } };
            }
            else return new WorldInteraction[0];
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            hideInteractionHelpInSurvival = properties["hideInteractionHelpInSurvival"].AsBool(false);
            drop = new AssetLocation(properties["drop"].ToString());
        }

        

        public override void OnUnloaded(ICoreAPI api)
        {
            axeItems.Clear();
        }        
        
    }
    
}