using ProtoBuf;
using System.Security.Cryptography.X509Certificates;
using Vintagestory.API.Common;

namespace ImmersiveWoodchopping
{
    [ProtoContract]
    public class ModConfig
    {
        [ProtoMember(1)]
        public ImmersiveWoodchoppingConfig config;
        public void ReadOrGenerateConfig(ICoreAPI api)
        {
            try
            {
                config = api.LoadModConfig<ImmersiveWoodchoppingConfig>("ImmersiveWoodchoppingConfig.json");
                if (config == null)
                {
                    api.StoreModConfig(new ImmersiveWoodchoppingConfig(), "ImmersiveWoodchoppingConfig.json");
                    config = api.LoadModConfig<ImmersiveWoodchoppingConfig>("ImmersiveWoodchoppingConfig.json");
                }
                else
                {
                    api.StoreModConfig(new ImmersiveWoodchoppingConfig(config), "ImmersiveWoodchoppingConfig.json");
                }
            }
            catch
            {
                api.StoreModConfig(new ImmersiveWoodchoppingConfig(), "ImmersiveWoodchoppingConfig.json");
                config = api.LoadModConfig<ImmersiveWoodchoppingConfig>("ImmersiveWoodchoppingConfig.json");
            }

            /*try
            {
                config = api.LoadModConfig<ImmersiveWoodchoppingConfig>("ImmersiveWoodchoppingConfig.json");
            }
            catch
            {
                config = new ImmersiveWoodchoppingConfig();
            }
            finally
            {
                api.StoreModConfig(config, "ImmersiveWoodchoppingConfig.json");
            }*/

            SetWorldConfig(api);
        }

        public void SetWorldConfig(ICoreAPI api)
        {
            api.World.Config.SetBool(Constants.ModId + ":AutoLogPlacement", config.AutoLogPlacement);
            api.World.Config.SetBool(Constants.ModId + ":DamageToolOnChop", config.DamageToolOnChop);
            api.World.Config.SetInt(Constants.ModId + ":IntsaChopMinTier", config.IntsaChopMinTier);
            api.World.Config.SetBool(Constants.ModId + ":DisableGridRecipe", config.DisableGridRecipe);
            api.World.Config.SetInt(Constants.ModId + ":DropAmount", config.DropAmount);
        }

        public ModConfig Clone()
        {
            ModConfig cfg = new ModConfig();
            cfg.config = new ImmersiveWoodchoppingConfig(config);
            return cfg;
        }
    }
}