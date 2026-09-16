using System;
using System.Text;
using UnityEngine;

namespace BigWalkDoom
{
    /// <summary>
    /// Sits near the couch/credits screen. Listens for "iddqd", proximity, or 
    /// shortcut keys (F1 to open Doom, '+' to teleport to screen).
    /// </summary>
    public class DoomCheatTrigger : MonoBehaviour
    {
        // Required boilerplate for any custom type registered with IL2CPP.
        public DoomCheatTrigger(IntPtr ptr) : base(ptr) { }

        private const string CheatCode = "iddqd";
        private const float ProximityRadius = 6.0f;
        private static readonly Vector3 TeleportTarget = new Vector3(-912.40f, 7.27f, 770.21f);

        private StringBuilder buffer = new StringBuilder();
        private DoomOverlay overlay;
        private Transform playerTransform;

        // Exact hierarchy path from scene dump
        private const string PreferredAnchorPath = "LandmarksNonChallenge/CreditsTheatre/Positioner/CreditsScreen";

        // Four corners fallback
        private static readonly Vector3 ScreenCornerTopLeft = new Vector3(-914.79f, 11.21f, 766.00f);
        private static readonly Vector3 ScreenCornerTopRight = new Vector3(-917.59f, 11.26f, 770.22f);
        private static readonly Vector3 ScreenCornerBottomRight = new Vector3(-918.12f, 8.02f, 769.99f);
        private static readonly Vector3 ScreenCornerBottomLeft = new Vector3(-915.12f, 7.77f, 765.42f);

        public static void SpawnAtScreen()
        {
            var anchor = GameObject.Find(PreferredAnchorPath) ?? GameObject.Find("CreditsScreen");
            GameObject go = new GameObject("DoomCheatTrigger");

            if (anchor != null)
            {
                go.transform.SetParent(anchor.transform, false);
                Plugin.InstanceLog.LogInfo($"DoomCheatTrigger: attached directly to '{anchor.name}'.");
            }
            else
            {
                var center = (ScreenCornerTopLeft + ScreenCornerTopRight + ScreenCornerBottomRight + ScreenCornerBottomLeft) / 4f;
                var right = (ScreenCornerTopRight - ScreenCornerTopLeft).normalized;
                var down = (ScreenCornerBottomLeft - ScreenCornerTopLeft).normalized;
                var normal = Vector3.Cross(down, right).normalized;

                go.transform.position = center;
                go.transform.rotation = Quaternion.LookRotation(normal, -down);

                Plugin.InstanceLog.LogWarning("DoomCheatTrigger: Anchor not found, using hardcoded position.");
            }

            go.AddComponent<DoomCheatTrigger>();
        }

        private void Update()
        {
            // Locate local player dynamically if transform isn't cached
            if (playerTransform == null)
            {
                LocatePlayer();
            }

            // Global Hotkey: Press F1 to instantly launch or toggle Doom
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ToggleDoom();
            }

            // Global Hotkey: Teleport to screen position when pressing '+'
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals))
            {
                TeleportPlayer();
            }

            if (playerTransform == null) return;

            bool inRange = Vector3.Distance(transform.position, playerTransform.position) <= ProximityRadius;

            if (!inRange)
            {
                buffer.Clear();
                return;
            }

            CaptureTypedCharacters();

            if (buffer.ToString().EndsWith(CheatCode, StringComparison.OrdinalIgnoreCase))
            {
                buffer.Clear();
                ToggleDoom();
            }
        }

        private void LocatePlayer()
        {
            var playerObj = GameObject.FindWithTag("Player");

            if (playerObj == null && Camera.main != null)
            {
                playerObj = Camera.main.transform.root.gameObject;
            }

            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        private void TeleportPlayer()
        {
            if (playerTransform == null)
            {
                LocatePlayer();
                if (playerTransform == null)
                {
                    Plugin.InstanceLog.LogWarning("Teleport failed: Player transform not found.");
                    return;
                }
            }

            var cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = TeleportTarget;

            if (cc != null) cc.enabled = true;

            var rb = playerTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Plugin.InstanceLog.LogInfo($"Teleported player to {TeleportTarget}");
        }

        private void CaptureTypedCharacters()
        {
            string typed = Input.inputString;
            if (string.IsNullOrEmpty(typed)) return;

            foreach (char c in typed)
            {
                if (char.IsLetterOrDigit(c)) buffer.Append(char.ToLowerInvariant(c));
                if (buffer.Length > CheatCode.Length) buffer.Remove(0, buffer.Length - CheatCode.Length);
            }
        }

        private void ToggleDoom()
        {
            if (overlay == null)
            {
                var go = new GameObject("DoomOverlayWindow");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.forward * -0.05f;
                overlay = go.AddComponent<DoomOverlay>();
                overlay.Open();
                Plugin.InstanceLog.LogInfo("Doom toggled on via F1 / iddqd.");
            }
            else
            {
                overlay.Close();
            }
        }

        private void OnDestroy()
        {
            if (overlay != null) overlay.Shutdown();
        }
    }
}