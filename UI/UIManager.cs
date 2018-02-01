using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace TwitchChirperChat.UI
{
    public class UIManager : LoadingExtensionBase
    {
        private static ChirpPanel _chirpPanel;

        private GameObject _optionsPanelObject;

        static UIManager()
        {
            MessageSound = null;
        }

        public static AudioClip MessageSound { get; set; }

        private void DestroyPanel()
        {
            if (_optionsPanelObject != null)
            {
                GameObject.Destroy(_optionsPanelObject);
            }
        }

        public static ConfigurationPanel OptionsPanelInstance { get; private set; }
        public static UIButton OptionsButtonInstance { get; set; }
        public static UIButton ClearButtonInstance { get; set; }

        /// <summary>
        /// Initialize the options menu and Option + Clear buttons
        /// </summary>
        /// <param name="mode"></param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            // For development
            DestroyPanel();

            _optionsPanelObject = new GameObject("TwitchChirperChatOptionsPanel", typeof(ConfigurationPanel));

            try
            {
                _chirpPanel = GameObject.Find("ChirperPanel").GetComponent<ChirpPanel>();
            }
            catch
            {
            }

            if (_chirpPanel != null)
            {
                //messageSound = chirpPane.m_NotificationSound;

                GameObject clearButtonObject = new GameObject("SuperChirperClearButton", typeof (UIButton));
                GameObject optionsButtonObject = new GameObject("SuperChirperOptionsButton", typeof (UIButton));

                clearButtonObject.transform.parent = _chirpPanel.transform;
                optionsButtonObject.transform.parent = _chirpPanel.transform;

                UIButton clearButton = clearButtonObject.GetComponent<UIButton>();
                UIButton optionsButton = optionsButtonObject.GetComponent<UIButton>();

                // Set the text to show on the button.
                clearButton.text = "Clear";
                optionsButton.text = "Options";

                // Set the button dimensions. 
                clearButton.width = 50;
                clearButton.height = 20;
                optionsButton.width = 65;
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
                clearButton.transformPosition = new Vector3(-1.42f, 0.9999f);
                optionsButton.transformPosition = new Vector3(-1.60f, 0.9999f);

                // Respond to button click.
                clearButton.eventClick += ClearButtonClick;
                optionsButton.eventClick += OptionsButtonClick;

                ClearButtonInstance = clearButton;
                OptionsButtonInstance = optionsButton;
            }

            UIView.GetAView().AttachUIComponent(_optionsPanelObject);
            var optionsPanel = _optionsPanelObject.GetComponent<ConfigurationPanel>();

            OptionsPanelInstance = optionsPanel;
        }

        private void ClearButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (eventParam.buttons == UIMouseButton.Left && ChirpPanel.instance != null)
            {
                // Clear all messages in Chirpy
                if (_chirpPanel != null)
                    _chirpPanel.ClearMessages();
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
