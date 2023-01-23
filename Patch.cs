using BaseX;
using CodeX;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HeadlessNext
{
    public class Patch : NeosMod
    {
        public override string Name => "Headless neXt";
        public override string Author => "LeCloutPanda";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/LeCloutPanda/HeadlessNeXt"; // Need to readd
        static Harmony harmony;
        // This was a joint effort between multiple users
        // LeCloutpanda: https://github.com/LeCloutPanda
        // NeroWolf: https://github.com/NeroWolf001
        // Sox: https://github.com/Sox-NeosVR 
        // Nytra: https://github.com/Nytra/NeosHeadlessToolTipKickCrashFix

        public override void OnEngineInit()
        {
            Harmony.DEBUG = true;
            harmony = new Harmony($"dev.LeCloutPanda.HeadlessNeXt");
            harmony.PatchAll();

            Engine.Current.OnReady += Current_OnReady;

            // Fix found by LeCloutPanda
            // Patch by Nytra
            MethodInfo originalMethod = AccessTools.DeclaredMethod(typeof(CommonTool), "TooltipDequipped", new Type[] { typeof(IToolTip), typeof(bool) });
            MethodInfo replacementMethod = AccessTools.DeclaredMethod(typeof(Patch), nameof(ToolTipPermissionFix));
            harmony.Patch(originalMethod, prefix: new HarmonyMethod(replacementMethod));
        }

        public static bool ToolTipPermissionFix(IToolTip tooltip, ref bool popOff)
        {
            popOff = false;
            return true;
        }

        private void Current_OnReady()
        {
            Engine.Current.WorldManager.WorldAdded += WorldAdded;
            Engine.Current.WorldManager.WorldRemoved += WorldRemoved;
        }

        private void WorldAdded(World obj) => obj.ComponentAdded += OnComponentAdded;        
        private void WorldRemoved(World obj) => obj.ComponentAdded -= OnComponentAdded;

        private void OnComponentAdded(Slot arg1, Component arg2)
        {
            // Requested by multiple to make host only
            if (!arg1.LocalUser.IsHost) return;

            // Patch by LeCloutPanda and NeroWolf
            if (arg2.GetType() == typeof(AudioOutput))
            {
                arg1.RunInUpdates(3, () =>
                {
                    VideoPlayer videoPlayer = arg1.GetComponent<VideoPlayer>();
                    if (videoPlayer == null) return;

                    AudioOutput audioOutput = (AudioOutput)arg2;
                    ValueUserOverride<float> userOverride = audioOutput.Volume.OverrideForUser<float>(arg1.World.HostUser, 0);
                    userOverride.CreateOverrideOnWrite.Value = true;
                    userOverride.Default.Value = 0;

                    Slot volume = arg1.FindChild(ch => ch.Name.Equals("Volume"), 1);
                    if (volume.FindChild(ch => ch.Name.Equals("Local Text"), 1) != null) return;

                    TextRenderer text = volume.AddSlot("Local Text").AttachComponent<TextRenderer>();
                    text.Text.Value = "Local";
                    text.Slot.PersistentSelf = false;
                    text.Slot.Scale_Field.Value = new BaseX.float3(0.5f, 0.5f, 0.5f);
                    text.Slot.Position_Field.Value = new BaseX.float3(0f, 0f, -0.02f);
                });
            }

            // Patch by LeCloutPanda and NeroWolf
            if (arg2.GetType() == typeof(UserAudioStream<StereoSample>))
            {
                arg1.RunInUpdates(3, () =>
                {
                    AudioOutput audioOutput = arg1.GetComponent<AudioOutput>();
                    if (audioOutput == null) return;

                    ValueUserOverride<float> userOverride = audioOutput.Volume.OverrideForUser<float>(arg1.World.HostUser, 0);
                    userOverride.CreateOverrideOnWrite.Value = true;
                    userOverride.Default.Value = 0;



                    Slot Handle = arg1.FindChild(ch => ch.Name.Equals("Handle"), 1);
                    if (Handle.FindChild(ch => ch.Name.Equals("Local Text"), 1) != null) return;

                    TextRenderer text = Handle.AddSlot("Local Text").AttachComponent<TextRenderer>();
                    text.Text.Value = "Local Audio";
                    text.Slot.PersistentSelf = false;
                    text.Slot.Scale_Field.Value = new BaseX.float3(0.3f, 0.3f, 0.3f);
                    text.Slot.Position_Field.Value = new BaseX.float3(0f, 0f, -0.0075f);
                });
            }

            // Patch by Sox
            if (arg2.GetType() == typeof(AvatarCreator))
            {
                arg1.RunInUpdates(3, () =>
                {
                AvatarCreator avatarCreator = arg1.GetComponent<AvatarCreator>();
                if (avatarCreator == null) return;

                    List<Slider> sliders = new List<Slider>();
                    sliders = arg1.GetComponentsInChildren<Slider>();
                    if (sliders.Count <= 0) return;

                    foreach (Slider slide in sliders)
                    {
                        slide.DontDrive.Value = true;
                    }
                });
            }   
        }

        [HarmonyPatch(typeof(AvatarAudioOutputManager))]
        static class PatchAvatarAudioOutputManager
        {
            [HarmonyPatch("OnAwake")]
            [HarmonyPostfix]
            static void FixScaleCompensation(AvatarAudioOutputManager __instance, Sync<float> ____scaleCompensation)
            {
                __instance.RunInUpdates(3, () =>
                {
                    ValueUserOverride<float> valueOverride = ____scaleCompensation.OverrideForUser(__instance.LocalUser, 1f);
                    valueOverride.Persistent = false;
                    valueOverride.Default.Value = 1;
                });
            }
        }
    }
}

