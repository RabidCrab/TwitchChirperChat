using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace TwitchChirperChat
{
    public class ModInfo : IUserMod
    {
        public string Name
        {
            get { return "Twitch Chirper Chat"; }
        }
        public string Description
        {
            get { return "Turn the Chirper into a Twitch chat feed"; }
        }
    }

    /* Debug code for making a custom panel for chirper
    public class FrameworkThreading : ThreadingExtensionBase
    {
        UIPanel _panel;

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            //before every recompile, hit ctrl+shift+d to remove the old panel
            //then recompile / copy the dll, switch to game, hit ctrl+d to spawn the panel
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D))
            {
                var citizenId = ChirperExtension.LookupCitizenId("rabidcrab");

                if (citizenId != 0u)
                {
                    // We don't need to stinkin' countdown timer. I want to use my own, but I'm going to leave the delta timer alone
                    // just in case someone wants to post their own messages and expect a reasonable timer countdown
                    //ChirperExtension.SetPrivateVariable<bool>(MessageManager.instance, "m_canShowMessage", true);
                    
                    //ChirperExtension.SetPrivateVariable<object>(MessageManager.instance, "m_messageTimeout", 2f);
                    
                    //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Attempting to send message");
                    var message = new Message("rabidcrab", "Test message", "", citizenId);

                    //MessageManager.instance.TryCreateMessage(message, 0u);
                    MessageManager.instance.QueueMessage(message);
                    ChirperExtension.SetPrivateVariable<float>(MessageManager.instance, "m_messageTimeout", 2f);
                    //var item = ChirperExtension.GetPrivateVariable<object>(MessageManager.instance, "m_properties");
                    //ChirperExtension.SetPrivateVariable<float>(item, "m_messageNextTimeout", 1f);
                    //((IChirperExtension)chirpyBannerComponent).OnNewMessage(new CitizenMessage(citizenId, "Hook successful!", "rabidcrab"));
                }
                else
                {
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Citizen ID pull failed");
                }
                /*
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    DestroyPanel();
                }
                else
                {
                    InitPanel();
                }
            }
        }

        void DestroyPanel()
        {
            if (_panel != null)
            {
                GameObject.Destroy(_panel);
            }
        }

        void InitPanel()
        {
            DestroyPanel();

            //the game caches (UI?) classes, so while developing init your UI here
            //alternatively use your own class, but rename it before each recompile

            //UIPanel or any UIComponent you want
            _panel = UIView.GetAView().AddUIComponent(typeof(UIPanel)) as UIPanel;
            _panel.backgroundSprite = "GenericPanel";
            _panel.color = new Color32(255, 0, 0, 100);
            _panel.width = 100;
            _panel.height = 200;

            _panel.Show();
        }
        

    }*/
}
