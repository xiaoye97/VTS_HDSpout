using BepInEx;
using HarmonyLib;
using Klak.Spout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VTS_HDSpout
{
    [BepInPlugin(GUID, PluginName, VERSION)]
    public class HDSpout : BaseUnityPlugin
    {
        public const string GUID = "me.xiaoye97.plugin.VTubeStudio.VTS_HDSpout";
        public const string PluginName = "VTS_HDSpout";
        public const string VERSION = "1.0.0";
        private CircleButtonController CircleButtonController;
        private RectTransform CircleButtonControllerRT;
        private static Camera live2dCamera;

        private static int sizeSettingIndex = 0;

        private static string[] sizeStrings = new string[]
        {
            "Default",
            "1080p",
            "2K",
            "4K",
            "8K",
            "16K"
        };

        private static Rect[] sizes = new Rect[]
        {
            Rect.zero,
            new Rect(0, 0, 1920, 1080),
            new Rect(0, 0, 2560, 1440),
            new Rect(0, 0, 3840, 2160),
            new Rect(0, 0, 7680, 4320),
            new Rect(0, 0, 15360, 8640)
        };

        private static RenderTexture cacheRT, emptyRT;

        private void Start()
        {
            Harmony.CreateAndPatchAll(typeof(HDSpout));
        }

        private void Update()
        {
            if (CircleButtonControllerRT == null)
            {
                CircleButtonController = GameObject.FindObjectOfType<CircleButtonController>();
                if (CircleButtonController != null)
                {
                    CircleButtonControllerRT = CircleButtonController.transform as RectTransform;
                }
            }
        }

        private void OnGUI()
        {
            if (CircleButtonControllerRT != null && CircleButtonControllerRT.anchoredPosition.x >= 0)
            {
                GUILayout.Space(30);
                GUILayout.BeginHorizontal();
                GUILayout.Space(250);
                GUILayout.BeginHorizontal("VTS_HDSpout", GUI.skin.window);
                sizeSettingIndex = GUILayout.SelectionGrid(sizeSettingIndex, sizeStrings, sizeStrings.Length);

                GUILayout.EndHorizontal();
                GUILayout.EndHorizontal();
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SpoutSender), "Update")]
        public static bool SpoutSender_Update_Prefix(SpoutSender __instance)
        {
            if (sizeSettingIndex == 0) return true;
            int hdwidth = (int)sizes[sizeSettingIndex].width;
            int hdheight = (int)sizes[sizeSettingIndex].height;
            if (cacheRT == null || cacheRT.width != hdwidth || cacheRT.height != hdheight)
            {
                Destroy(cacheRT);
                Destroy(emptyRT);
                cacheRT = new RenderTexture(hdwidth, hdheight, 0);
                emptyRT = new RenderTexture(hdwidth, hdheight, 0);
            }
            if (live2dCamera == null)
            {
                live2dCamera = GameObject.Find("Cameras/Live2D Camera").GetComponent<Camera>();
            }
            var _this = __instance;
            // 清空内容
            Blitter.Blit(_this._resources, emptyRT, cacheRT, _this._keepAlpha);
            live2dCamera.targetTexture = cacheRT;
            live2dCamera.Render();
            live2dCamera.targetTexture = null;
            _this.PrepareBuffer(cacheRT.width, cacheRT.height);
            Blitter.Blit(_this._resources, cacheRT, _this._buffer, _this._keepAlpha);
            if (_this._sender == null)
            {
                _this._sender = new Sender(_this._spoutName, _this._buffer);
            }
            _this._sender.Update();
            return false;
        }
    }
}