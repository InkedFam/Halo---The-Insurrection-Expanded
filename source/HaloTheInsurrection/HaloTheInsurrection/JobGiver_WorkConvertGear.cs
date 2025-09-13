using Verse;
using Verse.AI;
using RimWorld;

namespace HaloTheInsurrection
{
    public class JobGiver_WorkConvertGear : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            // Only pawns who can manipulate and are not drafted
            if (!pawn.CanReserveAndReach(FindGearToConvert(pawn), PathEndMode.Touch, Danger.Deadly, 1))
                return null;

            var gear = FindGearToConvert(pawn);
            if (gear == null)
                return null;

            // Create the convert job on the gear
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("ConvertGear"), gear);
        }

        private Apparel FindGearToConvert(Pawn pawn)
        {
            // Search for nearby gear to convert that is not reserved
            // Here you can improve by adding filtering for your gear defs or map

            var map = pawn.Map;
            if (map == null)
                return null;

            return (Apparel)GenClosest.ClosestThingReachable(
                pawn.Position,
                map,
                ThingRequest.ForDef(DefDatabase<ThingDef>.AllDefsListForReading.Find(def =>
                    def.defName.StartsWith("HALO_UNSC_") || def.defName.StartsWith("HALO_INS_") && def.IsApparel)),
                PathEndMode.Touch,
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false),
                30f,
                thing => !pawn.Map.reservationManager.IsReserved(thing, pawn)
            );
        }
    }
}