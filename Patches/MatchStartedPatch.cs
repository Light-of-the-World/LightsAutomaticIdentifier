using Comfort.Common;
using EFT;
using HarmonyLib;
using LightsAutomaticIdentiier;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LightsAutomaticIdentifier.Patches
{
    internal class MatchStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        private static void Postfix(GameWorld __instance)
        {
            if (__instance.LocationId.ToLower() == "hideout") return;
            if (__instance is HideoutGameWorld) return;
            Plugin.Log.LogInfo("Match started, Identifier is attatching to the player. We are additionally gathering current Attention and Perception levels.");
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            IdentifierManager2 manager = player.GetOrAddComponent<IdentifierManager2>();
            IdentifierManager2.isRaidOver = false;
            int attentionLevel = player.Skills.Attention.Level;
            int perceptionLevel = player.Skills.Perception.Level;
            int searchLevel = player.Skills.Search.Level;
            bool isAttentionElite = player.Skills.Attention.IsEliteLevel;
            bool isPerceptionElite = player.Skills.Perception.IsEliteLevel;
            bool isSearchElite = player.Skills.Search.IsEliteLevel;
            Plugin.Log.LogInfo($"Attention level was listed as {attentionLevel}, Perception level was listed as {perceptionLevel}, and Search level was listed as {searchLevel}. Adjusting identification times accordingly.");
            manager.SetCombinedDistances( attentionLevel, perceptionLevel, searchLevel, isAttentionElite, isPerceptionElite, isSearchElite );
        }
    }

    internal class MatchEndedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.UnregisterPlayer));
        }

        [PatchPostfix]

        private static void Postfix(GameWorld __instance, ref IPlayer iPlayer)
        {
            if (__instance.LocationId.ToLower() == "hideout") return;
            if (__instance is HideoutGameWorld) return;
            if (iPlayer.ProfileId == ClientAppUtils.GetClientApp().GetClientBackEndSession().Profile.ProfileId && !IdentifierManager2.isRaidOver)
            {
                IdentifierManager2.isRaidOver = true;
                Plugin.Log.LogInfo("Match over, Identifier is removing itself.");
                Player player = Singleton<GameWorld>.Instance.MainPlayer;
                IdentifierManager2 manager = player.GetComponent<IdentifierManager2>();
                manager.enabled = false;
            }
        }
    }
}
