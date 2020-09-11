using RimWorld;
using Verse;

namespace Geth
{
    [DefOf]
    public class GethCapacities
    {
        public static PawnCapacityDef GethCoolingEfficiency;
        public static PawnCapacityDef GethHeatGeneration;
        //public static PawnCapacityDef GethOverclock;

        static GethCapacities()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(GethCapacities));
        }
    }

    [DefOf]
    public class GethCoolingTags
    {
        public static BodyPartTagDef GethCoolantConduit;
        public static BodyPartTagDef GethCoolantPump;
        public static BodyPartTagDef GethHeatExchanger;

        static GethCoolingTags()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(GethCoolingTags));
        }
    }
}
