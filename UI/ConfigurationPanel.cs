using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using UnityEngine;

namespace TwitchChirperChat.UI
{
    public class ConfigurationPanel : UIPanel
    {
        private ChirpPanel _chirpPanel;
        private volatile bool _ircSettingsChanged = false;

        public override void Start()
        {
            _chirpPanel = GameObject.Find("ChirperPanel").GetComponent<ChirpPanel>();

            if (_chirpPanel == null) return;

            // Set visuals for panel
            this.backgroundSprite = "ChirperBubble";
            this.color = new Color32(122, 132, 138, 255);
            this.width = 550;
            this.height = 500;
            this.transformPosition = new Vector3(-1.6f, 0.9f);

            // Allow automated layout
            this.autoLayoutDirection = LayoutDirection.Vertical;
            this.autoLayoutStart = LayoutStart.TopLeft;
            this.autoLayoutPadding = new RectOffset(10, 10, 0, 10);
            this.autoLayout = true;

            // Add drag handle for panel
            UIDragHandle dragHandle = this.AddUIComponent<UIDragHandle>();
            // Add title to drag handle
            UILabel titleLabel = dragHandle.AddUIComponent<UILabel>();
            titleLabel.text = "Options";
            titleLabel.textScale = 1.5f;
            titleLabel.textColor = new Color32(36, 202, 255, 255);
            titleLabel.textAlignment = UIHorizontalAlignment.Center;

            // Add all the buttons and textboxes for the options panel

            //UIButton muteButton = AddNewButton("Mute");
            //UIButton filterButton = AddNewButton("Filters: OFF");
            //UIButton hashTagsButton = AddNewButton("HashTags: ON");

            AddNewLabel("Twitch User Name: ");
            UITextField twitchUserNameTextField =
                AddNewTextField(Configuration.ConfigurationSettings.UserName);
            twitchUserNameTextField.eventTextChanged += twitchUserNameTextField_eventTextChanged;

            AddNewLabel("Twitch OAuth Key. Should look like 'oauth:000aaa000bbb00' \nGet you oauth key from http://twitchapps.com/tmi/: ");
            UITextField oauthKeyTextField =
                AddNewTextField(Configuration.ConfigurationSettings.OAuthKey, false, true);
            oauthKeyTextField.eventTextChanged += oauthKeyTextField_eventTextChanged;

            AddNewLabel("Chat Channel. If you want to watch http://www.twitch.tv/cleavetv, \nyou would input 'cleavetv' here: ");
            UITextField channelTextField =
                AddNewTextField(Configuration.ConfigurationSettings.IrcChannel);
            channelTextField.eventTextChanged += channelTextFieldTextField_eventTextChanged;

            AddNewLabel("Delay Between Chirper Messages In Milliseconds:");
            UITextField delayBetweenChirperMessagesTextField =
                AddNewTextField(Configuration.ConfigurationSettings.DelayBetweenChirperMessages.ToString(), true);
            delayBetweenChirperMessagesTextField.eventTextChanged += delayBetweenChirperMessagesTextField_eventTextChanged;

            UIButton saveButton = AddNewButton("Save");
            saveButton.eventClick += saveButton_eventClick;

            //delayBetweenChirperMessagesText.isPasswordField = true;
            /*/// <summary>
        /// Default 9
        /// </summary>
        public int DelayBetweenChirperMessages { get; set; }

        /// <summary>
        /// Default true. If someone does @YourName, they will get chat priority
        /// </summary>
        public bool PrioritizePersonallyAddressedMessages { get; set; }

        public string NewSubscriberMessage { get; set; }
        public string RepeatSubscriberMessage { get; set; }
        public string SeniorSubscriberMessage { get; set; }
        public bool ShowSubscriberMessages { get; set; }

        public int MaximumGeneralChatMessageQueue { get; set; }
        public int MaximumSubscriberChatMessageQueue { get; set; }
        public int MaximumModeratorChatMessageQueue { get; set; }

        public bool RenameCitizensToLoggedInUsers { get; set; }
        public bool RenameCitizensToFollowers { get; set; }

        public bool ShowGeneralChatMessages { get; set; }
        public bool ShowSubscriberChatMessages { get; set; }
        public bool ShowModeratorChatMessages { get; set; }

        public string NewFollowersMessage { get; set; }
        public bool ShowNewFollowersMessage { get; set; }

        public bool ShowDefaultChirperMessages { get; set; }
        public int MaximumMessageSize { get; set; }*/
            

            // Defaults to ON if ChirpFilter is active.
            /*
            if (SuperChirper.HasFilters)
                filterButton.text = "Filters: ON";
             */

            //muteButton.eventClick += MuteButtonClick;
            //filterButton.eventClick += FilterButtonClick;
            //hashTagsButton.eventClick += HashTagsButtonClick;

            //SuperChirperMod.MuteButtonInstance = muteButton;
            //SuperChirperMod.FilterButtonInstance = filterButton;
            //SuperChirperMod.HashTagsButtonInstance = hashTagsButton;

            // Default to hidden
            this.Hide();

            //DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "[SuperChirper] ConfigPanel built.");

            
        }

