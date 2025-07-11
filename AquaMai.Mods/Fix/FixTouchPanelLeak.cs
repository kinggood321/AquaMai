using System.IO.Ports;
using AquaMai.Config.Attributes;
using HarmonyLib;
using IO;
using MAI2System;
using MelonLoader;

namespace AquaMai.Mods.Fix;

[ConfigSection(exampleHidden: true, defaultOn: true)]
public class FixTouchPanelLeak
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NewTouchPanel), "Open")]
    public static void PostOpen(SerialPort ____serialPort, NewTouchPanel __instance, uint ____monitorIndex)
    {
        ____serialPort.ErrorReceived += (_, e) =>
        {
            MelonLogger.Error($"[TouchPanel {____monitorIndex}] SerialPort error: {e.EventType}");
            var t = Traverse.Create(__instance);
            t.Field<ConstParameter.ErrorID>("_psError").Value = (____monitorIndex == 0)
                ? ConstParameter.ErrorID.TouchPanel_Left_OpenError
                : ConstParameter.ErrorID.TouchPanel_Right_OpenError;
            t.Field<bool>("_isRunning").Value = false;
            __instance.Status = NewTouchPanel.StatusEnum.GotoError;
            // t.Method("Close").GetValue();
            // __instance.Status = NewTouchPanel.StatusEnum.Error;
        };
    }
}