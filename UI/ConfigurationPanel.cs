using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using UnityEngine;

namespace TwitchChirperChat.UI
{
    public class ConfigurationPanel : UIPanel
    {
        private ChirpPanel _chirpPanel;

        private UITextField _twitchUserNameTextField;
        private UITextField _oauthKeyTextField;
        private UITextField _channelTextField;
        private UITextField _delayBetweenChirperMessagesTextField;
        //private UITextField _;

        /// <summary>
        /// Partially copied from https://github.com/AtheMathmo/SuperChirperMod
        /// </summary>
        public override void Start()
        {
            _chirpPanel = GameObject.Find("ChirperPanel").GetComponent<ChirpPanel>();

            if (_chirpPanel == null) return;

            // Set visuals for panel
            this.backgroundSprite = "ChirperBubble";
            this.color = new Color32(122, 132, 138, 255);
            this.width = 550;
            this.height = 650;
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

            // Yay UI stuff!
            AddNewLabel("(Optional) Twitch User Name: ");
            UITextField twitchUserNameTextField =
                AddNewTextField(Configuration.ConfigurationSettings.UserName);
            _twitchUserNameTextField = twitchUserNameTextField;

            AddNewLabel("(Optional) Twitch OAuth Key. Should look like 'oauth:000aaa000bbb00' \nGet you oauth key from http://twitchapps.com/tmi/: ");
            UITextField oauthKeyTextField =
                AddNewTextField(Configuration.ConfigurationSettings.OAuthKey, false, true);
            oauthKeyTextField.maxLength = 200;
            _oauthKeyTextField = oauthKeyTextField;

            AddNewLabel("Chat Channel. If you want to watch http://www.twitch.tv/cleavetv, \nyou would input 'cleavetv' here: ");
            UITextField channelTextField =
                AddNewTextField(Configuration.ConfigurationSettings.IrcChannel);
            _channelTextField = channelTextField;

            AddNewLabel("Delay Between Chirper Messages In Milliseconds:");
            UITextField delayBetweenChirperMessagesTextField =
                AddNewTextField(Configuration.ConfigurationSettings.DelayBetweenChirperMessages.ToString(), true);
            _delayBetweenChirperMessagesTextField = delayBetweenChirperMessagesTextField;

            // Buttons!
            UIButton showGeneralChatMessages = AddNewButton("Show General Chat Messages: " + (Configuration.ConfigurationSettings.ShowGeneralChatMessages ? "ON" : "OFF"));
            showGeneralChatMessages.eventClick += showGeneralChatMessages_eventClick;
            UIButton showSubscriberChatMessages = AddNewButton("Show Subscriber Chat Messages: " + (Configuration.ConfigurationSettings.ShowSubscriberMessages ? "ON" : "OFF"));
            showSubscriberChatMessages.eventClick += showSubscriberChatMessages_eventClick;
            UIButton showModeratorChatMessages = AddNewButton("Show Moderator Chat Messages: " + (Configuration.ConfigurationSettings.ShowModeratorChatMessages ? "ON" : "OFF"));
            showModeratorChatMessages.eventClick += showModeratorChatMessages_eventClick;
            UIButton showNewFollowersMessage = AddNewButton("Show New Followers Messages: " + (Configuration.ConfigurationSettings.ShowNewFollowersMessage ? "ON" : "OFF"));
            showNewFollowersMessage.eventClick += showNewFollowersMessage_eventClick;
            UIButton showSubscriberMessages = AddNewButton("Show New Subscriber Messages: " + (Configuration.ConfigurationSettings.ShowSubscriberMessages ? "ON" : "OFF"));
            showSubscriberMessages.eventClick += showSubscriberMessages_eventClick;
            UIButton showDefaultChirperMessages = AddNewButton("Show Default Chirper Messages: " + (Configuration.ConfigurationSettings.ShowDefaultChirperMessages ? "ON" : "OFF"));
            showDefaultChirperMessages.eventClick += showDefaultChirperMessages_eventClick;

            UIButton resetLogin = AddNewButton("Reset Login To Default");
            resetLogin.eventClick += resetLoginButton_eventClick;

            UIButton saveButton = AddNewButton("Save");
            saveButton.eventClick += saveButton_eventClick;

            // Default to hidden
            this.Hide();
        }