        private void oauthKeyTextField_eventTextChanged(UIComponent component, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (Configuration.ConfigurationSettings.OAuthKey != value)
                    _ircSettingsChanged = true;

                // Someone's going to do it, you know it
                if (!value.Contains("oauth:"))
                    Configuration.ConfigurationSettings.OAuthKey = "oauth:" + value;
                else
                    Configuration.ConfigurationSettings.OAuthKey = value;
            }
        }

        private void channelTextFieldTextField_eventTextChanged(UIComponent component, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (Configuration.ConfigurationSettings.IrcChannel != value)
                    _ircSettingsChanged = true;

                Configuration.ConfigurationSettings.IrcChannel = value;
                
            }
        }

        void twitchUserNameTextField_eventTextChanged(UIComponent component, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (Configuration.ConfigurationSettings.UserName != value)
                    _ircSettingsChanged = true;

                Configuration.ConfigurationSettings.UserName = value;
            }
        }

        void saveButton_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            Configuration.SaveConfigFile();

            try
            {
                //if (_ircSettingsChanged)
                //{
                    //_ircSettingsChanged = false;
                    ChirperExtension.IrcClient.Reconnect(Configuration.ConfigurationSettings.UserName, Configuration.ConfigurationSettings.OAuthKey, Configuration.ConfigurationSettings.IrcChannel);
                //}
            }
            catch (Exception ex)
            {
                Log.AddEntry(ex);
            }

            this.Hide();
        }

        void delayBetweenChirperMessagesTextField_eventTextChanged(UIComponent component, string value)
        {
            int parsed;
            if (int.TryParse(value, out parsed))
                Configuration.ConfigurationSettings.DelayBetweenChirperMessages = parsed;
        }

        private UILabel AddNewLabel(string text)
        {
            var newLabel = this.AddUIComponent<UILabel>();

            newLabel.text = text;
            newLabel.width = this.width - this.autoLayoutPadding.left * 2;
            newLabel.height = 25;

            return newLabel;
        }

        private UITextField AddNewTextField(string defaultText, bool isNumericalOnly = false, bool isPasswordField = false)
        {
            var newTextField = this.AddUIComponent<UITextField>();

            newTextField.text = defaultText;
            newTextField.width = this.width - this.autoLayoutPadding.left * 2;
            newTextField.height = 25;
            newTextField.enabled = true;
            newTextField.builtinKeyNavigation = true;
            newTextField.isInteractive = true;
            newTextField.readOnly = false;
            newTextField.submitOnFocusLost = true;
            newTextField.horizontalAlignment = UIHorizontalAlignment.Left;
            newTextField.selectionSprite = "EmptySprite";
            newTextField.selectionBackgroundColor = new Color32(0, 171, 234, 255);
            newTextField.normalBgSprite = "TextFieldPanel";
            newTextField.textColor = new Color32(174, 197, 211, 255);
            newTextField.disabledTextColor = new Color32(254, 254, 254, 255);
            newTextField.textScale = 1.3f;
            newTextField.opacity = 1;
            newTextField.color = new Color32(58, 88, 104, 255);
            newTextField.disabledColor = new Color32(254, 254, 254, 255);
            newTextField.numericalOnly = isNumericalOnly;
            newTextField.isPasswordField = isPasswordField;

            return newTextField;
        }

        #region "Button installation"
        private UIButton AddNewButton(string buttonText)
        {
            UIButton newButton = this.AddUIComponent<UIButton>();
            SetDefaultButton(newButton, buttonText);

            return newButton;
        }

        private void SetDefaultButton(UIButton button, string buttonText)
        {
            button.text = buttonText;
            button.width = this.width - this.autoLayoutPadding.left * 2;
            button.height = 25;

            button.normalBgSprite = "ButtonMenu";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.textColor = new Color32(255, 255, 255, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            button.focusedTextColor = new Color32(255, 255, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);
        }
        #endregion

        #region "Button Clicks"
        private void LabelClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (eventParam.buttons == UIMouseButton.Left && ChirpPanel.instance != null)
            {
                this.Hide();
            }
        }

        private void MuteButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (eventParam.buttons == UIMouseButton.Left && ChirpPanel.instance != null)
            {
                /*
                if (SuperChirper.IsMuted)
                {
                    // Unmute the chirper, let it make noise.
                    SuperChirper.IsMuted = false;
                    _chirpPane.m_NotificationSound = SuperChirperLoader.MessageSound;

                    // Inform user that chirpy has been unmuted
                    _chirpPane.AddMessage(new ChirpMessage("SuperChirper", "Chirpy now unmuted.", 12345), true);

                    // Adjust button.                    
                    SuperChirperMod.MuteButtonInstance.text = "Mute";
                }
                else if (!SuperChirper.IsMuted)
                {
                    // Set chirper to muted, update sounds.
                    SuperChirper.IsMuted = true;
                    _chirpPane.m_NotificationSound = null;
                    _chirpPane.ClearMessages();

                    // Adjust button.
                    SuperChirperMod.MuteButtonInstance.text = "Unmute";
                }
                */
            }
        }

        private void FilterButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (eventParam.buttons == UIMouseButton.Left && ChirpPanel.instance != null)
            {
                /*
                if (!SuperChirper.HasFilters)
                {
                    if (SuperChirper.IsFiltered)
                    {
                        SuperChirper.IsFiltered = false;
                        _chirpPane.AddMessage(new ChirpMessage("SuperChirper", "Filters removed.", 12345), true);
                        SuperChirperMod.FilterButtonInstance.text = "Filters: OFF";
                    }
                    else if (!SuperChirper.IsFiltered)
                    {
                        SuperChirper.IsFiltered = true;
                        _chirpPane.AddMessage(new ChirpMessage("SuperChirper", "Filters applied.", 12345), true);
                        SuperChirperMod.FilterButtonInstance.text = "Filters: ON";
                    }
                }
                else
                {
                    _chirpPane.AddMessage(new ChirpMessage("SuperChirper", "ChirpFilters by Zuppi detected. Please disable if you want to toggle filters.", 12345), true);
                }
                */

            }
        }

        private void HashTagsButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (eventParam.buttons == UIMouseButton.Left && ChirpPanel.instance != null)
            {
                /*if (SuperChirper.IsHashTagged)
                {
                    SuperChirper.IsHashTagged = false;
                    _chirpPane.AddMessage(new ChirpMessage("SuperChirper", "HashTags OFF.", 12345), true);
                    SuperChirperMod.HashTagsButtonInstance.text = "HashTags: OFF";
                }
                else if (!SuperChirper.IsHashTagged)
                {
                    SuperChirper.IsHashTagged = true;
                    _chirpPane.AddMessage(new ChirpMessage("SuperChirper", "HashTags ON.", 12345), true);
                    SuperChirperMod.HashTagsButtonInstance.text = "HashTags: ON";
                }*/
            }
        }
        #endregion

    }
}
