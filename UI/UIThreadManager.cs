using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICities;

namespace TwitchChirperChat.UI
{
    // ReSharper disable once InconsistentNaming
    public class UIThreadManager : ThreadingExtensionBase
    {
        /// <summary>
        /// Options display handling. Depending on the state of the Chirper window is what is/isn't visible
        /// </summary>
        /// <param name="realTimeDelta"></param>
        /// <param name="simulationTimeDelta"></param>
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
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
                Log.AddEntry(ex);
            }

            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }
    }
}