        #region Generic Button Clicks
        void showDefaultChirperMessages_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is UIButton)
            {
                Configuration.ConfigurationSettings.ShowDefaultChirperMessages = !Configuration.ConfigurationSettings.ShowDefaultChirperMessages;
                (component as UIButton).text = "Show Default Chirper Messages: " + (Configuration.ConfigurationSettings.ShowDefaultChirperMessages ? "ON" : "OFF");
            }
        }

        void showSubscriberMessages_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is UIButton)
            {
                Configuration.ConfigurationSettings.ShowSubscriberMessages = !Configuration.ConfigurationSettings.ShowSubscriberMessages;
                (component as UIButton).text = "Show New Subscriber Messages: " + (Configuration.ConfigurationSettings.ShowSubscriberMessages ? "ON" : "OFF");
            }
        }

        void showNewFollowersMessage_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is UIButton)
            {
                Configuration.ConfigurationSettings.ShowNewFollowersMessage = !Configuration.ConfigurationSettings.ShowNewFollowersMessage;
                (component as UIButton).text = "Show New Followers Messages: " + (Configuration.ConfigurationSettings.ShowNewFollowersMessage ? "ON" : "OFF");
            }
        }

        void showModeratorChatMessages_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is UIButton)
            {
                Configuration.ConfigurationSettings.ShowModeratorChatMessages = !Configuration.ConfigurationSettings.ShowModeratorChatMessages;
                (component as UIButton).text = "Show Moderator Chat Messages: " + (Configuration.ConfigurationSettings.ShowModeratorChatMessages ? "ON" : "OFF");
            }
        }

        void showSubscriberChatMessages_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is UIButton)
            {
                Configuration.ConfigurationSettings.ShowSubscriberChatMessages = !Configuration.ConfigurationSettings.ShowSubscriberChatMessages;
                (component as UIButton).text = "Show Subscriber Chat Messages: " + (Configuration.ConfigurationSettings.ShowSubscriberChatMessages ? "ON" : "OFF");
            }
        }

        void showGeneralChatMessages_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is UIButton)
            {
                Configuration.ConfigurationSettings.ShowGeneralChatMessages = !Configuration.ConfigurationSettings.ShowGeneralChatMessages;
                (component as UIButton).text = "Show General Chat Messages: " + (Configuration.ConfigurationSettings.ShowGeneralChatMessages ? "ON" : "OFF");
            }
        }
        #endregion

        /// <summary>
        /// Reset the login information to chirpertestclient. This is just in case someone edits the oauth key without meaning to
        /// </summary>
        /// <param name="component"></param>
        /// <param name="eventParam"></param>
        private void resetLoginButton_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            Configuration.ResetLoginCredentialsToDefault();

            _twitchUserNameTextField.text = Configuration.ConfigurationSettings.UserName;
            _oauthKeyTextField.text = Configuration.ConfigurationSettings.OAuthKey;

            saveButton_eventClick(null, null);
        }

        /// <summary>
        /// Save the config file changes
        /// </summary>
        /// <param name="component"></param>
        /// <param name="eventParam"></param>
        void saveButton_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            try
            {
                Configuration.ConfigurationSettings.UserName = _twitchUserNameTextField.text;
                Configuration.ConfigurationSettings.OAuthKey = _oauthKeyTextField.text;
                Configuration.ConfigurationSettings.IrcChannel = _channelTextField.text;

                int parsed;
                if (int.TryParse(_delayBetweenChirperMessagesTextField.text, out parsed))
                {
                    if (Configuration.ConfigurationSettings.DelayBetweenChirperMessages != parsed)
                    {
                        Configuration.ConfigurationSettings.DelayBetweenChirperMessages = parsed;

                        ChirperExtension.ChangeTimerDelay(parsed);
                    }
                        
                }
                    

                Configuration.SaveConfigFile();

                if (ChirperExtension.IrcClient != null)
                    ChirperExtension.IrcClient.Reconnect(Configuration.ConfigurationSettings.UserName, Configuration.ConfigurationSettings.OAuthKey, Configuration.ConfigurationSettings.IrcChannel);
            }
            catch (Exception ex)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, ex.Source + " " + ex.TargetSite);
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

        /// <summary>
        /// Copied mostly from http://www.skylinesmodding.com/t/uitextinput/120
        /// </summary>
        /// <param name="defaultText"></param>
        /// <param name="isNumericalOnly"></param>
        /// <param name="isPasswordField"></param>
        /// <returns></returns>
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

        
        #region Button installation 
        private UIButton AddNewButton(string buttonText)
        {
            UIButton newButton = this.AddUIComponent<UIButton>();
            SetDefaultButton(newButton, buttonText);

            return newButton;
        }
        /// <summary>
        /// Copied from https://github.com/AtheMathmo/SuperChirperMod
        /// </summary>
        /// <param name="button"></param>
        /// <param name="buttonText"></param>
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
