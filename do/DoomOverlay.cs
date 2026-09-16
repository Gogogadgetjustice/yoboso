using System;
using ManagedDoom;
using ManagedDoom.Unity;
using UnityEngine;

namespace BigWalkDoom
{
    /// <summary>
    /// Owns one running instance of the Doom engine and drives it every frame.
    /// This is the direct replacement for the OW mod's Doom.cs + DoomShipLogMode.cs,
    /// minus everything that was specific to the ship log menu.
    /// </summary>
    public class DoomOverlay : MonoBehaviour
    {
        public DoomOverlay(IntPtr ptr) : base(ptr) { }

        private UnityDoom doomGame;
        private bool running;

        public void Open()
        {
            if (doomGame != null)
            {
                doomGame.Visible = true;
                doomGame.AllowInput = true;
                doomGame.Volume = 15;
                running = true;
                return;
            }

            try
            {
                doomGame = new UnityDoom(new CommandLineArgs(new string[0]), transform);
                doomGame.Volume = 15;
                doomGame.AllowInput = true;
                doomGame.Visible = true;
                running = true;
            }
            catch (Exception e)
            {
                Plugin.InstanceLog.LogError($"Failed to start Doom: {e}");
            }
        }

        public void Close()
        {
            if (doomGame == null) return;
            doomGame.Visible = false;
            doomGame.AllowInput = false;
            running = false;
        }

        public void Shutdown()
        {
            running = false;
            if (doomGame != null)
            {
                doomGame.Dispose();
                doomGame = null;
            }
        }

        private void Update()
        {
            if (!running || doomGame == null) return;

            // UnityDoom.Run returns false when the internal quit sequence completes
            // (e.g. player backs all the way out of Doom's own menu).
            if (!doomGame.Run(Time.unscaledDeltaTime))
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}
