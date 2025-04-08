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
            Plugin.Log.LogInfo("Match started, Identifier is attatching to the player.");
            Singleton<GameWorld>.Instance.MainPlayer.GetOrAddComponent<IdentifierManager2>();
        }
    }
}
