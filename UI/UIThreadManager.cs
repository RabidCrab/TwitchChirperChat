using System;
using ColossalFramework.Plugins;
using ICities;
using UnityEngine;

namespace TwitchChirperChat.UI
{
    public class UIThreadManager : ThreadingExtensionBase
    {
        private bool _isShowing = false;

        /// <summary>
        /// Options display handling. Depending on the state of the Chirper window is what is/isn't visible
        /// </summary>
        /// <param name="realTimeDelta">I'm guessing it's the delta of real-life time, generally just called the delta time</param>
        /// <param name="simulationTimeDelta">I'm guessing this is why realTimeDelta has realTime added to it. Must be the delta of the in-game simulation time so that it's possible to fast-forward the game without fast-forwarding the UI stuff</param>
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.C))
            {
                if (UIManager.OptionsPanelInstance != null)
                {
                    if (_isShowing)
                        UIManager.OptionsPanelInstance.Hide();
                    else
                        UIManager.OptionsPanelInstance.Show();

                    _isShowing = !_isShowing;
                }
                else
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "The panel is null");
                }
            }

            try
            {
                if (ChirpPanel.instance != null && UIManager.ClearButtonInstance != null)
                {
                    if (UIManager.ClearButtonInstance.isVisible && !ChirpPanel.instance.isShowing)
                    {
                        UIManager.ClearButtonInstance.Hide();
                        if (UIManager.OptionsButtonInstance != null)
                            UIManager.OptionsButtonInstance.Hide();
                        if (UIManager.OptionsPanelInstance != null)
                            UIManager.OptionsPanelInstance.Hide();
                    }
                    else if (!UIManager.ClearButtonInstance.isVisible && ChirpPanel.instance.isShowing)
                    {
                        UIManager.ClearButtonInstance.Show();
                        UIManager.OptionsButtonInstance.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                ChirperExtension.Logger.AddEntry(ex);
            }

            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }
    }
}
