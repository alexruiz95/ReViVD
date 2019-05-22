﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class Selector : MonoBehaviour {
        public bool inverse = false;
        public bool erase = false;

        private void SeparateFromManager() {
            if (Persistent) {
                SelectorManager.Instance.persistentSelectors[(int)Color].Remove(this);
            }
            else if (SelectorManager.Instance.handSelectors[(int)Color] == this)
                SelectorManager.Instance.handSelectors[(int)Color] = null;
            foreach (SelectorPart p in GetComponents<SelectorPart>())
                p.Shown = false;
        }

        private void AttachToManager() {
            if (Persistent) {
                SelectorManager.Instance.persistentSelectors[(int)Color].Add(this);
                foreach (SelectorPart p in GetComponents<SelectorPart>())
                    p.Shown = true;
            }
            else {
                SelectorManager.Instance.handSelectors[(int)Color] = this;
                if (SelectorManager.Instance.CurrentColor == Color)
                    foreach (SelectorPart p in GetComponents<SelectorPart>())
                        p.Shown = true;
                else {
                    enabled = false;
                }
            }
        }

        private bool old_persistent;
        [SerializeField]
        private bool _persistent = false;
        public bool Persistent {
            get => _persistent;
            set {
                if (value == _persistent && _persistent == old_persistent) //On rafraîchit la valeur si elle a été changée dans l'éditeur
                    return;

                SeparateFromManager();

                _persistent = value;
                old_persistent = _persistent;

                AttachToManager();
            }
        }

        private SelectorManager.ColorGroup old_color;
        [SerializeField]
        private SelectorManager.ColorGroup _color = 0;
        public SelectorManager.ColorGroup Color {
            get => _color;
            set {
                if (value == _color && _color == old_color)
                    return;

                SeparateFromManager();

                _color = value;
                old_color = _color;

                AttachToManager();
            }
        }

        private void OnEnable() {
            SteamVR_ControllerManager.RightController.Gripped += Select;
            SteamVR_ControllerManager.RightController.MenuButtonClicked += MakePersistentCopy;
            AttachToManager();
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.RightController != null) {
                SteamVR_ControllerManager.RightController.Gripped -= Select;
                SteamVR_ControllerManager.RightController.MenuButtonClicked -= MakePersistentCopy;
            }
            foreach (SelectorPart p in GetComponents<SelectorPart>())
                p.Shown = false;
        }

        private bool ShouldSelect {
            get {
                return SteamVR_ControllerManager.RightController.triggerPressed;
            }
        }

        private List<SelectorPart> parts = new List<SelectorPart>();
        private HashSet<Atom> handledRibbons = new HashSet<Atom>();
        private bool needsCheckedHighlightCleanup = false;

        private void Select(SteamVR_TrackedController sender) {
            if (!Persistent && sender != null) //Handheld selectors are only operated in the update loop (not as events)
                return;

            Visualization viz = Visualization.Instance;
            HashSet<Atom> selectedRibbons = SelectorManager.Instance.selectedRibbons[(int)Color];

            foreach (SelectorPart p in parts) {
                p.districtsToCheck.Clear();
                p.FindDistrictsToCheck();

                foreach (Atom a in p.ribbonsToCheck)
                    a.ShouldHighlightBecauseChecked((int)Color, false);
                p.ribbonsToCheck.Clear();

                foreach (int[] c in p.districtsToCheck) {
                    if (viz.districts.TryGetValue(c, out Visualization.District d)) {
                        foreach (Atom a in d.atoms_segment) {
                            if (a.ShouldDisplay) {
                                p.ribbonsToCheck.Add(a);
                                if (SelectorManager.Instance.highlightChecked && !Persistent) {
                                    a.ShouldHighlightBecauseChecked((int)Color, true);
                                }
                            }
                        }
                    }
                }

                p.touchedRibbons.Clear();
                p.FindTouchedRibbons();
            }

            handledRibbons.Clear();

            foreach (SelectorPart p in parts) {
                if (p.Positive) {
                    foreach (Atom a in p.touchedRibbons)
                        handledRibbons.Add(a);
                }
                else {
                    foreach (Atom a in p.touchedRibbons)
                        handledRibbons.Remove(a);
                }
            }

            if (inverse) { //Very inefficient code for now, may need an in-depth restructuration of the Viz/Path/Atom architecture
                List<Atom> allRibbons = new List<Atom>();
                foreach (Path p in viz.PathsAsBase) {
                    allRibbons.AddRange(p.AtomsAsBase);
                }
                HashSet<Atom> inversed = new HashSet<Atom>(allRibbons);
                inversed.ExceptWith(handledRibbons);

                if (erase)
                    selectedRibbons.ExceptWith(inversed);
                else
                    selectedRibbons.UnionWith(inversed);
            }
            else {
                if (erase)
                    selectedRibbons.ExceptWith(handledRibbons);
                else
                    selectedRibbons.UnionWith(handledRibbons);
            }

            if (SelectorManager.Instance.highlightSelected) {
                foreach (Atom a in selectedRibbons) {
                    a.ShouldHighlightBecauseSelected((int)Color, true);
                }
            }

            if (SelectorManager.Instance.highlightChecked)
                needsCheckedHighlightCleanup = true;
        }

        private void MakePersistentCopy(SteamVR_TrackedController sender) {
            if (Persistent)
                return;
            Persistent = true;
            GameObject go = Instantiate(this.gameObject, SelectorManager.Instance.transform);
            go.name = name;
            name = "Persistent " + name;
            go.GetComponent<Selector>().Persistent = false;
        }

        private void Awake() {
            old_color = Color;
            old_persistent = Persistent;
        }

        private void Update() {
            Color = _color; //Une update se fera si nécessaire (couleur changée dans l'éditeur)
            Persistent = _persistent; //idem

            GetComponents(parts);
            parts.RemoveAll(p => p.isActiveAndEnabled == false);

            foreach (SelectorPart p in parts) {
                p.UpdatePrimitive();
                if (!ShouldSelect && needsCheckedHighlightCleanup) {
                    foreach (Atom a in p.ribbonsToCheck)
                        a.ShouldHighlightBecauseChecked((int)Color, false);
                }
            }

            if (ShouldSelect && !Persistent)
                Select(null);
        }
    }
}