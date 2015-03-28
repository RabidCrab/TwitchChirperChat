using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using TwitchChirperChat.UI;
using UnityEngine;

namespace TwitchChirperChat.UI
{
    // ReSharper disable once InconsistentNaming
    public class UIManager : LoadingExtensionBase
    {
        private static ChirpPanel chirpPane;

        private GameObject optionsPanelObject;

        private static AudioClip messageSound = null;

        public static AudioClip MessageSound
        {
            get
            {
                return messageSound;
            }
            set
            {
                messageSound = value;
            }
        }

        private void DestroyPanel()
        {
            if (optionsPanelObject != null)
            {
                GameObject.Destroy(optionsPanelObject);
            }
        }

        public static ConfigurationPanel OptionsPanelInstance { get; set; }
        public static UIButton OptionsButtonInstance { get; set; }
        public static UIButton ClearButtonInstance { get; set; }

        /// <summary>
        /// Initialize the options menu and Option + Clear buttons
        /// </summary>
        /// <param name="mode"></param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            chirpPane = GameObject.Find("ChirperPanel").GetComponent<ChirpPanel>();

            // For development
            //DestroyPanel();

            if (chirpPane == null) return;

            messageSound = chirpPane.m_NotificationSound;

            GameObject clearButtonObject = new GameObject("SuperChirperClearButton", typeof(UIButton));
            GameObject optionsButtonObject = new GameObject("SuperChirperOptionsButton", typeof(UIButton));
            optionsPanelObject = new GameObject("SuperChirperOptionsPanel", typeof(ConfigurationPanel));

            // Make the Objects a child of the uiView.
            clearButtonObject.transform.parent = chirpPane.transform;
            optionsButtonObject.transform.parent = chirpPane.transform;

            // Get the button component.
            UIButton clearButton = clearButtonObject.GetComponent<UIButton>();
            UIButton optionsButton = optionsButtonObject.GetComponent<UIButton>();
            ConfigurationPanel optionsPanel = optionsPanelObject.GetComponent<ConfigurationPanel>();

            if (optionsPanel == null)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "[TwitchChirperChat] No ConfigPanel component found.");
            }

            UIView.GetAView().AttachUIComponent(optionsPanelObject);

            // Set the text to show on the button.
            clearButton.text = "Clear";
            optionsButton.text = "Options";

            // Set the button dimensions. 
            clearButton.width = 50;
            clearButton.height = 20;
            optionsButton.width = 60;
            optionsButton.height = 20;

            // Style the buttons to make them look like a menu button.
            clearButton.normalBgSprite = "ButtonMenu";
            clearButton.disabledBgSprite = "ButtonMenuDisabled";
            clearButton.hoveredBgSprite = "ButtonMenuHovered";
            clearButton.focusedBgSprite = "ButtonMenuFocused";
            clearButton.pressedBgSprite = "ButtonMenuPressed";
            clearButton.textColor = new Color32(255, 255, 255, 255);
            clearButton.disabledTextColor = new Color32(7, 7, 7, 255);
            clearButton.hoveredTextColor = new Color32(7, 132, 255, 255);
            clearButton.focusedTextColor = new Color32(255, 255, 255, 255);
            clearButton.pressedTextColor = new Color32(30, 30, 44, 255);

            optionsButton.normalBgSprite = "ButtonMenu";
            optionsButton.disabledBgSprite = "ButtonMenuDisabled";
            optionsButton.hoveredBgSprite = "ButtonMenuHovered";
            optionsButton.focusedBgSprite = "ButtonMenuFocused";
            optionsButton.pressedBgSprite = "ButtonMenuPressed";
            optionsButton.textColor = new Color32(255, 255, 255, 255);
            optionsButton.disabledTextColor = new Color32(7, 7, 7, 255);
            optionsButton.hoveredTextColor = new Color32(7, 132, 255, 255);
            optionsButton.focusedTextColor = new Color32(255, 255, 255, 255);
            optionsButton.pressedTextColor = new Color32(30, 30, 44, 255);

            // Enable sounds.
            clearButton.playAudioEvents = true;
            optionsButton.playAudioEvents = true;

            // Place the button.
            clearButton.transformPosition = new Vector3(-1.22f, 1.0f);
            optionsButton.transformPosition = new Vector3(-1.37f, 1.0f);

            // Respond to button click.
            clearButton.eventClick += ClearButtonClick;
            optionsButton.eventClick += OptionsButtonClick;

            ClearButtonInstance = clearButton;
            OptionsButtonInstance = optionsButton;
            OptionsPanelInstance = optionsPanel;
        }

        private void ClearButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (eventParam.buttons == UIMouseButton.Left && ChirpPanel.instance != null)
            {
                // Clear all messages in Chirpy
                chirpPane.ClearMessages();
            }
        }

        private void OptionsButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (eventParam.buttons == UIMouseButton.Left && ChirpPanel.instance != null)
            {
                if (OptionsPanelInstance.isVisible)
                {
                    OptionsPanelInstance.Hide();
                }
                else
                {
                    OptionsPanelInstance.Show();
                }

            }
        }
    }
}
