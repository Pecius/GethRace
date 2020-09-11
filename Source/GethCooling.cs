using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Geth
{
    public class HediffGiver_GethCoolantLeak : HediffGiver
    {
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            HediffSet hediffSet = pawn.health.hediffSet;
            if (hediffSet.BleedRateTotal >= 0.1f)
            {
                HealthUtility.AdjustSeverity(pawn, hediff, hediffSet.BleedRateTotal * 0.001f);
            }
        }
    }

    public class HediffGiver_GethOverheat : HediffGiver_Heat
    {
        private HediffDef cooldownHediff;

        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            HediffSet hediffSet = pawn.health.hediffSet;

            Hediff cooldown = hediffSet.GetFirstHediffOfDef(cooldownHediff);
            if (cooldown != null)
            {
                // TODO: custom cooldown based on temperature, maybe?
                return;
            }

            Hediff overheat = hediffSet.GetFirstHediffOfDef(hediff);
            if (overheat != null && overheat.Severity >= hediff.maxSeverity)
            {
                pawn.health.RemoveHediff(overheat);
                HealthUtility.AdjustSeverity(pawn, cooldownHediff, 1f);
                return;
            }

            base.OnIntervalPassed(pawn, cause);
        }
    }

    public class PawnCapacityWorker_GethCoolingEfficiency : PawnCapacityWorker
    {
        public override float CalculateCapacityLevel(HediffSet diffSet, List<PawnCapacityUtility.CapacityImpactor> impactors = null)
        {
            BodyDef body = diffSet.pawn.RaceProps.body;
            float limbvalue = CalculateForLimbs(diffSet, BodyPartTagDefOf.MovingLimbCore, impactors);
            float torsoValue = CalculateForDirectChildren(diffSet, body.corePart, GethCoolingTags.GethCoolantConduit, impactors) *
                CalculateForDirectChildren(diffSet, body.corePart, GethCoolingTags.GethHeatExchanger, impactors);

            float value = limbvalue * .4f + torsoValue * .6f;
            return value * PawnCapacityUtility.CalculateTagEfficiency(diffSet, GethCoolingTags.GethCoolantPump, impactors: impactors);
        }

        public override bool CanHaveCapacity(BodyDef body)
        {
            return body.HasPartWithTag(GethCoolingTags.GethCoolantPump);
        }

        private float CalculateForDirectChildren(HediffSet diffSet, BodyPartRecord part, BodyPartTagDef partTag, List<PawnCapacityUtility.CapacityImpactor> impactors)
        {
            float value = 0;
            int partCount = 0;

            foreach (BodyPartRecord partWithTag in part.parts.Where(t => t.def.tags.Contains(partTag)))
            {
                value += PawnCapacityUtility.CalculateImmediatePartEfficiencyAndRecord(diffSet, partWithTag, impactors);
                partCount++;
            }

            if (partCount == 0)
                return 0f;

            return value / partCount;
        }

        private float CalculateForLimbs(HediffSet diffSet, BodyPartTagDef limbCore, List<PawnCapacityUtility.CapacityImpactor> impactors)
        {
            BodyDef body = diffSet.pawn.RaceProps.body;
            float totalValue = 0;
            int partCount = 0;

            foreach (BodyPartRecord limb in body.GetPartsWithTag(limbCore))
            {
                var conduitParts = limb.GetChildParts(GethCoolingTags.GethCoolantConduit);

                if (!conduitParts.Any())
                    continue;

                float value = 1;

                foreach (BodyPartRecord conduitPart in conduitParts)
                    value *= PawnCapacityUtility.CalculateImmediatePartEfficiencyAndRecord(diffSet, conduitPart, impactors);

                float heatExchangerValue = 0;
                int heatExchangerCount = 0;

                foreach (BodyPartRecord heatExchangerPart in limb.GetChildParts(GethCoolingTags.GethHeatExchanger))
                {
                    heatExchangerValue += PawnCapacityUtility.CalculateImmediatePartEfficiencyAndRecord(diffSet, heatExchangerPart, impactors);
                    heatExchangerCount++;
                }

                totalValue += value * (heatExchangerValue / heatExchangerCount);
                partCount++;
            }

            if (partCount == 0)
                return 0f;

            return totalValue / partCount;
        }
    }

    public class PawnCapacityWorker_GethHeatGeneration : PawnCapacityWorker
    {
        public override float CalculateCapacityLevel(HediffSet diffSet, List<PawnCapacityUtility.CapacityImpactor> impactors = null)
        {
            return CalculateCapacityAndRecord(diffSet, PawnCapacityDefOf.Consciousness, impactors);
        }

        public override bool CanHaveCapacity(BodyDef body)
        {
            return body.HasPartWithTag(GethCoolingTags.GethCoolantPump);
        }
    }

    public class StatPart_GethCoolingEfficiency : StatPart
    {
        private SimpleCurve heatCoefficientCurve;

        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;

            float coefficient = HeatCoefficient(pawn.health.capacities);
            if (coefficient != 0)
                return "StatsReport_Geth_CoolingEfficiency".Translate($"{(coefficient >= 0 ? '+' : '-')}x{coefficient.ToStringPercent()}");

            return null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn = req.Thing as Pawn;

            PawnCapacitiesHandler capacities = pawn?.health.capacities;

            if (capacities == null || !capacities.CapableOf(GethCapacities.GethCoolingEfficiency))
                return;

            val -= val * HeatCoefficient(capacities);
        }

        private float HeatCoefficient(PawnCapacitiesHandler capacities)
        {
            float x = capacities.GetLevel(GethCapacities.GethHeatGeneration) - capacities.GetLevel(GethCapacities.GethCoolingEfficiency);

            if (heatCoefficientCurve != null)
                return heatCoefficientCurve.Evaluate(x);

            return x;
        }
    }
}