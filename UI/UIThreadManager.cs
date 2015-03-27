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
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            try
            {
                if (ChirpPanel.instance != null && UIManager.ClearButtonInstance != null)
                {
                    if (UIManager.ClearButtonInstance.isVisible && !ChirpPanel.instance.isShowing)
                    {
                        UIManager.ClearButtonInstance.Hide();
                        UIManager.OptionsButtonInstance.Hide();
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
