using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace HaloTheInsurrection
{
    [StaticConstructorOnStartup]
    public static class PawnApparelReplacer
    {
        static PawnApparelReplacer()
        {
            var harmony = new Harmony("HaloTheInsurrection");
            harmony.Patch(
                original: AccessTools.Method(typeof(PawnGenerator), "GenerateGearFor"),
                postfix: new HarmonyMethod(typeof(PawnApparelReplacer), nameof(Postfix_GenerateGearFor))
            );
        }

        public static void Postfix_GenerateGearFor(Pawn pawn)
        {
            if (pawn?.Faction?.def?.defName != "HALO_UNSC_Insurgents_Faction" || pawn.apparel == null)
                return;

            var replacements = new Dictionary<string, string>
            {
                { "HALO_UNSC_MarineHelmet_Regular", "HALO_INS_MarineHelmet_Regular" },
                { "HALO_UNSC_MarineHelmet_Advanced", "HALO_INS_MarineHelmet_Advanced" },
                { "HALO_UNSC_MarineHelmet_Marksman", "HALO_INS_MarineHelmet_Marksman" },
                { "HALO_UNSC_MarineHelmet_Reinforced", "HALO_INS_MarineHelmet_Reinforced" },
                { "HALO_UNSC_MarineArmor", "HALO_INS_MarineArmor" },
                { "HALO_UNSC_MjolnirHelmet", "HALO_INS_MjolnirHelmet" },
                { "HALO_UNSC_MjolnirArmor", "HALO_INS_MjolnirArmor" },
                { "HALO_UNSC_ODSTHelmet", "HALO_INS_ODSTHelmet" },
                { "HALO_UNSC_ODSTArmor", "HALO_INS_ODSTArmor" }
            };

            var toReplace = pawn.apparel.WornApparel
                .Where(a => replacements.ContainsKey(a.def.defName))
                .ToList();

            foreach (var apparel in toReplace)
            {
                var replacementDef = DefDatabase<ThingDef>.GetNamedSilentFail(replacements[apparel.def.defName]);
                if (replacementDef != null)
                {
                    pawn.apparel.Remove(apparel);
                    var newApparel = ThingMaker.MakeThing(replacementDef) as Apparel;
                    pawn.apparel.Wear(newApparel);
                }
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class ApparelGizmoPatch
    {
        static ApparelGizmoPatch()
        {
            var harmony = new Harmony("HaloTheInsurrection");
            harmony.Patch(
                original: AccessTools.Method(typeof(ThingWithComps), "GetGizmos"),
                postfix: new HarmonyMethod(typeof(ApparelGizmoPatch), nameof(GetGizmosPostfixHalo))
            );
        }

        // This postfix adds your custom gizmo if conditions are met
        public static IEnumerable<Gizmo> GetGizmosPostfixHalo(IEnumerable<Gizmo> __result, ThingWithComps __instance)
        {
            foreach (var gizmo in __result)
                yield return gizmo;

            var apparel = __instance as Apparel;
            if (apparel == null)
                yield break;

            var validDefs = new HashSet<string>
            {
                "HALO_UNSC_MarineHelmet_Regular",
                "HALO_UNSC_MarineHelmet_Advanced",
                "HALO_UNSC_MarineHelmet_Marksman",
                "HALO_UNSC_MarineHelmet_Reinforced",
                "HALO_UNSC_MarineArmor",
                "HALO_UNSC_MjolnirHelmet",
                "HALO_UNSC_MjolnirArmor",
                "HALO_UNSC_ODSTHelmet",
                "HALO_UNSC_ODSTArmor",
                "HALO_INS_MarineHelmet_Regular",
                "HALO_INS_MarineHelmet_Advanced",
                "HALO_INS_MarineHelmet_Marksman",
                "HALO_INS_MarineHelmet_Reinforced",
                "HALO_INS_MarineArmor",
                "HALO_INS_MjolnirHelmet",
                "HALO_INS_MjolnirArmor",
                "HALO_INS_ODSTHelmet",
                "HALO_INS_ODSTArmor"
            };

            if (!validDefs.Contains(apparel.def.defName))
                yield break;

            yield return new Command_Action
            {
                defaultLabel = "Convert Gear",
                defaultDesc = "Convert this gear between UNSC and INS versions.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/ConvertGear", true),
                action = () =>
                {
                    var currentDef = apparel.def.defName;
                    string targetDefName = null;
                    if (currentDef.StartsWith("HALO_UNSC_"))
                        targetDefName = currentDef.Replace("HALO_UNSC_", "HALO_INS_");
                    else if (currentDef.StartsWith("HALO_INS_"))
                        targetDefName = currentDef.Replace("HALO_INS_", "HALO_UNSC_");
                    else
                    {
                        Messages.Message("Cannot convert this gear.", MessageTypeDefOf.RejectInput);
                        return;
                    }

                    var targetDef = DefDatabase<ThingDef>.GetNamedSilentFail(targetDefName);
                    if (targetDef == null)
                    {
                        Messages.Message($"Conversion target not found: {targetDefName}", MessageTypeDefOf.RejectInput);
                        return;
                    }

                    var map = apparel.Map;
                    var position = apparel.Position;

                    apparel.Destroy();

                    var newGear = ThingMaker.MakeThing(targetDef);

                    // Preserve quality
                    if (apparel.TryGetComp<CompQuality>() != null && newGear.TryGetComp<CompQuality>() != null)
                        newGear.TryGetComp<CompQuality>().SetQuality(apparel.TryGetComp<CompQuality>().Quality, ArtGenerationContext.Outsider);

                    // Preserve hitpoints
                    newGear.HitPoints = apparel.HitPoints;

                    //var oldArtComp = apparel.TryGetComp<CompArt>();
                    //var newArtComp = newGear.TryGetComp<CompArt>();

                    //if (oldArtComp != null)
                   // {
                        // Copy saved data using reflection:
                   //     var artistField = typeof(CompArt).GetField("artist", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                   //     var titleField = typeof(CompArt).GetField("title", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                   //     var artStateField = typeof(CompArt).GetField("artState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                  //      if (artistField != null && titleField != null && artStateField != null)
                   //     {
                  //          artistField.SetValue(newArtComp, artistField.GetValue(oldArtComp));
                  //          titleField.SetValue(newArtComp, titleField.GetValue(oldArtComp));
                  //          artStateField.SetValue(newArtComp, artStateField.GetValue(oldArtComp));
                  //      }
                  //  }

                    GenSpawn.Spawn(newGear, position, map);

                    Messages.Message($"Converted gear to {targetDef.label}", MessageTypeDefOf.PositiveEvent);
                }
            };
        }
    }
}
