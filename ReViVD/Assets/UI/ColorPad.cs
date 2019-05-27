﻿using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class ColorPad : MonoBehaviour {
        float prevFingerX = 0;
        bool swiping = false;

        public float dampener = 50;
        public float centeringSpeed = 0.2f;

        int color;

        void Update() {
            RectTransform rt = GetComponent<RectTransform>();

            if (SteamVR_ControllerManager.RightController.padTouched) {
                if (!swiping)
                    swiping = true;
                else {
                    rt.Translate(Tools.Limit(SteamVR_ControllerManager.RightController.Pad.x - prevFingerX, -2.5f - rt.localPosition.x, 2.5f - rt.localPosition.x) / dampener, 0, 0);
                }
                prevFingerX = SteamVR_ControllerManager.RightController.Pad.x;
            }
            else {
                swiping = false;
                float centering = Tools.MaxAbs(2.5f - color - rt.localPosition.x, centeringSpeed);
                if (Mathf.Abs(centering) < centeringSpeed / 100)
                    centering = 0;
                rt.Translate(centering * Time.deltaTime, 0, 0);
            }

            color = Mathf.FloorToInt(3f - rt.localPosition.x);
            if (color != (int)SelectorManager.Instance.CurrentColor)
                SelectorManager.Instance.CurrentColor = (SelectorManager.ColorGroup)color;
        }
    }

}