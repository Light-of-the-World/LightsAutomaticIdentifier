using Comfort.Common;
using EFT;
using LightsAutomaticIdentiier;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LightsAutomaticIdentifier.Patches
{
    internal class MatchStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPrefix]
        public static void PatchPrefix()
        {
            Plugin.Log.LogInfo("Match started, Identifier is attatching to the player. We are additionally gathering current Attention and Perception levels.");
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            IdentifierManager2 manager = player.GetOrAddComponent<IdentifierManager2>();
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
}
