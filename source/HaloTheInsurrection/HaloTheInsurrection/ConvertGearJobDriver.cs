using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;

namespace HaloTheInsurrection
{
    public class ConvertGearJobDriver : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserve the gear to convert
            return this.pawn.Reserve(this.TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Go to the gear
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Convert gear toil
            var convertToil = new Toil();
            convertToil.initAction = () =>
            {
                var gear = TargetA.Thing as Apparel;
                if (gear == null)
                    return;

                var currentDef = gear.def.defName;
                string targetDefName = null;
                if (currentDef.StartsWith("HALO_UNSC_"))
                    targetDefName = currentDef.Replace("HALO_UNSC_", "HALO_INS_");
                else if (currentDef.StartsWith("HALO_INS_"))
                    targetDefName = currentDef.Replace("HALO_INS_", "HALO_UNSC_");
                else
                    return;

                var targetDef = DefDatabase<ThingDef>.GetNamedSilentFail(targetDefName);
                if (targetDef == null)
                    return;

                var map = gear.Map;
                var position = gear.Position;

                // Destroy old gear
                gear.Destroy();

                // Spawn new gear
                var newGear = ThingMaker.MakeThing(targetDef);
                GenSpawn.Spawn(newGear, position, map);

                Messages.Message($"Gear converted to {targetDef.label}", MessageTypeDefOf.PositiveEvent);
            };
            convertToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return convertToil;
        }
    }
}