using System;
using System.Collections.Generic;
using AquaMai.Config.Attributes;
using HarmonyLib;
using Manager;
using MelonLoader;
using Monitor;
using Process;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AquaMai.Mods.Utils;

[ConfigSection(
    zh: "在游戏过程中在副屏显示触摸输入，可用于调试吃和蹭的问题")]
public static class DisplayTouchInGame
{
    private static GameObject prefab;

    [ConfigEntry(
        zh: "将上屏改成白底",
        en: "Display white background on top screen")]
    public static bool whiteBackground = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.RegisterMouseTouchPanel))]
    public static void RegisterMouseTouchPanel(int target, MouseTouchPanel targetMouseTouchPanel)
    {
        if (target != 0)
            return;
        prefab = targetMouseTouchPanel.gameObject;
        MelonLogger.Msg("[DisplayTouchInGame] RegisterMouseTouchPanel");
        MelonLogger.Msg(prefab);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameProcess), nameof(GameProcess.OnStart))]
    public static void OnGameStart(GameProcess __instance, GameMonitor[] ____monitors)
    {
        MelonLogger.Msg("[DisplayTouchInGame] OnGameStart");
        if (prefab == null)
        {
            MelonLogger.Error("[DisplayTouchInGame] prefab is null");
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            var sub = ____monitors[i].gameObject.transform.Find("Canvas/Sub");
            if (sub == null)
            {
                MelonLogger.Error($"[DisplayTouchInGame] sub is null for monitor {i}");
                continue;
            }
            var canvas = new GameObject("[AquaMai] DisplayTouchInGame");
            canvas.transform.SetParent(sub, false);

            if (whiteBackground)
            {
                var rect = canvas.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1080, 450);
                rect.localPosition = new Vector3(0f, 0f, 0.1f);
                var img = canvas.AddComponent<Image>();
                img.color = Color.white;
            }
            
            var touchPanel = Object.Instantiate(prefab, canvas.transform, false);
            Object.Destroy(touchPanel.GetComponent<MouseTouchPanel>());
            foreach (Transform item in touchPanel.transform)
            {
                Object.Destroy(item.GetComponent<MeshButton>());
                Object.Destroy(item.GetComponent<Collider>());
                var customGraphic = item.GetComponent<CustomGraphic>();
                customGraphic.color = Color.blue;
                var tmp = item.GetComponentInChildren<TextMeshProUGUI>();
                tmp.color = Color.black;
            }
            touchPanel.transform.localScale = Vector3.one * 450 / 1080f;
            touchPanel.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            var display = touchPanel.AddComponent<Display>();
            display.player = i;
        }
    }

    private class Display : MonoBehaviour
    {
        public int player;

        private List<CustomGraphic> _buttonList;

        private Color _offTouchCol;

        private Color _onTouchCol;

        private void Start()
        {
            _offTouchCol = new Color(0f, 0f, 1f, whiteBackground ? 1f : 0.6f);
            _onTouchCol = new Color(1f, 0f, 0f, whiteBackground ? 1f : 0.6f);
            _buttonList = new List<CustomGraphic>();
            foreach (Transform item in transform)
            {
                CustomGraphic component = item.GetComponent<CustomGraphic>();
                _buttonList.Add(component);
            }
        }

        private void OnGUI()
        {
            foreach (CustomGraphic graphic in _buttonList)
            {
                if (!Enum.TryParse(graphic.name, out InputManager.TouchPanelArea button)) return;
                graphic.color = (InputManager.GetTouchPanelAreaPush(player, button) ? _onTouchCol : _offTouchCol);
            }
        }
    }
}