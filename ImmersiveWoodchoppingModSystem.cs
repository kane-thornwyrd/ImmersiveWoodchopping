using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Threading.Channels;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace ImmersiveWoodchopping
{
    public class ImmersiveWoodchoppingModSystem : ModSystem
    {
        ModConfig config = new ModConfig();
        public readonly Dictionary<string, CraftingRecipeIngredient> choppingRecipes = new();
        public readonly List<string> choppingMaterials = new();

        private IServerNetworkChannel schannel;
        private IClientNetworkChannel cchannel;
        private ICoreAPI _api = null;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            _api = api;
            config.ReadOrGenerateConfig(api);

            if (api is ICoreServerAPI sApi)
            {
                schannel = sApi.Network.RegisterChannel(Constants.ModId + "-syncconfig")
                    .RegisterMessageType<ImmersiveWoodchoppingConfig>();
            }
            if (api is ICoreClientAPI cApi)
            {
                cchannel = cApi.Network.RegisterChannel(Constants.ModId + "-syncconfig")
                    .RegisterMessageType<ImmersiveWoodchoppingConfig>()
                    .SetMessageHandler<ImmersiveWoodchoppingConfig>(OnSyncConfigReceived);
            }
        }

        private void OnSyncConfigReceived(ImmersiveWoodchoppingConfig modConfig)
        {
            config.config = modConfig;
            config.SetWorldConfig(_api as ICoreClientAPI);
        }
        public override void Start(ICoreAPI api)
        {
            api.RegisterCollectibleBehaviorClass("WoodChopping", typeof(WoodChopping));
            api.RegisterBlockBehaviorClass("AxeChoppable", typeof(BlockBehaviorAxeChoppable));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.PlayerJoin += byPlayer =>
            {
                schannel.SendPacket(config.Clone().config, byPlayer);
            };
        }

        public override double ExecuteOrder()
        {
            return 1;
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            GenerateFirewoodRecipeList(api);
            AssignHandbookAttributes(api);
            AssingDrops(api);

            if (api.Side == EnumAppSide.Server)
            {
                foreach (var item in api.World.Items)
                {
                    if (item.Code == null) continue;
                    if (item.Tool == EnumTool.Axe && !WildcardUtil.Match("*-ruined", item.Code.Path))
                    //if (item.Code.Path.StartsWith("axe-") && !WildcardUtil.Match("*-ruined", item.Code.Path))
                    {
                        item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new WoodChopping(item));
                        //Debug.WriteLine("Behavior added to: " + item.Code);
                    }
                }
            }
            //Always check on which game side you add your behavior!
        }

        public void GenerateFirewoodRecipeList(ICoreAPI api)
        {
            foreach (var grecipe in api.World.GridRecipes)
            {
                if (!grecipe.Output.Code.Path.StartsWith("firewood")) continue;
                if (grecipe.ResolvedIngredients.Length != 2) continue;

                bool flag = false;
                if (grecipe.Width == 1 && grecipe.Height == 2)
                {
                    flag = true;
                }
                else if (grecipe.Width == 2 && grecipe.Height == 1)
                {
                    flag = true;
                }
                if (!flag) continue;

                AssetLocation icode;
                string ipath;
                string icodefirstpart;
                bool enabled = !api.World.Config.GetBool(Constants.ModId + ":DisableGridRecipe", true);

                foreach (CraftingRecipeIngredient ingredient in grecipe.ResolvedIngredients)
                {
                    icode = ingredient.Code;
                    ipath = icode.Path;
                    if (ingredient.Type != EnumItemClass.Block) continue;

                    icodefirstpart = icode.FirstCodePart();

                    string genVariant;

                    if (ingredient.AllowedVariants != null)
                    {
                        foreach (var variant in ingredient.AllowedVariants)
                        {
                            genVariant = icode.ToString().Replace("*", variant).Replace("-ne", "-*").Replace("-ud", "-*");
                            
                                //new AssetLocation(icode.Domain, icodefirstpart + "-" + variant.Replace("-ne-ud", "-*-*").Replace("-ud", "-*")).ToString();
                            if (!choppingRecipes.ContainsKey(genVariant))
                            {
                                choppingRecipes.Add(genVariant, grecipe.Output);
                            }
                            else
                            {
                                choppingRecipes[genVariant] = grecipe.Output;
                            }
                        }
                    }
                    else
                    {
                        genVariant = icode.ToString().Replace("-ne", "-*").Replace("-ud", "-*");
                        if (!choppingRecipes.ContainsKey(genVariant))
                        {
                            choppingRecipes.Add(genVariant, grecipe.Output);
                        }
                        else
                        {
                            choppingRecipes[genVariant] = grecipe.Output;
                        }
                    }

                    if (!choppingMaterials.Contains(icodefirstpart))
                    {
                        choppingMaterials.Add(icodefirstpart);
                    }
                    grecipe.Enabled = enabled;
                    grecipe.ShowInCreatedBy = enabled;
                }

            }
            /*foreach (string key in choppingRecipes.Keys)
            {
                Debug.WriteLine(key + " " + choppingRecipes[key].Code);
            }*/
        }

        public void AssingDrops(ICoreAPI api)
        {
            foreach (var block in api.World.Blocks)
            {
                if (block.Code == null) continue;
                if (!choppingMaterials.Contains(block.Code.FirstCodePart())) continue;

                CraftingRecipeIngredient firewoodResult = null;

                foreach (var key in choppingRecipes.Keys)
                {
                    if (WildcardUtil.Match(new AssetLocation(key), block.Code))
                    {
                        firewoodResult = choppingRecipes[key];
                    }
                }
                //Debug.WriteLine(block.Code);
                if (firewoodResult != null)
                {
                    BlockBehaviorAxeChoppable behaviour = new BlockBehaviorAxeChoppable(block);
                    JObject jobj = new JObject
                        {
                            { "hideInteractionHelpInSurvival", new JValue(false) },
                            { "drop", new JValue(firewoodResult.Code.ToString()) },
                        };
                    behaviour.Initialize(new JsonObject(jobj));

                    block.BlockBehaviors = block.BlockBehaviors.Append(behaviour);
                    //Debug.WriteLine("Found " + block.Code);
                    /*if (api.World.Config.TryGetBool("logInOffhandChopping") == true && block.StorageFlags < EnumItemStorageFlags.Offhand)
                    {
                        block.StorageFlags = block.StorageFlags + (int)EnumItemStorageFlags.Offhand;
                    }*/
                }
            }
        }

        //Thank you Tyron for providing me with an example for patching JsonObject and JToken nightmare in Attributes 0_o
        public void AssignHandbookAttributes(ICoreAPI api)
        {
            /*
            List<AssetLocation> firewoodOutputs = new();
            foreach(var firewoodOutput in choppingRecipes.Values)
            {
                if (!firewoodOutputs.Contains(firewoodOutput.Code))
                {
                    firewoodOutputs.Add(firewoodOutput.Code);
                }
            }
            */
            foreach(var firewoodType in choppingRecipes.Values)
            {
                Item firewood = api.World.GetItem(firewoodType.Code);
                JToken token;
                if (firewood.Attributes?["handbook"].Exists != true)
                {
                    if (firewood.Attributes == null) firewood.Attributes = new JsonObject(JToken.Parse("{ handbook: {} }"));
                    else
                    {
                        token = firewood.Attributes.Token;
                        token["handbook"] = JToken.Parse("{ }");
                    }
                }

                token = firewood.Attributes["handbook"].Token;
                token["createdBy"] = JToken.FromObject(Constants.ModId + ":handbook-chopping-craftinfo");
            }
        }
    }
}
