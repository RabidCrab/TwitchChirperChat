using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace TwitchChirperChat.UI
{
    public class TwitchChirpPanel : ToolsModifierControl
    {
	    private static readonly string kChirpTemplate = "ChirpTemplate";
	    public AudioClip m_NotificationSound;
	    public int m_MessageBufferSize = 16;
	    private SavedBool m_AutoExpand = new SavedBool(Settings.autoExpandChirper, Settings.gameSettingsFile, DefaultSettings.autoExpandChirper, true);
	    private SavedFloat m_ChirperAudioVolume = new SavedFloat(Settings.chirperAudioVolume, Settings.gameSettingsFile, DefaultSettings.chirperAudioVolume, true);
	    public float m_MessageTimeout = 6f;
	    public float m_ShowHideTime = 0.3f;
	    public EasingType m_ShowEasingType = EasingType.ExpoEaseOut;
	    public EasingType m_HideEasingType = EasingType.ExpoEaseOut;
	    private int m_NewMessageCount;
	    private UIScrollablePanel m_Container;
	    private UIPanel m_Chirps;
	    private UILabel m_Counter;
	    private Vector2 m_DefaultSize;
	    private bool m_Showing;
	    private float m_Timeout;
	    private static TwitchChirpPanel sInstance;
	    public bool isShowing
	    {
		    get
		    {
			    return this.m_Showing;
		    }
	    }
	    public static TwitchChirpPanel instance
	    {
		    get
		    {
			    return sInstance;
		    }
	    }
	    public void Expand()
	    {
		    this.Expand(0f);
	    }
	    public void Expand(float timeout)
	    {
		    this.Show(timeout);
	    }
	    public void Collapse()
	    {
		    this.Hide();
	    }
	    public void AddMessage(IChirperMessage message)
	    {
		    this.AddMessage(message, true);
	    }
	    public void AddMessage(IChirperMessage message, bool show)
	    {
		    this.AddEntry(message, false);
		    if (show && this.m_AutoExpand)
		    {
			    this.Show(this.m_MessageTimeout);
		    }
	    }
	    public void ClearMessages()
	    {
		    this.m_NewMessageCount = 0;
		    UITemplateManager.ClearInstances(kChirpTemplate);
	    }
	    [DebuggerHidden]
	    private IEnumerator<MessageBase[]> SimulationGetMessages()
	    {
	        return null; /*new TwitchChirpPanel.<SimulationGetMessages>c__Iterator1D();*/
	    }
	    [DebuggerHidden]
	    private IEnumerator SynchronizeMessagesCoroutine()
	    {
		    /*TwitchChirpPanel.<SynchronizeMessagesCoroutine>c__Iterator1E <SynchronizeMessagesCoroutine>c__Iterator1E = new TwitchChirpPanel.<SynchronizeMessagesCoroutine>c__Iterator1E();
		    <SynchronizeMessagesCoroutine>c__Iterator1E.<>f__this = this;
		    return <SynchronizeMessagesCoroutine>c__Iterator1E;*/
            return null;
	    }
	    public void SynchronizeMessages()
	    {
		    base.StartCoroutine(this.SynchronizeMessagesCoroutine());
	    }
	    private void Awake()
	    {
		    sInstance = this;
		    this.m_Counter = base.Find<UILabel>("Counter");
		    this.m_Counter.Hide();
		    this.m_Chirps = base.Find<UIPanel>("Chirps");
		    this.m_Container = base.Find<UIScrollablePanel>("Container");
		    this.m_DefaultSize = base.component.size;
		    base.component.size = new Vector2(38f, 45f);
		    this.m_Chirps.Hide();
		    new GameObject("ChirperBehaviourContainer", new Type[]
		    {
			    typeof(ChirperBehaviourContainer)
		    });

            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, "Custom panel enabled");
	    }
	    private void OnDestroy()
	    {
		    sInstance = null;
	    }
	    private void OnEnable()
	    {
		    this.SynchronizeMessages();
		    if (Singleton<MessageManager>.exists)
		    {
			    Singleton<MessageManager>.instance.m_messagesUpdated += new Action(this.SynchronizeMessages);
			    Singleton<MessageManager>.instance.m_newMessages += new MessageManager.NewMessageHandler(this.AddMessage);
		    }
	    }
	    private void OnDisable()
	    {
		    if (Singleton<MessageManager>.exists)
		    {
			    Singleton<MessageManager>.instance.m_messagesUpdated -= new Action(this.SynchronizeMessages);
			    Singleton<MessageManager>.instance.m_newMessages -= new MessageManager.NewMessageHandler(this.AddMessage);
		    }
	    }
	    public void Show(float timeout)
	    {
		    this.m_Counter.Hide();
		    this.m_NewMessageCount = 0;
		    if (!this.m_Showing)
		    {
			    this.m_Timeout = timeout;
		    }
		    this.m_Showing = true;
		    if (!this.m_Chirps.isVisible)
		    {
			    this.m_Chirps.Show();
			    this.m_Container.ScrollToBottom();
			    ValueAnimator.Animate("ChirpPanelX", delegate(float val)
			    {
				    Vector2 size = base.component.size;
				    size.x = val;
				    base.component.size = size;
			    }, new AnimatedFloat(38f, this.m_DefaultSize.x, this.m_ShowHideTime, this.m_ShowEasingType), delegate
			    {
				    if (this.m_Showing)
				    {
					    ValueAnimator.Animate("ChirpPanelY", delegate(float val)
					    {
						    Vector2 size = base.component.size;
						    size.y = val;
						    base.component.size = size;
					    }, new AnimatedFloat(45f, this.m_DefaultSize.y, this.m_ShowHideTime, this.m_ShowEasingType));
				    }
			    });
		    }
	    }
	    public void Hide()
	    {
		    this.m_Showing = false;
		    if (this.m_Chirps.isVisible)
		    {
			    if (ValueAnimator.IsAnimating("ChirpPanelX"))
			    {
				    ValueAnimator.Animate("ChirpPanelX", delegate(float val)
				    {
					    Vector2 size = base.component.size;
					    size.x = val;
					    base.component.size = size;
				    }, new AnimatedFloat(this.m_DefaultSize.x, 38f, this.m_ShowHideTime, this.m_ShowEasingType), delegate
				    {
					    this.m_Chirps.Hide();
				    });
			    }
			    else
			    {
				    ValueAnimator.Animate("ChirpPanelY", delegate(float val)
				    {
					    Vector2 size = base.component.size;
					    size.y = val;
					    base.component.size = size;
				    }, new AnimatedFloat(this.m_DefaultSize.y, 45f, this.m_ShowHideTime, this.m_ShowEasingType), delegate
				    {
					    ValueAnimator.Animate("ChirpPanelX", delegate(float val)
					    {
						    Vector2 size = base.component.size;
						    size.x = val;
						    base.component.size = size;
					    }, new AnimatedFloat(this.m_DefaultSize.x, 38f, this.m_ShowHideTime, this.m_ShowEasingType), delegate
					    {
						    this.m_Chirps.Hide();
					    });
				    });
			    }
		    }
	    }
	    public void OnMouseDown(UIComponent component, UIMouseEventParameter p)
	    {
		    this.m_Timeout = 0f;
	    }
	    [DebuggerHidden]
	    private IEnumerator SimulationDeleteMessage(MessageBase message)
	    {
		    /*TwitchChirpPanel.<SimulationDeleteMessage>c__Iterator1F <SimulationDeleteMessage>c__Iterator1F = new TwitchChirpPanel.<SimulationDeleteMessage>c__Iterator1F();
		    <SimulationDeleteMessage>c__Iterator1F.message = message;
		    <SimulationDeleteMessage>c__Iterator1F.<$>message = message;
		    return <SimulationDeleteMessage>c__Iterator1F;*/
	        return null;
	    }
	    public void OnClick(UIComponent comp, UIMouseEventParameter p)
	    {
		    if (p != null && p.source is UIButton && p.source.name == "Remove")
		    {
			    if (Singleton<SimulationManager>.exists && Singleton<MessageManager>.exists)
			    {
				    Singleton<SimulationManager>.instance.AddAction(this.SimulationDeleteMessage((MessageBase)p.source.parent.objectUserData));
			    }
			    UITemplateManager.RemoveInstance(kChirpTemplate, p.source.parent);
			    if (this.m_Container.childCount == 0)
			    {
				    this.Hide();
			    }
		    }
	    }
	    public static float Integrate(float src, float dst, float speed, float time)
	    {
		    float num = 1f - Mathf.Pow(1f - speed, time);
		    return src + (dst - src) * num;
	    }
	    public static Vector3 Integrate(Vector3 src, Vector3 dst, float speed, float time)
	    {
		    return new Vector3(Integrate(src.x, dst.x, speed, time), Integrate(src.y, dst.y, speed, time), Integrate(src.z, dst.z, speed, time));
	    }
	    private void Update()
	    {
		    if (this.m_Showing && this.m_Timeout > 0f)
		    {
			    this.m_Timeout -= Time.deltaTime;
			    if (this.m_Timeout <= 0f)
			    {
				    this.Hide();
			    }
		    }
	    }
	    public void Toggle()
	    {
		    if (this.m_Showing)
		    {
			    this.Hide();
		    }
		    else
		    {
			    this.Show(0f);
		    }
	    }
	    protected void OnTargetClick(UIComponent comp, UIMouseEventParameter p)
	    {
		    if (p.source != null && cameraController != null)
		    {
			    InstanceID id = (InstanceID)p.source.objectUserData;
			    if (InstanceManager.IsValid(id))
			    {
				    cameraController.SetTarget(id, cameraController.transform.position, true);
			    }
		    }
	    }
	    public void AddEntry(IChirperMessage message, bool noAudio)
	    {
		    if (this.m_NotificationSound != null && !noAudio)
		    {
			    Singleton<AudioManager>.instance.PlaySound(this.m_NotificationSound, this.m_ChirperAudioVolume.value);
		    }
		    if (!this.m_Showing)
		    {
			    this.m_NewMessageCount++;
			    this.m_Counter.Show();
			    this.m_Counter.text = this.m_NewMessageCount.ToString();
		    }
		    int childCount = this.m_Container.childCount;
		    UIPanel uIPanel;
		    if (childCount + 1 > this.m_MessageBufferSize && childCount > 0)
		    {
			    uIPanel = (this.m_Container.components[0] as UIPanel);
		    }
		    else
		    {
			    GameObject asGameObject = UITemplateManager.GetAsGameObject(kChirpTemplate);
			    uIPanel = (this.m_Container.AttachUIComponent(asGameObject) as UIPanel);
		    }
		    UIButton uIButton = uIPanel.Find<UIButton>("Sender");
		    uIButton.objectUserData = new InstanceID
		    {
			    Citizen = message.senderID
		    };
		    uIButton.eventClick += new MouseEventHandler(this.OnTargetClick);
		    UILabel uILabel = uIPanel.Find<UILabel>("Content");
		    uIPanel.FitTo(uIPanel.parent, LayoutDirection.Horizontal);
		    uIButton.text = message.senderName;
		    uILabel.text = message.text;
		    uIPanel.objectUserData = message;
		    uIPanel.FitChildrenVertically(10f);
		    uIPanel.zOrder = 2147483647;
		    this.m_Container.ScrollToBottom();
	    }
    }
}
