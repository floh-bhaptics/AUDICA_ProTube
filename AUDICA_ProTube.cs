using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Text.Json;

[assembly: MelonInfo(typeof(AUDICA_ProTube.AUDICA_ProTube), "AUDICA_ProTube", "1.1.0", "Florian Fahrenberger")]
[assembly: MelonGame("Harmonix Music Systems, Inc.", "Audica")]

namespace AUDICA_ProTube
{
    public class AUDICA_ProTube : MelonMod
    {
        public static string configPath = Directory.GetCurrentDirectory() + "\\UserData\\";
        public static bool dualWield = false;

        public override void OnInitializeMelon()
        {
            InitializeProTube();
        }

        public static void saveChannel(string channelName, string proTubeName)
        {
            string fileName = configPath + channelName + ".pro";
            File.WriteAllText(fileName, proTubeName, Encoding.UTF8);
        }

        public static string readChannel(string channelName)
        {
            string fileName = configPath + channelName + ".pro";
            if (!File.Exists(fileName)) return "";
            return File.ReadAllText(fileName, Encoding.UTF8);
        }

        public static void dualWieldSort()
        {
            //MelonLogger.Msg("Channels: " + ForceTubeVRInterface.ListChannels());
            JsonDocument doc = JsonDocument.Parse(ForceTubeVRInterface.ListChannels());
            JsonElement pistol1 = doc.RootElement.GetProperty("channels").GetProperty("pistol1");
            JsonElement pistol2 = doc.RootElement.GetProperty("channels").GetProperty("pistol2");
            if ((pistol1.GetArrayLength() > 0) && (pistol2.GetArrayLength() > 0))
            {
                dualWield = true;
                MelonLogger.Msg("Two ProTube devices detected, player is dual wielding.");
                if ((readChannel("rightHand") == "") || (readChannel("leftHand") == ""))
                {
                    MelonLogger.Msg("No configuration files found, saving current right and left hand pistols.");
                    saveChannel("rightHand", pistol1[0].GetProperty("name").ToString());
                    saveChannel("leftHand", pistol2[0].GetProperty("name").ToString());
                }
                else
                {
                    string rightHand = readChannel("rightHand");
                    string leftHand = readChannel("leftHand");
                    MelonLogger.Msg("Found and loaded configuration. Right hand: " + rightHand + ", Left hand: " + leftHand);
                    ForceTubeVRInterface.ClearChannel(4);
                    ForceTubeVRInterface.ClearChannel(5);
                    ForceTubeVRInterface.AddToChannel(4, rightHand);
                    ForceTubeVRInterface.AddToChannel(5, leftHand);
                }
            }
            else if (File.Exists(configPath + "lefty.pro"))
            {
                MelonLogger.Msg("File for only left channel detected. Player is a lefty.");
                string leftHand = pistol1[0].GetProperty("name").ToString();
                MelonLogger.Msg("Found one ProTube device. Left hand: " + leftHand);
                ForceTubeVRInterface.ClearChannel(4);
                ForceTubeVRInterface.ClearChannel(5);
                // ForceTubeVRInterface.AddToChannel(4, rightHand);
                ForceTubeVRInterface.AddToChannel(5, leftHand);
            }
        }

        private async void InitializeProTube()
        {
            MelonLogger.Msg("Initializing ProTube gear...");
            await ForceTubeVRInterface.InitAsync(true);
            Thread.Sleep(10000);
            dualWieldSort();
        }

        [HarmonyPatch(typeof(Gun), "Fire", new Type[] { typeof(Target), typeof(Vector3), typeof(int), typeof(bool), typeof(bool), typeof(ShootableToy) })]
        public class bhaptics_GunFire
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance)
            {
                bool isRight = (__instance.hand == Target.TargetHandType.Right);
                ForceTubeVRChannel myChannel = ForceTubeVRChannel.pistol1;
                if (!isRight) myChannel = ForceTubeVRChannel.pistol2;
                ForceTubeVRInterface.Kick(150, myChannel);
            }
        }

        [HarmonyPatch(typeof(MeleeWeapon), "OnMeleeAttackSuccess", new Type[] { typeof(Target), typeof(Vector3) })]
        public class bhaptics_MeleeSuccess
        {
            [HarmonyPostfix]
            public static void Postfix(MeleeWeapon __instance)
            {
                ForceTubeVRInterface.Rumble(200, 100f, ForceTubeVRChannel.all);
            }
        }

        [HarmonyPatch(typeof(Gun), "OnMeleeAttackSuccess", new Type[] { typeof(Target), typeof(Vector3) })]
        public class bhaptics_GunMelee
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance)
            {
                bool isRight = (__instance.hand == Target.TargetHandType.Right);
                ForceTubeVRChannel myChannel = ForceTubeVRChannel.pistol1;
                if (!isRight) myChannel = ForceTubeVRChannel.pistol2;
                ForceTubeVRInterface.Rumble(200, 100f, myChannel);
            }
        }

        [HarmonyPatch(typeof(Gun), "UpdateSustainFX", new Type[] { })]
        public class bhaptics_GunSustainFX
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance)
            {
                bool isRight = (__instance.hand == Target.TargetHandType.Right);
                ForceTubeVRChannel myChannel = ForceTubeVRChannel.pistol1;
                if (!isRight) myChannel = ForceTubeVRChannel.pistol2;
                if (!__instance.mSustainFlarePlaying) { return; }
                ForceTubeVRInterface.Rumble(200, 300f, myChannel);
            }
        }

    }
}
