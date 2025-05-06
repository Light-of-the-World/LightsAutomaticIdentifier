using BepInEx;
using BepInEx.Logging;
using LightsAutomaticIdentifier.Patches;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
using LightsAutomaticIdentiier.Codebase;
using LightsAutomaticIdentiier.Patches;
*/

namespace LightsAutomaticIdentiier
{
    [BepInPlugin("Light.LightsAutomaticIdentiier", "LightsAutomaticIdentiier", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        // BaseUnityPlugin inherits MonoBehaviour, so you can use base unity functions like Awake() and Update()
        private void Awake()
        {
            // save the Logger to variable so we can use it elsewhere in the project
            Log = Logger;
            new MatchStartedPatch().Enable();
            new MatchEndedPatch().Enable();
            Log.LogWarning("Identifer Loaded");
            //Don't forget to enable patches here!
        }
    }
}
