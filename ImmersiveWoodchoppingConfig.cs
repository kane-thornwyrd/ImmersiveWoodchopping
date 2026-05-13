using ProtoBuf;
using System.ComponentModel;

namespace ImmersiveWoodchopping
{
    [ProtoContract]
    public class ImmersiveWoodchoppingConfig
    {
        [ProtoMember(1), DefaultValue(false)] public bool AutoLogPlacement = false;
        [ProtoMember(2), DefaultValue(false)] public bool DamageToolOnChop = false;
        [ProtoMember(3)] public int IntsaChopMinTier = 1;
        [ProtoMember(4), DefaultValue(true)] public bool DisableGridRecipe = true;
        [ProtoMember(5), DefaultValue(4)] public int DropAmount = 4;
        public ImmersiveWoodchoppingConfig()
        {

        }

        public ImmersiveWoodchoppingConfig(ImmersiveWoodchoppingConfig previousConfig)
        {
            AutoLogPlacement = previousConfig.AutoLogPlacement;
            DamageToolOnChop = previousConfig.DamageToolOnChop;
            IntsaChopMinTier = previousConfig.IntsaChopMinTier;
            DisableGridRecipe = previousConfig.DisableGridRecipe;
            DropAmount = previousConfig.DropAmount;
        }
    }
}