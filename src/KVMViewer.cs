/*
Copyright 2009-2022 Intel Corporation

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace MeshCentralRouter
{
    public partial class KVMViewer : Form
    {
        // Cache for tinted icons: keyed by (source image hash, tint color ARGB)
        private Dictionary<string, Bitmap> _tintedIconCache = new Dictionary<string, Bitmap>();

        private Image GetTintedIcon(Image source, Color tint)
        {
            if (source == null) { return null; }

            // Create a unique key for this source + tint combination
            string cacheKey = source.GetHashCode() + "|" + tint.ToArgb();

            if (_tintedIconCache.ContainsKey(cacheKey))
            {
                return _tintedIconCache[cacheKey];
            }

            Bitmap tinted = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var srcBmp = new Bitmap(source))
            {
                for (int y = 0; y < srcBmp.Height; y++)
                {
                    for (int x = 0; x < srcBmp.Width; x++)
                    {
                        Color p = srcBmp.GetPixel(x, y);
                        if (p.A == 0)
                        {
                            tinted.SetPixel(x, y, Color.Transparent);
                        }
                        else
                        {
                            // Preserve alpha, replace RGB with tint
                            tinted.SetPixel(x, y, Color.FromArgb(p.A, tint.R, tint.G, tint.B));
                        }
                    }
                }
            }

            _tintedIconCache[cacheKey] = tinted;
            return tinted;
        }

        private MainForm parent = null;
        private KVMControl kvmControl = null;
        private KVMStats kvmStats = null;
        private MeshCentralServer server = null;
        private NodeClass node = null;
        private int state = 0;
        private RandomNumberGenerator rand = RandomNumberGenerator.Create();
        private string randomIdHex = null;
        private bool sessionIsRecorded = false;
        public int consentFlags = 0;
        public webSocketClient wc = null;
        public Dictionary<string, int> userSessions = null;
        private string lastClipboardSent = null;
        private DateTime lastClipboardTime = DateTime.MinValue;
        public string lang = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        private bool splitMode = false;
        private KVMViewerExtra[] extraDisplays = null;
        private System.Windows.Forms.Timer delayedConnectionTimer = null;
        private bool localAutoReconnect = true;
        private Dictionary<int, Button> displaySelectionButtons = new Dictionary<int, Button>();

        // Settings pane controls (using standardized SettingsToggleItem and SettingsActionButton)
        private SettingsActionButton settingsConnectionButton = null;
        private SettingsToggleItem settingsStatusBarItem = null;
        private SettingsToggleItem settingsAutoReconnectItem = null;
        private SettingsToggleItem settingsSwapMouseItem = null;
        private SettingsToggleItem settingsRemoteKeyMapItem = null;
        private SettingsToggleItem settingsSyncClipboardItem = null;

        // Title bar dragging support
        private bool isDragging = false;
        private Point dragOffset;

        // Stats
        public long bytesIn = 0;
        public long bytesInCompressed = 0;
        public long bytesOut = 0;
        public long bytesOutCompressed = 0;

        // Frame rate panel toggle button group
        private ToggleButtonGroup frameRateButtonGroup;

        // Quality (compression) controls
        private Label qualityLevelLabel;
        private int currentQualityPercent = 60; // Current quality percentage (20-100, steps of 20)

        // Display scaling controls
        private Label scalingLevelLabel;
        private double currentScalingPercent = 100; // Current display zoom percentage (12.5-200)
        private List<Button> scalingPresetButtons = new List<Button>();

        // Auto-hide title bar when maximized
        private bool titleBarVisible = true;
        private System.Windows.Forms.Timer titleBarHideTimer;
        private System.Windows.Forms.Timer titleBarAnimationTimer;
        private System.Windows.Forms.Timer titleBarShowDelayTimer;
        private int titleBarTargetTop;
        private const int TITLE_BAR_ANIMATION_STEP = 4;
        private const int TITLE_BAR_SHOW_DELAY_MS = 100;

        public KVMViewer(MainForm parent, MeshCentralServer server, NodeClass node)
        {
            this.parent = parent;
            InitializeComponent();
            Translate.TranslateControl(this);
            this.Text += " - " + node.name;
            this.node = node;
            this.server = server;

            // Update custom titlebar with node name
            titleLabel.Text = "Remote Desktop - " + node.name;
            kvmControl = resizeKvmControl.KVM;
            kvmControl.parent = this;
            kvmControl.DesktopSizeChanged += KvmControl_DesktopSizeChanged;
            kvmControl.ScreenAreaUpdated += KvmControl_ScreenAreaUpdated;
            kvmControl.MouseDown += KvmControl_MouseDown_ClosePane;
            resizeKvmControl.MouseDown += KvmControl_MouseDown_ClosePane;
            mainStatusStrip.MouseDown += KvmControl_MouseDown_ClosePane;
            titleBarPanel.MouseDown += KvmControl_MouseDown_ClosePane;
            resizeKvmControl.ZoomToFit = true;
            UpdateStatus();
            this.MouseWheel += MainForm_MouseWheel;
            parent.ClipboardChanged += Parent_ClipboardChanged;

            mainToolTip.SetToolTip(connectButton, Translate.T(Properties.Resources.ToggleRemoteDesktopConnection, lang));
            mainToolTip.SetToolTip(cadButton, Translate.T(Properties.Resources.SendCtrlAltDelToRemoteDevice, lang));
            mainToolTip.SetToolTip(settingsButton, Translate.T(Properties.Resources.ChangeRemoteDesktopSettings, lang));
            mainToolTip.SetToolTip(clipOutboundButton, Translate.T(Properties.Resources.PushLocaClipboardToRemoteDevice, lang));
            mainToolTip.SetToolTip(clipInboundButton, Translate.T(Properties.Resources.PullClipboardFromRemoteDevice, lang));
            mainToolTip.SetToolTip(zoomButton, Translate.T(Properties.Resources.ToggleZoomToFitMode, lang));
            mainToolTip.SetToolTip(statsButton, Translate.T(Properties.Resources.DisplayConnectionStatistics, lang));
            mainToolTip.SetToolTip(infoButton, Translate.T(Properties.Resources.DisplayConnectionStatistics, lang));
            mainToolTip.SetToolTip(displayButton, Translate.T(Properties.Resources.DisplaySettings, lang));
            mainToolTip.SetToolTip(otherButton, "Other");

            // Load remote desktop settings
            int CompressionLevel = 60;
            try { CompressionLevel = int.Parse(Settings.GetRegValue("kvmCompression", "60")); } catch (Exception) { }
            int ScalingLevel = 1024;
            try { ScalingLevel = int.Parse(Settings.GetRegValue("kvmScaling", "1024")); } catch (Exception) { }
            int FrameRate = 100;
            try { FrameRate = int.Parse(Settings.GetRegValue("kvmFrameRate", "100")); } catch (Exception) { }
            kvmControl.SetCompressionParams(CompressionLevel, ScalingLevel, FrameRate);
            kvmControl.SwamMouseButtons = Settings.GetRegValue("kvmSwamMouseButtons", "0").Equals("1");
            kvmControl.RemoteKeyboardMap = Settings.GetRegValue("kvmSwamMouseButtons", "0").Equals("1");
            kvmControl.AutoSendClipboard = Settings.GetRegValue("kvmAutoClipboard", "0").Equals("1");
            kvmControl.AutoReconnect = Settings.GetRegValue("kvmAutoReconnect", "0").Equals("1");

            // Subscribe to theme changes and apply initial theme
            ThemeManager.Instance.ThemeChanged += ThemeManager_ThemeChanged;
            UpdateTheme();

            // Keep title bar buttons aligned from the top-right edge
            titleBarPanel.Resize += TitleBarPanel_Resize;
            PositionTitleBarButtons();

            // Load status bar visibility preference
            bool statusBarVisible = Settings.GetRegValue("kvmStatusBarVisible", "1").Equals("1");
            mainStatusStrip.Visible = statusBarVisible;
            paneStatusBarToggleSwitch.Checked = statusBarVisible;

            // Load auto reconnect preference and sync toggle
            paneAutoReconnectToggleSwitch.Checked = kvmControl.AutoReconnect;

            // Load swap mouse buttons preference and sync toggle
            paneSwapMouseToggleSwitch.Checked = kvmControl.SwamMouseButtons;

            // Load remote keyboard map preference and sync toggle
            paneRemoteKeyMapToggleSwitch.Checked = kvmControl.RemoteKeyboardMap;

            // Load auto send clipboard preference and sync toggle
            paneSyncClipboardToggleSwitch.Checked = kvmControl.AutoSendClipboard;

            // Load display scaling preference
            try { currentScalingPercent = double.Parse(Settings.GetRegValue("kvmDisplayScaling", "100")); } catch (Exception) { currentScalingPercent = 100; }
            if (currentScalingPercent < 12.5) currentScalingPercent = 12.5;
            if (currentScalingPercent > 200) currentScalingPercent = 200;

            // Setup auto-hide title bar timers
            titleBarHideTimer = new System.Windows.Forms.Timer();
            titleBarHideTimer.Interval = 100;
            titleBarHideTimer.Tick += TitleBarHideTimer_Tick;
            titleBarHideTimer.Start();

            titleBarAnimationTimer = new System.Windows.Forms.Timer();
            titleBarAnimationTimer.Interval = 16; // ~60fps
            titleBarAnimationTimer.Tick += TitleBarAnimationTimer_Tick;

            titleBarShowDelayTimer = new System.Windows.Forms.Timer();
            titleBarShowDelayTimer.Interval = TITLE_BAR_SHOW_DELAY_MS;
            titleBarShowDelayTimer.Tick += TitleBarShowDelayTimer_Tick;
        }

        private void KvmControl_ScreenAreaUpdated(Bitmap desktop, Rectangle r)
        {
            if (extraDisplays == null) return;
            foreach (KVMViewerExtra x in extraDisplays) {
                if (x != null) { x.UpdateScreenArea(desktop, r); }
            }
        }

        private void Parent_ClipboardChanged()
        {
            if (state != 3) return;
            if (kvmControl.AutoSendClipboard) { SendClipboard(); }
        }

        private delegate void SendClipboardHandler();

        private void SendClipboard()
        {
            if (this.InvokeRequired) { this.Invoke(new SendClipboardHandler(SendClipboard)); return; }
            string textData = (string)Clipboard.GetData(DataFormats.Text);
            if (textData != null)
            {
                if ((DateTime.Now.Subtract(lastClipboardTime).TotalSeconds < 20) && (lastClipboardSent != null) && (lastClipboardSent.Equals(textData))) return; // Don't resend clipboard if same and sent in last 20 seconds. This avoids clipboard loop.
                string textData2 = textData.Replace("\\", "\\\\").Replace("\"", "\\\"");
                server.sendCommand("{\"action\":\"msg\",\"type\":\"setclip\",\"nodeid\":\"" + node.nodeid + "\",\"data\":\"" + textData2 + "\"}");
                lastClipboardTime = DateTime.Now;
                lastClipboardSent = textData;
            }
        }

        public void TryAutoConnect()
        {
            if ((localAutoReconnect == false) || (kvmControl.AutoReconnect == false)) return;
            if ((state == 0) && (wc == null) && (delayedConnectionTimer == null)) {
                // Hold half a second before trying to connect
                delayedConnectionTimer = new System.Windows.Forms.Timer(this.components);
                delayedConnectionTimer.Tick += new EventHandler(updateTimerTick);
                delayedConnectionTimer.Interval = 500;
                delayedConnectionTimer.Enabled = true;
            }
        }

        private void updateTimerTick(object sender, EventArgs e)
        {
            delayedConnectionTimer.Dispose();
            delayedConnectionTimer = null;
            if ((state == 0) && (wc == null)) { MenuItemConnect_Click(this, null); }
        }

        private void KvmControl_DesktopSizeChanged(object sender, EventArgs e)
        {
            kvmControl.Visible = true;
        }

        private void Server_onStateChanged(int state)
        {
            UpdateStatus();
        }

        void MainForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta == 0) return;
            Control c = this.GetChildAtPoint(e.Location);
            if (c != null && c == resizeKvmControl) resizeKvmControl.MouseWheelEx(sender, e);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Size = new Size(820, 480);
            resizeKvmControl.CenterKvmControl(false);
            CenterTitleBarControls();

            // Restore Window Location
            string locationStr = Settings.GetRegValue("kvmlocation", "");
            if (locationStr != null)
            {
                string[] locationSplit = locationStr.Split(',');
                if (locationSplit.Length == 4)
                {
                    try
                    {
                        var x = int.Parse(locationSplit[0]);
                        var y = int.Parse(locationSplit[1]);
                        var w = int.Parse(locationSplit[2]);
                        var h = int.Parse(locationSplit[3]);
                        Point p = new Point(x, y);
                        if (isPointVisibleOnAScreen(p))
                        {
                            Location = p;
                            if ((w > 50) && (h > 50)) { Size = new Size(w, h); }
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        public void OnScreenChanged()
        {
            resizeKvmControl.CenterKvmControl(true);
        }

        private void MenuItemExit_Click(object sender, EventArgs e)
        {
            node.desktopViewer = null;
            closeKvmStats();
            Close();
        }

        public void MenuItemConnect_Click(object sender, EventArgs e)
        {
            if (wc != null) return;
            byte[] randomid = new byte[10];
            rand.GetBytes(randomid);
            randomIdHex = BitConverter.ToString(randomid).Replace("-", string.Empty);

            state = 1;
            string ux = server.wsurl.ToString().Replace("/control.ashx", "/");
            int i = ux.IndexOf("?");
            if (i >= 0) { ux = ux.Substring(0, i); }
            Uri u = new Uri(ux + "meshrelay.ashx?browser=1&p=2&nodeid=" + node.nodeid + "&id=" + randomIdHex + "&auth=" + server.authCookie);
            wc = new webSocketClient();
            wc.debug = server.debug;
            wc.onStateChanged += Wc_onStateChanged;
            wc.onBinaryData += Wc_onBinaryData;
            wc.onStringData += Wc_onStringData;
            wc.TLSCertCheck = webSocketClient.TLSCertificateCheck.Fingerprint;
            wc.Start(u, server.wshash, null);
        }

        private void Wc_onStateChanged(webSocketClient sender, webSocketClient.ConnectionStates wsstate)
        {
            switch (wsstate)
            {
                case webSocketClient.ConnectionStates.Disconnected:
                    {
                        // Disconnect
                        state = 0;
                        wc.Dispose();
                        wc = null;
                        kvmControl.DetacheKeyboard();
                        break;
                    }
                case webSocketClient.ConnectionStates.Connecting:
                    {
                        state = 1;
                        displayMessage(null);
                        break;
                    }
                case webSocketClient.ConnectionStates.Connected:
                    {
                        // Reset stats
                        bytesIn = 0;
                        bytesInCompressed = 0;
                        bytesOut = 0;
                        bytesOutCompressed = 0;

                        state = 2;

                        string u = "*" + server.wsurl.AbsolutePath.Replace("control.ashx", "meshrelay.ashx") + "?p=2&nodeid=" + node.nodeid + "&id=" + randomIdHex + "&rauth=" + server.rauthCookie;
                        server.sendCommand("{ \"action\": \"msg\", \"type\": \"tunnel\", \"nodeid\": \"" + node.nodeid + "\", \"value\": \"" + u.ToString() + "\", \"usage\": 2 }");
                        displayMessage(null);
                        break;
                    }
            }
            UpdateStatus();
        }

        private void Wc_onStringData(webSocketClient sender, string data, int orglen)
        {
            bytesIn += data.Length;
            bytesInCompressed += orglen;

            if ((state == 2) && ((data == "c") || (data == "cr")))
            {
                if (data == "cr") { sessionIsRecorded = true; }
                state = 3;

                // Send any connection options here
                if (consentFlags != 0) { kvmControl.Send("{ \"type\": \"options\", \"consent\": " + consentFlags + " }"); }

                // Send remote desktop protocol (2)
                kvmControl.Send("2");
                kvmControl.SendCompressionLevel();
                kvmControl.SendPause(false);
                kvmControl.SendRefresh();
                UpdateStatus();
                displayMessage(null);

                // Send clipboard
                if (kvmControl.AutoSendClipboard) { SendClipboard(); }

                return;
            }
            if (state != 3) return;

            // Parse the received JSON
            Dictionary<string, object> jsonAction = new Dictionary<string, object>();
            jsonAction = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(data);
            if ((jsonAction == null) || (jsonAction.ContainsKey("type") == false) || (jsonAction["type"].GetType() != typeof(string))) return;

            string action = jsonAction["type"].ToString();
            switch (action)
            {
                case "metadata":
                    {
                        if ((jsonAction.ContainsKey("users") == false) || (jsonAction["users"] == null)) return;
                        Dictionary<string, object> usersex = (Dictionary<string, object>)jsonAction["users"];
                        userSessions = new Dictionary<string, int>();
                        foreach (string user in usersex.Keys) { userSessions.Add(user, (int)usersex[user]); }
                        UpdateStatus();
                        break;
                    }
                case "console":
                    {
                        string msg = null;
                        int msgid = -1;
                        if ((jsonAction.ContainsKey("msg")) && (jsonAction["msg"] != null)) { msg = jsonAction["msg"].ToString(); }
                        if (jsonAction.ContainsKey("msgid")) { msgid = (int)jsonAction["msgid"]; }
                        if (msgid == 1) { msg = Translate.T(Properties.Resources.WaitingForUserToGrantAccess, lang); }
                        if (msgid == 2) { msg = Translate.T(Properties.Resources.Denied, lang); }
                        if (msgid == 3) { msg = Translate.T(Properties.Resources.FailedToStartRemoteDesktopSession, lang); }
                        if (msgid == 4) { msg = Translate.T(Properties.Resources.Timeout, lang); }
                        if (msgid == 5) { msg = Translate.T(Properties.Resources.ReceivedInvalidNetworkData, lang); }
                        displayMessage(msg);
                        break;
                    }
            }
        }

        private void Wc_onBinaryData(webSocketClient sender, byte[] data, int offset, int length, int orglen)
        {
            bytesIn += length;
            bytesInCompressed += orglen;

            if (state != 3) return;
            kvmControl.ProcessData(data, offset, length);
        }

        private void MenuItemDisconnect_Click(object sender, EventArgs e)
        {
            if (wc != null)
            {
                // Disconnect
                if (splitMode) { splitButton_Click(this, null); }
                splitButton.Visible = false;
                state = 0;
                wc.Dispose();
                wc = null;
                UpdateStatus();
                localAutoReconnect = false;
            }
            else
            {
                // Connect
                if (sender != null) { consentFlags = 0; }
                MenuItemConnect_Click(null, null);
                kvmControl.AttachKeyboard();
                localAutoReconnect = true;
            }
            displayMessage(null);
        }


        public delegate void UpdateStatusHandler();

        private void UpdateStatus()
        {
            if (this.InvokeRequired) { try { this.Invoke(new UpdateStatusHandler(UpdateStatus)); } catch (Exception) { } return; }

            //if (kvmControl == null) return;
            switch (state)
            {
                case 0: // Disconnected
                    mainToolStripStatusLabel.Text = Translate.T(Properties.Resources.Disconnected, lang);
                    extraButtonsPanel.Visible = false;
                    kvmControl.Visible = false;
                    kvmControl.screenWidth = 0;
                    kvmControl.screenHeight = 0;
                    connectButton.Text = Translate.T(Properties.Resources.Connect, lang);
                    break;
                case 1: // Connecting
                    mainToolStripStatusLabel.Text = Translate.T(Properties.Resources.Connecting, lang);
                    extraButtonsPanel.Visible = false;
                    kvmControl.Visible = false;
                    connectButton.Text = Translate.T(Properties.Resources.Disconnect, lang);
                    break;
                case 2: // Setup
                    mainToolStripStatusLabel.Text = "Setup...";
                    extraButtonsPanel.Visible = false;
                    kvmControl.Visible = false;
                    connectButton.Text = Translate.T(Properties.Resources.Disconnect, lang);
                    break;
                case 3: // Connected
                    string label = Translate.T(Properties.Resources.Connected, lang);
                    if (sessionIsRecorded) { label += Translate.T(Properties.Resources.RecordedSession, lang); }
                    if ((userSessions != null) && (userSessions.Count > 1)) { label += string.Format(Translate.T(Properties.Resources.AddXUsers, lang), userSessions.Count); }
                    label += ".";
                    mainToolStripStatusLabel.Text = label;
                    connectButton.Text = Translate.T(Properties.Resources.Disconnect, lang);
                    kvmControl.SendCompressionLevel();
                    break;
            }

            cadButton.Enabled = (state == 3);
            openRemoteFilesButton.Enabled = (state == 3 && (node.agentcaps & 4) != 0);
            if ((kvmControl.AutoSendClipboard) && ((server.features2 & 0x1000) == 0)) // 0x1000 Clipboard Set
            {
                clipInboundButton.Visible = false;
                clipOutboundButton.Visible = false;
            }
            else
            {
                clipInboundButton.Visible = ((server.features2 & 0x0800) == 0); // 0x0800 Clipboard Get
                clipOutboundButton.Visible = ((server.features2 & 0x1000) == 0); // 0x1000 Clipboard Set
            }
            clipInboundButton.Enabled = (state == 3);
            clipOutboundButton.Enabled = (state == 3);

            // Update connection button in settings pane if visible
            if (settingsConnectionButton != null)
            {
                bool isDisconnected = (state == 0);
                settingsConnectionButton.Icon = isDisconnected ? "🔌" : "⏏";
                settingsConnectionButton.LabelText = isDisconnected ? Translate.T(Properties.Resources.Connect, lang) : Translate.T(Properties.Resources.Disconnect, lang);
            }
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop the title bar hide timer
            if (titleBarHideTimer != null)
            {
                titleBarHideTimer.Stop();
                titleBarHideTimer.Dispose();
                titleBarHideTimer = null;
            }
            if (titleBarAnimationTimer != null)
            {
                titleBarAnimationTimer.Stop();
                titleBarAnimationTimer.Dispose();
                titleBarAnimationTimer = null;
            }
            if (titleBarShowDelayTimer != null)
            {
                titleBarShowDelayTimer.Stop();
                titleBarShowDelayTimer.Dispose();
                titleBarShowDelayTimer = null;
            }

            if (wc != null)
            {
                // Disconnect
                state = 0;
                wc.Dispose();
                wc = null;
                UpdateStatus();
            }
            node.desktopViewer = null;
            closeKvmStats();

            // Save window location
            Settings.SetRegValue("kvmlocation", Location.X + "," + Location.Y + "," + Size.Width + "," + Size.Height);

            // Close any extra windows
            extraScreenClosed();
        }

        private void toolStripMenuItem2_DropDownOpening(object sender, EventArgs e)
        {
            //MenuItemConnect.Enabled = (kvmControl.State == KVMControl.ConnectState.Disconnected);
            //MenuItemDisconnect.Enabled = (kvmControl.State != KVMControl.ConnectState.Disconnected);
            //serverConnectToolStripMenuItem.Enabled = (server == null && kvmControl.State == KVMControl.ConnectState.Disconnected);
            //serviceDisconnectToolStripMenuItem.Enabled = (server != null && server.CurrentState != MeshSwarmServer.State.Disconnected);
        }

        private void kvmControl_StateChanged(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HideSettingsFlyout();
            if (kvmControl == null) return;
            using (KVMSettingsForm form = new KVMSettingsForm(server.features2))
            {
                form.Compression = kvmControl.CompressionLevel;
                form.Scaling = kvmControl.ScalingLevel;
                form.FrameRate = kvmControl.FrameRate;
                form.SwamMouseButtons = kvmControl.SwamMouseButtons;
                form.RemoteKeyboardMap = kvmControl.RemoteKeyboardMap;
                form.AutoSendClipboard = kvmControl.AutoSendClipboard;
                form.AutoReconnect = kvmControl.AutoReconnect;
                if (form.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    kvmControl.SetCompressionParams(form.Compression, form.Scaling, form.FrameRate);
                    kvmControl.SwamMouseButtons = form.SwamMouseButtons;
                    kvmControl.RemoteKeyboardMap = form.RemoteKeyboardMap;
                    kvmControl.AutoReconnect = form.AutoReconnect;

                    Settings.SetRegValue("kvmCompression", kvmControl.CompressionLevel.ToString());
                    Settings.SetRegValue("kvmScaling", kvmControl.ScalingLevel.ToString());
                    Settings.SetRegValue("kvmFrameRate", kvmControl.FrameRate.ToString());
                    Settings.SetRegValue("kvmSwamMouseButtons", kvmControl.SwamMouseButtons ? "1" : "0");
                    Settings.SetRegValue("kvmRemoteKeyboardMap", kvmControl.RemoteKeyboardMap ? "1" : "0");
                    Settings.SetRegValue("kvmAutoReconnect", kvmControl.AutoReconnect ? "1" : "0");

                    if (kvmControl.AutoSendClipboard != form.AutoSendClipboard)
                    {
                        kvmControl.AutoSendClipboard = form.AutoSendClipboard;
                        Settings.SetRegValue("kvmAutoClipboard", kvmControl.AutoSendClipboard ? "1" : "0");
                        if (kvmControl.AutoSendClipboard == true) { Parent_ClipboardChanged(); }
                    }
                    UpdateStatus();
                }
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (kvmControl != null) kvmControl.SendPause(WindowState == FormWindowState.Minimized);
            UpdateMaximizeButtonIcon();

            // Handle title bar overlay mode when maximized
            if (this.WindowState == FormWindowState.Maximized)
            {
                // When maximized, hide the title bar initially for auto-hide behavior
                if (titleBarVisible)
                {
                    titleBarVisible = false;
                    titleBarPanel.Dock = DockStyle.None;
                    titleBarPanel.Width = this.ClientSize.Width;
                    titleBarPanel.Top = -titleBarPanel.Height;
                    titleBarPanel.Left = 0;
                    titleBarPanel.Visible = false;
                }
            }
            else
            {
                // When not maximized, restore normal title bar docking
                titleBarAnimationTimer.Stop();
                titleBarShowDelayTimer.Stop();
                titleBarVisible = true;
                titleBarPanel.Dock = DockStyle.Top;
                titleBarPanel.Visible = true;
            }

            CenterTitleBarControls();
            PositionTitleBarButtons();
        }

        private void CenterTitleBarControls()
        {
            // Center the display, other, files, chat, gear and info buttons in the title bar (with small spacing between them)
            int spacing = 4;
            int totalWidth = displayButton.Width + spacing + otherButton.Width + spacing + openRemoteFilesButton.Width + spacing + chatButton.Width + spacing + gearButton.Width + spacing + infoButton.Width;
            int startX = (titleBarPanel.Width - totalWidth) / 2;
            int currentX = startX;

            displayButton.Location = new Point(currentX, displayButton.Location.Y);
            currentX += displayButton.Width + spacing;

            otherButton.Location = new Point(currentX, otherButton.Location.Y);
            currentX += otherButton.Width + spacing;

            openRemoteFilesButton.Location = new Point(currentX, openRemoteFilesButton.Location.Y);
            currentX += openRemoteFilesButton.Width + spacing;

            chatButton.Location = new Point(currentX, chatButton.Location.Y);
            currentX += chatButton.Width + spacing;

            gearButton.Location = new Point(currentX, gearButton.Location.Y);
            currentX += gearButton.Width + spacing;

            infoButton.Location = new Point(currentX, infoButton.Location.Y);

            // Also center the dropdown pane if it's visible
            if (dropdownPane.Visible)
            {
                int paneCenterX = (this.Width - dropdownPane.Width) / 2;
                dropdownPane.Location = new Point(paneCenterX, dropdownPane.Location.Y);
            }
        }

        private void PositionTitleBarButtons()
        {
            // Position buttons from the right edge moving left to avoid overlap
            int padding = 6;
            int spacing = 6;
            int y = closeButton.Location.Y;
            int x = titleBarPanel.Width - padding;

            x -= closeButton.Width;
            closeButton.Location = new Point(x, y);

            x -= spacing + maximizeButton.Width;
            maximizeButton.Location = new Point(x, y);

            x -= spacing + minimizeButton.Width;
            minimizeButton.Location = new Point(x, y);

            x -= spacing + themeButton.Width;
            themeButton.Location = new Point(x, y);

            x -= spacing + zoomButton.Width;
            zoomButton.Location = new Point(x, y);
        }

        private void UpdateMaximizeButtonIcon()
        {
            // Update button icon based on window state
            // Single square when normal, overlapping squares when maximized
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.maximizeButton.Text = "⬙"; // Overlapping squares
            }
            else
            {
                this.maximizeButton.Text = "⬜"; // Single square
            }
        }

        private void sendCtrlAltDelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (kvmControl != null) kvmControl.SendCtrlAltDel();
        }

        private void debugButton_Click(object sender, EventArgs e)
        {
            if (kvmControl != null) kvmControl.debugmode = !kvmControl.debugmode;
        }

        private void zoomButton_Click(object sender, EventArgs e)
        {
            resizeKvmControl.ZoomToFit = !resizeKvmControl.ZoomToFit;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void resizeKvmControl_DisplaysReceived(object sender, EventArgs e)
        {
            if (kvmControl == null || kvmControl.displays.Count == 0) return;

            if (kvmControl.displays.Count > 0)
            {
                extraButtonsPanel.Visible = true;
                extraButtonsPanel.Controls.Clear();
                displaySelectionButtons.Clear();
                foreach (ushort displayNum in kvmControl.displays)
                {
                    if (displayNum == 0xFFFF)
                    {
                        Button b = new Button();
                        b.ImageList = displaySelectorImageList;
                        b.ImageIndex = (kvmControl.currentDisp == displayNum) ? 2 : 3; // All displayes
                        b.Width = 32;
                        b.Height = 32;
                        mainToolTip.SetToolTip(b, Translate.T(Properties.Resources.AllDisplays, lang));
                        b.Click += new System.EventHandler(this.displaySelectComboBox_SelectionChangeCommitted);
                        b.Tag = displayNum;
                        b.Dock = DockStyle.Left;
                        extraButtonsPanel.Controls.Add(b);
                        displaySelectionButtons.Add(displayNum, b);
                    }
                    else
                    {
                        Button b = new Button();
                        b.ImageList = displaySelectorImageList;
                        b.ImageIndex = (kvmControl.currentDisp == displayNum) ? 0 : 1; // One display grayed out
                        b.Width = 32;
                        b.Height = 32;
                        mainToolTip.SetToolTip(b, string.Format(Translate.T(Properties.Resources.DisplayX, lang), displayNum));
                        b.Click += new System.EventHandler(this.displaySelectComboBox_SelectionChangeCommitted);
                        b.Tag = displayNum;
                        b.Dock = DockStyle.Left;
                        extraButtonsPanel.Controls.Add(b);
                        displaySelectionButtons.Add(displayNum, b);
                    }
                }
            }
            else
            {
                extraButtonsPanel.Visible = false;
                extraButtonsPanel.Controls.Clear();
                displaySelectionButtons.Clear();
            }

            // If there are many displays and all displays is selected, enable split/join button.
            splitButton.Visible = ((kvmControl.currentDisp == 65535) && (kvmControl.displays.Count > 1));
        }

        private void displaySelectComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (splitMode) { splitButton_Click(this, null); }
            if (kvmControl != null) {
                ushort displayNum = (ushort)((Button)sender).Tag;
                kvmControl.SendDisplay(displayNum);
            }
        }

        private void resizeKvmControl_TouchEnabledChanged(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void commandsToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            //if (server == null) return;
            //if (commandsToolStripMenuItem.Checked) server.ShowCommandViewer(); else server.HideCommandViewer();
        }

        private void emulateTouchToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            //kvmControl.emulateTouch = emulateTouchToolStripMenuItem.Checked;
        }

        private void packetsToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            //kvmControl.ShowPackets(packetsToolStripMenuItem.Checked);
        }

        private void kVMCommandsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //kvmControl.ShowCommands(kVMCommandsToolStripMenuItem.Checked);
        }

        private void winButton_Click(object sender, EventArgs e)
        {
            //kvmControl.SendWindowsKey();
        }

        private void charmButton_Click(object sender, EventArgs e)
        {
            //kvmControl.SendCharmsKey();
        }

        public delegate void displayMessageHandler(string msg);
        public void displayMessage(string msg)
        {
            if (this.InvokeRequired) { this.Invoke(new displayMessageHandler(displayMessage), msg); return; }
            if (msg == null)
            {
                consoleMessage.Visible = false;
                consoleTimer.Enabled = false;
            }
            else
            {
                consoleMessage.Text = msg;
                consoleMessage.Visible = true;
                //consoleTimer.Enabled = true;
            }
        }

        private void consoleTimer_Tick(object sender, EventArgs e)
        {
            consoleMessage.Visible = false;
            consoleTimer.Enabled = false;
        }

        private void statsButton_Click(object sender, EventArgs e)
        {
            HideSettingsFlyout();
            if (kvmStats == null)
            {
                kvmStats = new KVMStats(this);
                kvmStats.Show(this);
            }
            else
            {
                kvmStats.Focus();
            }
        }

        public void closeKvmStats()
        {
            if (kvmStats == null) return;
            kvmStats.Close();
            kvmStats = null;
        }

        // Enable resize hit-testing on a borderless window
        private const int wmNcHitTest = 0x84;
        private const int htLeft = 10;
        private const int htRight = 11;
        private const int htTop = 12;
        private const int htTopLeft = 13;
        private const int htTopRight = 14;
        private const int htBottom = 15;
        private const int htBottomLeft = 16;
        private const int htBottomRight = 17;
        private const int resizeBorderThickness = 15;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == wmNcHitTest)
            {
                // Extract screen coordinates from lParam
                int lParam = m.LParam.ToInt32();
                Point screenPos = new Point((short)(lParam & 0xFFFF), (short)((lParam >> 16) & 0xFFFF));
                Point clientPos = PointToClient(screenPos);

                bool onLeft = clientPos.X <= resizeBorderThickness;
                bool onRight = clientPos.X >= ClientSize.Width - resizeBorderThickness;
                bool onTop = clientPos.Y <= resizeBorderThickness;
                bool onBottom = clientPos.Y >= ClientSize.Height - resizeBorderThickness;

                if (onTop && onLeft) { m.Result = (IntPtr)htTopLeft; return; }
                if (onTop && onRight) { m.Result = (IntPtr)htTopRight; return; }
                if (onBottom && onLeft) { m.Result = (IntPtr)htBottomLeft; return; }
                if (onBottom && onRight) { m.Result = (IntPtr)htBottomRight; return; }
                if (onLeft) { m.Result = (IntPtr)htLeft; return; }
                if (onRight) { m.Result = (IntPtr)htRight; return; }
                if (onTop) { m.Result = (IntPtr)htTop; return; }
                if (onBottom) { m.Result = (IntPtr)htBottom; return; }
            }

            base.WndProc(ref m);
        }

        private void clipInboundButton_Click(object sender, EventArgs e)
        {
            //string textData = "abc";
            //Clipboard.SetData(DataFormats.Text, (Object)textData);
            server.sendCommand("{\"action\":\"msg\",\"type\":\"getclip\",\"nodeid\":\"" + node.nodeid + "\"}");
        }

        private void clipOutboundButton_Click(object sender, EventArgs e)
        {
            SendClipboard();
        }

        private void resizeKvmControl_Enter(object sender, EventArgs e)
        {
            kvmControl.AttachKeyboard();
            HideSettingsFlyout();
        }

        private void resizeKvmControl_Leave(object sender, EventArgs e)
        {
            kvmControl.DetacheKeyboard();
        }

        private void KVMViewer_Deactivate(object sender, EventArgs e)
        {
            kvmControl.DetacheKeyboard();
            // Auto-close dropdown pane when form loses focus
            if (dropdownPane.Visible)
            {
                HideDropdownPane();
            }
        }

        private void KVMViewer_Activated(object sender, EventArgs e)
        {
            kvmControl.AttachKeyboard();
        }

        bool isPointVisibleOnAScreen(Point p)
        {
            foreach (Screen s in Screen.AllScreens) { if ((p.X < s.Bounds.Right) && (p.X > s.Bounds.Left) && (p.Y > s.Bounds.Top) && (p.Y < s.Bounds.Bottom)) return true; }
            return false;
        }

        private void askConsentBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            consentFlags = 0x0008 + 0x0040; // Consent Prompt + Privacy bar
            MenuItemDisconnect_Click(null, null);
        }

        private void askConsentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            consentFlags = 0x0008; // Consent Prompt
            MenuItemDisconnect_Click(null, null);
        }

        private void privacyBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            consentFlags = 0x0040; // Privacy bar
            MenuItemDisconnect_Click(null, null);
        }

        private void consentContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (wc != null) { e.Cancel = true; }
        }

        public void extraScreenClosed()
        {
            if (splitMode) { splitButton_Click(this, null); }
        }

        private void splitButton_Click(object sender, EventArgs e)
        {
            if (splitMode)
            {
                kvmControl.cropDisplay(Point.Empty, Rectangle.Empty);
                splitButton.Text = Translate.T(Properties.Resources.Split, lang);
                splitMode = false;
                if (extraDisplays != null)
                {
                    // Close all extra displays
                    for (int i = 0; i < extraDisplays.Length; i++)
                    {
                        KVMViewerExtra extraDisplay = extraDisplays[i];
                        extraDisplay.Close();
                    }
                    extraDisplays = null;
                }
            }
            else if ((kvmControl.displayInfo != null) && (kvmControl.displayInfo.Length > 1))
            {
                int minx = 0;
                int miny = 0;
                foreach (Rectangle r in kvmControl.displayInfo) { if (r.X < minx) { minx = r.X; } if (r.Y < miny) { miny = r.Y; } }
                kvmControl.cropDisplay(new Point(minx, miny), kvmControl.displayInfo[0]);
                splitButton.Text = Translate.T(Properties.Resources.Join, lang);
                splitMode = true;

                // Open extra displays
                extraDisplays = new KVMViewerExtra[kvmControl.displayInfo.Length - 1];
                for (int i = 1; i < kvmControl.displayInfo.Length; i++)
                {
                    KVMViewerExtra extraDisplay = new KVMViewerExtra(parent, this, node, kvmControl, i);
                    extraDisplays[i - 1] = extraDisplay;
                    extraDisplay.Show(parent);
                }
            }
        }

        private void openRemoteFilesButton_Click(object sender, EventArgs e)
        {
            if ((node.conn & 1) == 0) { return; } // Agent not connected on this device

            if (node.fileViewer == null)
            {
                node.fileViewer = new FileViewer(server, node);
                node.fileViewer.Show();
                node.fileViewer.MenuItemConnect_Click(null, null);
            }
            else
            {
                node.fileViewer.Focus();
            }
        }

        private void otherButton_Click(object sender, EventArgs e)
        {
            // If clicking the same pane that's already open, close it
            if (dropdownPane.Visible && dropdownPaneLabel.Text == "Other")
            {
                HideDropdownPane();
                return;
            }

            // Set the title
            dropdownPaneLabel.Text = "Other";

            // Clear existing content
            dropdownPaneContent.Controls.Clear();

            var ps = new DropdownPaneStyle();
            int yOffset = DropdownPaneStyle.ContentTopPadding;

            // === Actions Section ===
            dropdownPaneContent.Controls.Add(ps.CreateSectionHeader("Actions", yOffset));
            yOffset += DropdownPaneStyle.SectionHeaderHeight;

            // CAD (Ctrl+Alt+Del) button - one unit wide, same height as frame rate buttons
            int buttonUnitWidth = DropdownPaneStyle.ButtonUnitWidth(DropdownPaneStyle.PaneWidth);
            Button cadPaneButton = new Button();
            ps.ApplyFlatButtonStyle(cadPaneButton);
            cadPaneButton.Font = DropdownPaneStyle.SmallFont;
            cadPaneButton.Text = "Send\nCtrl-Alt-Del";
            cadPaneButton.TextAlign = ContentAlignment.MiddleCenter;
            cadPaneButton.Cursor = Cursors.Hand;
            cadPaneButton.Location = new Point(DropdownPaneStyle.SidePadding, yOffset);
            cadPaneButton.Size = new Size(buttonUnitWidth, DropdownPaneStyle.ItemHeight);
            cadPaneButton.Click += (s, ev) => {
                sendCtrlAltDelToolStripMenuItem_Click(s, ev);
                HideDropdownPane();
            };
            dropdownPaneContent.Controls.Add(cadPaneButton);
            yOffset += DropdownPaneStyle.ItemHeight + 8;

            // Size and show the dropdown pane
            ps.FinalizePane(dropdownPane, dropdownPaneLabel, dropdownPaneContent,
                           yOffset, this.Width, titleBarPanel.Height);
        }

        private void chatButton_Click(object sender, EventArgs e)
        {
            if (kvmControl == null) return;
            server.sendCommand("{\"action\":\"meshmessenger\",\"nodeid\":\"" + node.nodeid + "\"}");
            string url = "https://" + server.serverinfo["name"];
            if (server.serverinfo.TryGetValue("port", out var value1) && value1 is int portNumber && portNumber != 443)
            {
                url += ":" + portNumber;
            }
            if (server.serverinfo.TryGetValue("domainsuffix", out var value) && value is string domainSuffix && !string.IsNullOrEmpty(domainSuffix))
            {
                url += "/" + domainSuffix;
            }
            url += "/messenger?id=meshmessenger/" + Uri.EscapeDataString(node.nodeid) + "/" + Uri.EscapeDataString(server.userid) + "&title=" + node.name;
            if ((server.authCookie != null) && (server.authCookie != "")) { url += "&auth=" + server.authCookie; }
            try
            {
                if (server.debug) { try { File.AppendAllText("debug.log", "Opening chat window locally using ProcessStartInfo\r\n"); } catch (Exception) { } }
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                try
                {
                    if (server.debug) { try { File.AppendAllText("debug.log", "Opening chat window locally using cmd\r\n"); } catch (Exception) { } }
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                catch (Exception)
                {
                    if (server.debug) { try { File.AppendAllText("debug.log", "Failed to open chat window locally\r\n"); } catch (Exception) { } }
                }
            }
        }

        // Title bar window dragging and theme button handlers
        private void titleBarPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragOffset = new Point(e.X, e.Y);
            }
        }

        private void titleBarPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point currentScreenPos = PointToScreen(e.Location);
                Location = new Point(currentScreenPos.X - dragOffset.X,
                                   currentScreenPos.Y - dragOffset.Y);
            }
        }

        private void titleBarPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void titleBarPanel_DoubleClick(object sender, EventArgs e)
        {
            // Toggle maximize/restore on double-click
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
            UpdateMaximizeButtonIcon();
        }

        private void TitleBarHideTimer_Tick(object sender, EventArgs e)
        {
            // Only auto-hide when maximized
            if (this.WindowState != FormWindowState.Maximized)
            {
                titleBarShowDelayTimer.Stop();
                titleBarAnimationTimer.Stop();
                if (!titleBarVisible)
                {
                    ShowTitleBar();
                }
                return;
            }

            // Don't process during animation
            if (titleBarAnimationTimer.Enabled)
            {
                return;
            }

            // Get mouse position in screen coordinates and check if within form bounds
            Point screenPos = Cursor.Position;
            Rectangle formBounds = this.Bounds;

            // Check if mouse is within the form's horizontal bounds
            bool mouseInFormX = screenPos.X >= formBounds.Left && screenPos.X < formBounds.Right;

            // Check if mouse is at the very top of the form (screen coordinates)
            int triggerZone = 5;
            bool mouseAtTop = mouseInFormX && screenPos.Y >= formBounds.Top && screenPos.Y <= formBounds.Top + triggerZone;

            // Check if mouse is over the title bar area (when visible)
            int titleBarHeight = titleBarPanel.Height;
            bool mouseOverTitleBar = titleBarVisible && mouseInFormX &&
                screenPos.Y >= formBounds.Top && screenPos.Y <= formBounds.Top + titleBarHeight;

            // Also check if dropdown pane is visible
            bool dropdownOpen = dropdownPane.Visible;

            if (mouseAtTop || mouseOverTitleBar || dropdownOpen)
            {
                if (!titleBarVisible && !titleBarShowDelayTimer.Enabled)
                {
                    // Start the delay timer before showing
                    titleBarShowDelayTimer.Start();
                }
            }
            else
            {
                // Mouse moved away, cancel any pending show
                titleBarShowDelayTimer.Stop();

                if (titleBarVisible && !isDragging)
                {
                    HideTitleBar();
                }
            }
        }

        private void TitleBarShowDelayTimer_Tick(object sender, EventArgs e)
        {
            titleBarShowDelayTimer.Stop();

            // Verify mouse is still at top before showing (using screen coordinates)
            Point screenPos = Cursor.Position;
            Rectangle formBounds = this.Bounds;
            bool mouseInFormX = screenPos.X >= formBounds.Left && screenPos.X < formBounds.Right;
            bool mouseAtTop = mouseInFormX && screenPos.Y >= formBounds.Top && screenPos.Y <= formBounds.Top + 5;

            if (mouseAtTop)
            {
                ShowTitleBar();
            }
        }

        private void TitleBarAnimationTimer_Tick(object sender, EventArgs e)
        {
            int currentTop = titleBarPanel.Top;

            if (currentTop < titleBarTargetTop)
            {
                // Animating down (showing)
                int newTop = Math.Min(currentTop + TITLE_BAR_ANIMATION_STEP, titleBarTargetTop);
                titleBarPanel.Top = newTop;

                if (newTop >= titleBarTargetTop)
                {
                    titleBarAnimationTimer.Stop();
                }
            }
            else if (currentTop > titleBarTargetTop)
            {
                // Animating up (hiding)
                int newTop = Math.Max(currentTop - TITLE_BAR_ANIMATION_STEP, titleBarTargetTop);
                titleBarPanel.Top = newTop;

                if (newTop <= titleBarTargetTop)
                {
                    titleBarAnimationTimer.Stop();
                    if (titleBarTargetTop < 0)
                    {
                        titleBarPanel.Visible = false;
                    }
                }
            }
            else
            {
                titleBarAnimationTimer.Stop();
            }
        }

        private void ShowTitleBar()
        {
            if (titleBarVisible) return;
            titleBarVisible = true;

            if (this.WindowState == FormWindowState.Maximized)
            {
                // Switch to None dock so it can overlay
                titleBarPanel.Dock = DockStyle.None;
                titleBarPanel.Width = this.ClientSize.Width;
                titleBarPanel.Top = -titleBarPanel.Height;
                titleBarPanel.Left = 0;
                titleBarPanel.Visible = true;
                titleBarPanel.BringToFront();
                dropdownPane.BringToFront();
                titleBarTargetTop = 0;
                titleBarAnimationTimer.Start();
            }
            else
            {
                // Not maximized: restore normal docking
                titleBarPanel.Dock = DockStyle.Top;
                titleBarPanel.Visible = true;
            }
        }

        private void HideTitleBar()
        {
            if (!titleBarVisible) return;
            titleBarVisible = false;

            // Also hide the dropdown pane if visible
            if (dropdownPane.Visible)
            {
                HideDropdownPane();
            }

            if (this.WindowState == FormWindowState.Maximized)
            {
                // Animate: slide up to hide
                titleBarTargetTop = -titleBarPanel.Height;
                titleBarAnimationTimer.Start();
            }
            else
            {
                // Not maximized: just hide (shouldn't happen, but handle it)
                titleBarPanel.Visible = false;
            }
        }

        private void themeButton_Click(object sender, EventArgs e)
        {
            ThemeManager.Instance.IsDarkMode = !ThemeManager.Instance.IsDarkMode;
        }

        private void infoButton_Click(object sender, EventArgs e)
        {
            // If clicking the same pane that's already open, close it
            if (dropdownPane.Visible && dropdownPaneLabel.Text == "Statistics")
            {
                HideDropdownPane();
                statisticsRefreshTimer.Enabled = false;
                return;
            }

            // Set the title
            dropdownPaneLabel.Text = "Statistics";

            // Clear existing content
            dropdownPaneContent.Controls.Clear();

            var ps = new DropdownPaneStyle();
            int yOffset = DropdownPaneStyle.ContentTopPadding;

            // Data Transfer section header
            dropdownPaneContent.Controls.Add(ps.CreateSectionHeader("Data Transfer", yOffset, DropdownPaneStyle.StatsRowHeight));
            yOffset += DropdownPaneStyle.StatsRowHeight + 4;

            // Create value labels
            statsBytesInValueLabel = new Label();
            statsBytesOutValueLabel = new Label();
            statsCompInValueLabel = new Label();
            statsCompOutValueLabel = new Label();
            statsInRatioValueLabel = new Label();
            statsOutRatioValueLabel = new Label();

            yOffset = ps.AddStatsRow(dropdownPaneContent, "Bytes In:", statsBytesInValueLabel, yOffset);
            yOffset = ps.AddStatsRow(dropdownPaneContent, "Bytes Out:", statsBytesOutValueLabel, yOffset);

            yOffset += 6; // Add spacing before compression section

            // Compression section header
            dropdownPaneContent.Controls.Add(ps.CreateSectionHeader("Compression", yOffset, DropdownPaneStyle.StatsRowHeight));
            yOffset += DropdownPaneStyle.StatsRowHeight + 4;

            yOffset = ps.AddStatsRow(dropdownPaneContent, "Compressed In:", statsCompInValueLabel, yOffset);
            yOffset = ps.AddStatsRow(dropdownPaneContent, "Compressed Out:", statsCompOutValueLabel, yOffset);
            yOffset = ps.AddStatsRow(dropdownPaneContent, "In Ratio:", statsInRatioValueLabel, yOffset);
            yOffset = ps.AddStatsRow(dropdownPaneContent, "Out Ratio:", statsOutRatioValueLabel, yOffset);

            yOffset += 4;

            // Size and show the dropdown pane
            ps.FinalizePane(dropdownPane, dropdownPaneLabel, dropdownPaneContent,
                           yOffset, this.Width, titleBarPanel.Bottom);

            // Update statistics immediately and start refresh timer
            UpdateStatisticsDisplay();
            statisticsRefreshTimer.Enabled = true;
        }

        private void statisticsRefreshTimer_Tick(object sender, EventArgs e)
        {
            // Only update if the statistics pane is visible
            if (dropdownPane.Visible && dropdownPaneLabel.Text == "Statistics")
            {
                UpdateStatisticsDisplay();
            }
            else
            {
                statisticsRefreshTimer.Enabled = false;
            }
        }

        private void UpdateStatisticsDisplay()
        {
            if (statsBytesInValueLabel == null) return;

            statsBytesInValueLabel.Text = FormatBytes(bytesIn);
            statsBytesOutValueLabel.Text = FormatBytes(bytesOut);
            statsCompInValueLabel.Text = FormatBytes(bytesInCompressed);
            statsCompOutValueLabel.Text = FormatBytes(bytesOutCompressed);

            if (bytesIn == 0)
            {
                statsInRatioValueLabel.Text = "0%";
            }
            else
            {
                statsInRatioValueLabel.Text = (100 - ((bytesInCompressed * 100) / bytesIn)) + "%";
            }

            if (bytesOut == 0)
            {
                statsOutRatioValueLabel.Text = "0%";
            }
            else
            {
                statsOutRatioValueLabel.Text = (100 - ((bytesOutCompressed * 100) / bytesOut)) + "%";
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return bytes + " B";
            else if (bytes < 1024 * 1024)
                return (bytes / 1024.0).ToString("F1") + " KB";
            else if (bytes < 1024 * 1024 * 1024)
                return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
            else
                return (bytes / (1024.0 * 1024 * 1024)).ToString("F1") + " GB";
        }

        private string FormatScalingPercent(double percent)
        {
            // Format as integer if whole number, otherwise show one decimal place
            if (percent % 1 == 0)
                return ((int)percent).ToString() + "%";
            else
                return percent.ToString("0.#") + "%";
        }

        private void displayButton_Click(object sender, EventArgs e)
        {
            // If clicking the same pane that's already open, close it
            if (dropdownPane.Visible && dropdownPaneLabel.Text == "Display")
            {
                HideDropdownPane();
                return;
            }

            // Set the title
            dropdownPaneLabel.Text = "Display";

            // Clear existing content
            dropdownPaneContent.Controls.Clear();

            var ps = new DropdownPaneStyle();
            int yOffset = DropdownPaneStyle.ContentTopPadding;

            // Select Displays section - only show if we have display info
            if (kvmControl != null && kvmControl.displays != null && kvmControl.displays.Count > 0)
            {
                // Select Displays section header
                dropdownPaneContent.Controls.Add(ps.CreateSectionHeader("Select Displays", yOffset));
                yOffset += DropdownPaneStyle.SectionHeaderHeight;

                // Calculate display button layout
                int displayCount = kvmControl.displays.Count;
                int displayButtonSize = 36; // Square buttons for display icons
                int displayButtonSpacing = DropdownPaneStyle.ButtonSpacing;
                int buttonsPerRow = Math.Min(displayCount, 6); // Max 6 per row
                int totalButtonsWidth = buttonsPerRow * displayButtonSize + (buttonsPerRow - 1) * displayButtonSpacing;
                int displayStartX = DropdownPaneStyle.SidePadding + (DropdownPaneStyle.PaneWidth - (DropdownPaneStyle.SidePadding * 2) - totalButtonsWidth) / 2;

                int displayX = displayStartX;
                int displayY = yOffset;
                int buttonsInRow = 0;

                // Create display selection buttons in the dropdown pane
                foreach (ushort displayNum in kvmControl.displays)
                {
                    Button displayBtn = new Button();
                    bool isSelected = (kvmControl.currentDisp == displayNum);
                    ps.ApplyFlatButtonStyle(displayBtn, isSelected);
                    displayBtn.Location = new Point(displayX, displayY);
                    displayBtn.Size = new Size(displayButtonSize, displayButtonSize);
                    displayBtn.Tag = displayNum;
                    displayBtn.ImageList = displaySelectorImageList;

                    if (displayNum == 0xFFFF)
                    {
                        // All displays button
                        displayBtn.ImageIndex = isSelected ? 2 : 3;
                        mainToolTip.SetToolTip(displayBtn, Translate.T(Properties.Resources.AllDisplays, lang));
                    }
                    else
                    {
                        // Individual display button
                        displayBtn.ImageIndex = isSelected ? 0 : 1;
                        mainToolTip.SetToolTip(displayBtn, string.Format(Translate.T(Properties.Resources.DisplayX, lang), displayNum));
                    }

                    displayBtn.Click += DisplayPaneButton_Click;
                    dropdownPaneContent.Controls.Add(displayBtn);

                    displayX += displayButtonSize + displayButtonSpacing;
                    buttonsInRow++;

                    if (buttonsInRow >= buttonsPerRow && buttonsInRow < displayCount)
                    {
                        // Move to next row
                        displayX = displayStartX;
                        displayY += displayButtonSize + displayButtonSpacing;
                        buttonsInRow = 0;
                    }
                }

                // Calculate the total height used by display buttons
                int totalRows = (displayCount + buttonsPerRow - 1) / buttonsPerRow;
                yOffset += totalRows * displayButtonSize + (totalRows - 1) * displayButtonSpacing + 4;

                // Add Split/Join button if applicable (multiple displays and "All Displays" is selected)
                if (kvmControl.currentDisp == 65535 && kvmControl.displays.Count > 1)
                {
                    yOffset += 4;
                    Button splitJoinBtn = new Button();
                    ps.ApplyFlatButtonStyle(splitJoinBtn);
                    splitJoinBtn.Font = DropdownPaneStyle.ItemFont;
                    splitJoinBtn.Location = new Point(DropdownPaneStyle.SidePadding, yOffset);
                    splitJoinBtn.Size = new Size(DropdownPaneStyle.PaneWidth - (DropdownPaneStyle.SidePadding * 2), DropdownPaneStyle.ItemHeight);
                    splitJoinBtn.Text = splitMode ? Translate.T(Properties.Resources.Join, lang) : Translate.T(Properties.Resources.Split, lang);
                    splitJoinBtn.Click += DisplayPaneSplitButton_Click;
                    dropdownPaneContent.Controls.Add(splitJoinBtn);
                    yOffset += DropdownPaneStyle.ItemHeight;
                }

                yOffset += 8; // Add spacing after section
            }

            // Frame Rate section header
            dropdownPaneContent.Controls.Add(ps.CreateSectionHeader(Translate.T(Properties.Resources.FrameRate, lang), yOffset));
            yOffset += DropdownPaneStyle.SectionHeaderHeight;

            // Create ToggleButtonGroup for frame rate options
            frameRateButtonGroup = new ToggleButtonGroup();
            frameRateButtonGroup.Location = new Point(0, yOffset);
            frameRateButtonGroup.Size = new Size(DropdownPaneStyle.PaneWidth, DropdownPaneStyle.ItemHeight);
            frameRateButtonGroup.ButtonSpacing = DropdownPaneStyle.ButtonSpacing;
            frameRateButtonGroup.SidePadding = DropdownPaneStyle.SidePadding;
            frameRateButtonGroup.SelectedValue = kvmControl.FrameRate;

            // Add frame rate options: Fast (50ms), Medium (100ms), Slow (400ms), Very Slow (1000ms)
            frameRateButtonGroup.AddButton(Translate.T(Properties.Resources.Fast, lang), 50);
            frameRateButtonGroup.AddButton(Translate.T(Properties.Resources.Medium, lang), 100);
            frameRateButtonGroup.AddButton(Translate.T(Properties.Resources.Slow, lang), 400);
            frameRateButtonGroup.AddButton(Translate.T(Properties.Resources.VerySlow, lang), 1000);

            // Apply theme colors
            frameRateButtonGroup.UpdateTheme(ps.PaneBgColor, ps.PaneTextColor, ps.SelectedColor,
                ps.BorderColor, ps.SelectedBorderColor, ps.PaneHoverColor);

            // Handle value changes
            frameRateButtonGroup.SelectedValueChanged += FrameRateButtonGroup_SelectedValueChanged;

            dropdownPaneContent.Controls.Add(frameRateButtonGroup);
            yOffset += DropdownPaneStyle.ItemHeight + 8;

            // Quality section header
            dropdownPaneContent.Controls.Add(ps.CreateSectionHeader("Quality", yOffset));
            yOffset += DropdownPaneStyle.SectionHeaderHeight;

            // Quality uses 1 unit wide, 2 half rows like scaling
            int qualityHalfHeight = DropdownPaneStyle.SectionHeaderHeight;
            int qualityRowWidth = DropdownPaneStyle.ButtonUnitWidth(DropdownPaneStyle.PaneWidth);
            int qualityButtonWidth = (qualityRowWidth - 2) / 2; // 2 buttons with small gap

            // Sync currentQualityPercent with kvmControl
            currentQualityPercent = kvmControl.CompressionLevel;

            // First row: Quality percentage label (full width of 1 unit)
            qualityLevelLabel = new Label();
            qualityLevelLabel.Font = DropdownPaneStyle.ScalingLabelFont;
            qualityLevelLabel.ForeColor = ps.PaneTextColor;
            qualityLevelLabel.BackColor = ps.PaneBgColor;
            qualityLevelLabel.Location = new Point(DropdownPaneStyle.SidePadding, yOffset);
            qualityLevelLabel.Size = new Size(qualityRowWidth, qualityHalfHeight);
            qualityLevelLabel.TextAlign = ContentAlignment.MiddleCenter;
            qualityLevelLabel.Text = currentQualityPercent.ToString() + "%";
            dropdownPaneContent.Controls.Add(qualityLevelLabel);

            yOffset += qualityHalfHeight + 2;

            // Second row: − button | + button
            Button qualityDownBtn = new Button();
            ps.ApplyFlatButtonStyle(qualityDownBtn);
            qualityDownBtn.Font = DropdownPaneStyle.ZoomButtonFont;
            qualityDownBtn.Location = new Point(DropdownPaneStyle.SidePadding, yOffset);
            qualityDownBtn.Size = new Size(qualityButtonWidth, qualityHalfHeight);
            qualityDownBtn.Text = "−";
            qualityDownBtn.Click += QualityDown_Click;
            dropdownPaneContent.Controls.Add(qualityDownBtn);

            Button qualityUpBtn = new Button();
            ps.ApplyFlatButtonStyle(qualityUpBtn);
            qualityUpBtn.Font = DropdownPaneStyle.ZoomButtonFont;
            qualityUpBtn.Location = new Point(DropdownPaneStyle.SidePadding + qualityButtonWidth + 2, yOffset);
            qualityUpBtn.Size = new Size(qualityButtonWidth, qualityHalfHeight);
            qualityUpBtn.Text = "+";
            qualityUpBtn.Click += QualityUp_Click;
            dropdownPaneContent.Controls.Add(qualityUpBtn);

            yOffset += qualityHalfHeight + 8;

            // Scaling section header
            dropdownPaneContent.Controls.Add(ps.CreateSectionHeader("Scaling", yOffset));
            yOffset += DropdownPaneStyle.SectionHeaderHeight;

            // First row: Zoom Out | Current Level | Zoom In (half-height)
            int halfHeight = DropdownPaneStyle.SectionHeaderHeight;
            int scalingRowWidth = DropdownPaneStyle.PaneWidth - (DropdownPaneStyle.SidePadding * 2);
            int zoomButtonWidth = (scalingRowWidth - 4) / 3; // 3 columns with small gaps

            // Zoom out button
            Button zoomOutBtn = new Button();
            ps.ApplyFlatButtonStyle(zoomOutBtn);
            zoomOutBtn.Font = DropdownPaneStyle.ZoomButtonFont;
            zoomOutBtn.Location = new Point(DropdownPaneStyle.SidePadding, yOffset);
            zoomOutBtn.Size = new Size(zoomButtonWidth, halfHeight);
            zoomOutBtn.Text = "−";
            zoomOutBtn.Click += ScalingZoomOut_Click;
            dropdownPaneContent.Controls.Add(zoomOutBtn);

            // Current scaling level label (center)
            scalingLevelLabel = new Label();
            scalingLevelLabel.Font = DropdownPaneStyle.ScalingLabelFont;
            scalingLevelLabel.ForeColor = ps.PaneTextColor;
            scalingLevelLabel.BackColor = ps.PaneBgColor;
            scalingLevelLabel.Location = new Point(DropdownPaneStyle.SidePadding + zoomButtonWidth + 2, yOffset);
            scalingLevelLabel.Size = new Size(zoomButtonWidth, halfHeight);
            scalingLevelLabel.TextAlign = ContentAlignment.MiddleCenter;
            scalingLevelLabel.Text = FormatScalingPercent(currentScalingPercent);
            dropdownPaneContent.Controls.Add(scalingLevelLabel);

            // Zoom in button
            Button zoomInBtn = new Button();
            ps.ApplyFlatButtonStyle(zoomInBtn);
            zoomInBtn.Font = DropdownPaneStyle.ZoomButtonFont;
            zoomInBtn.Location = new Point(DropdownPaneStyle.SidePadding + (zoomButtonWidth + 2) * 2, yOffset);
            zoomInBtn.Size = new Size(zoomButtonWidth, halfHeight);
            zoomInBtn.Text = "+";
            zoomInBtn.Click += ScalingZoomIn_Click;
            dropdownPaneContent.Controls.Add(zoomInBtn);

            yOffset += halfHeight + 4;

            // Second row: 50% | 75% | 100% | 150% | 200% preset buttons (half-height, 5 columns)
            int presetButtonWidth = (scalingRowWidth - 8) / 5; // 5 columns with small gaps

            scalingPresetButtons.Clear();
            double[] presetValues = { 50, 75, 100, 150, 200 };
            for (int i = 0; i < presetValues.Length; i++)
            {
                double presetValue = presetValues[i];
                bool isSelected = Math.Abs(currentScalingPercent - presetValue) < 0.01;
                Button presetBtn = new Button();
                ps.ApplyFlatButtonStyle(presetBtn, isSelected);
                presetBtn.Font = DropdownPaneStyle.SmallFont;
                presetBtn.Location = new Point(DropdownPaneStyle.SidePadding + i * (presetButtonWidth + 2), yOffset);
                presetBtn.Size = new Size(presetButtonWidth, halfHeight);
                presetBtn.Text = FormatScalingPercent(presetValue);
                presetBtn.Tag = presetValue;
                presetBtn.Click += ScalingPreset_Click;
                dropdownPaneContent.Controls.Add(presetBtn);
                scalingPresetButtons.Add(presetBtn);
            }

            yOffset += halfHeight + 4;

            // Size and show the dropdown pane
            ps.FinalizePane(dropdownPane, dropdownPaneLabel, dropdownPaneContent,
                           yOffset, this.Width, titleBarPanel.Bottom);
        }

        private void FrameRateButtonGroup_SelectedValueChanged(object sender, ToggleButtonValueChangedEventArgs e)
        {
            int frameRateValue = (int)e.Value;

            // Apply the new frame rate
            kvmControl.SetCompressionParams(kvmControl.CompressionLevel, kvmControl.ScalingLevel, frameRateValue);

            // Save to registry
            Settings.SetRegValue("kvmFrameRate", frameRateValue.ToString());
        }

        private void QualityDown_Click(object sender, EventArgs e)
        {
            // Decrease by 20% (minimum 20%)
            int newValue = currentQualityPercent - 20;
            if (newValue < 20) newValue = 20;
            ApplyQuality(newValue);
        }

        private void QualityUp_Click(object sender, EventArgs e)
        {
            // Increase by 20% (maximum 100%)
            int newValue = currentQualityPercent + 20;
            if (newValue > 100) newValue = 100;
            ApplyQuality(newValue);
        }

        private void ApplyQuality(int qualityPercent)
        {
            currentQualityPercent = qualityPercent;

            // Apply the new quality (compression level)
            kvmControl.SetCompressionParams(qualityPercent, kvmControl.ScalingLevel, kvmControl.FrameRate);

            // Update the label if it exists
            if (qualityLevelLabel != null)
            {
                qualityLevelLabel.Text = currentQualityPercent.ToString() + "%";
            }

            // Save to registry
            Settings.SetRegValue("kvmCompression", qualityPercent.ToString());
        }

        private void DisplayPaneButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null || kvmControl == null) return;

            ushort displayNum = (ushort)btn.Tag;

            // If in split mode, exit it first
            if (splitMode) { splitButton_Click(this, null); }

            // Send the display switch command
            kvmControl.SendDisplay(displayNum);

            // Refresh the display pane to show updated selection
            displayButton_Click(sender, e);
        }

        private void DisplayPaneSplitButton_Click(object sender, EventArgs e)
        {
            // Toggle split mode
            splitButton_Click(this, null);

            // Refresh the display pane to show updated button text
            displayButton_Click(sender, e);
        }

        private void ScalingZoomOut_Click(object sender, EventArgs e)
        {
            // Decrease by 12.5% (minimum 12.5%), round to nearest 12.5%
            double newValue = currentScalingPercent - 12.5;
            if (newValue < 12.5) newValue = 12.5;
            // Round to nearest 12.5% step
            double rounded = Math.Round(newValue / 12.5) * 12.5;
            if (rounded < 12.5) rounded = 12.5;
            ApplyDisplayScaling(rounded);
        }

        private void ScalingZoomIn_Click(object sender, EventArgs e)
        {
            // Increase by 12.5% (maximum 200%), round to nearest 12.5%
            double newValue = currentScalingPercent + 12.5;
            if (newValue > 200) newValue = 200;
            // Round to nearest 12.5% step
            double rounded = Math.Round(newValue / 12.5) * 12.5;
            if (rounded > 200) rounded = 200;
            ApplyDisplayScaling(rounded);
        }

        private void ScalingPreset_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                double presetValue = Convert.ToDouble(btn.Tag);
                ApplyDisplayScaling(presetValue);
            }
        }

        private void ApplyDisplayScaling(double percent)
        {
            currentScalingPercent = percent;

            // Scaling percentage is relative to remote desktop resolution
            // 100% = window client area matches remote desktop pixels exactly
            // 50% = window is half the remote desktop size
            // 200% = window is double the remote desktop size
            double displayMultiplier = percent / 100.0;

            // Calculate the new client area size based on remote desktop dimensions
            int scaledWidth = (int)(kvmControl.DesktopWidth * displayMultiplier);
            int scaledHeight = (int)(kvmControl.DesktopHeight * displayMultiplier);

            // Account for title bar and status bar within client area
            int titleBarHeight = titleBarPanel.Height;
            int statusBarHeight = mainStatusStrip.Visible ? mainStatusStrip.Height : 0;

            // Calculate window size (client area + window chrome)
            int chromeWidth = this.Width - this.ClientSize.Width;
            int chromeHeight = this.Height - this.ClientSize.Height;

            int newWindowWidth = scaledWidth + chromeWidth;
            int newWindowHeight = scaledHeight + titleBarHeight + statusBarHeight + chromeHeight;

            // Save the pane's screen position before resizing (it must not move)
            Point paneScreenPos = Point.Empty;
            bool paneWasVisible = dropdownPane.Visible;
            if (paneWasVisible)
            {
                paneScreenPos = dropdownPane.PointToScreen(Point.Empty);
            }

            // The pane is centered under the title bar, so calculate where the window's
            // horizontal center should be (aligned with pane center)
            int paneCenterScreenX = paneScreenPos.X + dropdownPane.Width / 2;

            // Calculate new window position so its center aligns with the pane
            int newLeft = paneCenterScreenX - newWindowWidth / 2;
            // Keep the top edge relative to the pane position (pane is below title bar)
            int newTop = paneScreenPos.Y - titleBarHeight - chromeHeight / 2;

            this.SetBounds(newLeft, newTop, newWindowWidth, newWindowHeight);

            // Restore the pane to its exact screen position
            if (paneWasVisible)
            {
                Point newClientPos = this.PointToClient(paneScreenPos);
                dropdownPane.Location = newClientPos;
            }

            // Enable ZoomToFit so the remote desktop scales to fit the new window size
            resizeKvmControl.ZoomToFit = true;
            resizeKvmControl.CenterKvmControl(true);

            // Update the label if it exists
            if (scalingLevelLabel != null)
            {
                scalingLevelLabel.Text = FormatScalingPercent(currentScalingPercent);
            }

            // Update preset button states in place (no panel refresh needed)
            if (dropdownPane.Visible && dropdownPaneLabel.Text == "Display")
            {
                var ps = new DropdownPaneStyle();
                foreach (Button btn in scalingPresetButtons)
                {
                    if (btn.Tag != null)
                    {
                        double presetValue = Convert.ToDouble(btn.Tag);
                        bool isSelected = (Math.Abs(currentScalingPercent - presetValue) < 0.01);
                        btn.BackColor = isSelected ? ps.SelectedColor : ps.PaneBgColor;
                        btn.FlatAppearance.BorderColor = isSelected ? ps.SelectedBorderColor : ps.BorderColor;
                    }
                }
            }

            // Save to registry
            Settings.SetRegValue("kvmDisplayScaling", currentScalingPercent.ToString());
        }

        private void gearButton_Click(object sender, EventArgs e)
        {
            // If clicking the same pane that's already open, close it
            if (dropdownPane.Visible && dropdownPaneLabel.Text == "Settings")
            {
                HideDropdownPane();
                return;
            }

            // Hide any other open pane first
            if (dropdownPane.Visible)
            {
                HideDropdownPane();
            }

            // Set the title
            dropdownPaneLabel.Text = "Settings";

            // Clear existing content
            dropdownPaneContent.Controls.Clear();

            var ps = new DropdownPaneStyle();
            bool isDark = ThemeManager.Instance.IsDarkMode;

            // Layout constants matching DropdownPaneStyle patterns
            int itemWidth = 80;
            int itemHeight = 64;
            int itemSpacing = 8;
            int sidePadding = 12;
            int topPadding = 10;
            int paneWidth = (itemWidth * 4) + (itemSpacing * 3) + (sidePadding * 2);

            int yOffset = topPadding;

            // === Connection Group (at top) ===
            dropdownPaneContent.Controls.Add(ps.CreateSectionHeader("Connection", yOffset, DropdownPaneStyle.SectionHeaderHeight, paneWidth));
            yOffset += DropdownPaneStyle.SectionHeaderHeight;

            // Connect/Disconnect action button using SettingsActionButton
            bool isDisconnected = (this.state == 0);
            string disconnectText = isDisconnected ? Translate.T(Properties.Resources.Connect, lang) : Translate.T(Properties.Resources.Disconnect, lang);
            string disconnectIcon = isDisconnected ? "🔌" : "⏏";

            settingsConnectionButton = new SettingsActionButton();
            settingsConnectionButton.Icon = disconnectIcon;
            settingsConnectionButton.LabelText = disconnectText;
            settingsConnectionButton.Size = new Size(itemWidth, itemHeight);
            settingsConnectionButton.Location = new Point(sidePadding, yOffset);
            settingsConnectionButton.UpdateTheme(isDark);
            settingsConnectionButton.ButtonClick += (s, ev) => { MenuItemDisconnect_Click(s, ev); };
            dropdownPaneContent.Controls.Add(settingsConnectionButton);

            yOffset += itemHeight + itemSpacing + 4;

            // === Settings Group ===
            dropdownPaneContent.Controls.Add(ps.CreateSectionHeader("Settings", yOffset, DropdownPaneStyle.SectionHeaderHeight, paneWidth));
            yOffset += DropdownPaneStyle.SectionHeaderHeight;

            // Helper to create rounded rectangle region
            Func<int, int, int, System.Drawing.Region> createRoundedRegion = (width, height, radius) =>
            {
                System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                int diameter = radius * 2;
                path.AddArc(0, 0, diameter, diameter, 180, 90);
                path.AddArc(width - diameter, 0, diameter, diameter, 270, 90);
                path.AddArc(width - diameter, height - diameter, diameter, diameter, 0, 90);
                path.AddArc(0, height - diameter, diameter, diameter, 90, 90);
                path.CloseFigure();
                return new System.Drawing.Region(path);
            };

            int cornerRadius = 8;

            // Helper to create a toggle item panel with rounded corners
            Func<string, ToggleSwitch, int, int, Panel> createToggleItem = (labelText, toggle, x, y) =>
            {
                Panel itemPanel = new Panel();
                itemPanel.Size = new Size(itemWidth, itemHeight);
                itemPanel.Location = new Point(x, y);
                itemPanel.BackColor = isDark ? Color.FromArgb(55, 55, 55) : Color.FromArgb(235, 235, 235);
                itemPanel.Cursor = Cursors.Hand;
                itemPanel.Region = createRoundedRegion(itemWidth, itemHeight, cornerRadius);

                // Toggle switch centered near top
                toggle.Size = new Size(36, 18);
                toggle.Location = new Point((itemWidth - 36) / 2, 12);
                toggle.OffColor = isDark ? Color.FromArgb(90, 90, 90) : Color.FromArgb(180, 180, 180);
                toggle.OnColor = Color.FromArgb(76, 175, 80);
                toggle.ThumbColor = isDark ? Color.FromArgb(235, 235, 235) : Color.White;
                toggle.BackColor = itemPanel.BackColor;
                itemPanel.Controls.Add(toggle);

                // Label below toggle
                Label lbl = new Label();
                lbl.Text = labelText;
                lbl.Font = new Font("Segoe UI", 8F);
                lbl.ForeColor = ps.PaneTextColor;
                lbl.BackColor = Color.Transparent;
                lbl.Size = new Size(itemWidth - 4, 28);
                lbl.Location = new Point(2, 34);
                lbl.TextAlign = ContentAlignment.TopCenter;
                itemPanel.Controls.Add(lbl);

                // Click anywhere on panel toggles the switch
                itemPanel.Click += (s, ev) => { toggle.Checked = !toggle.Checked; };
                lbl.Click += (s, ev) => { toggle.Checked = !toggle.Checked; };

                return itemPanel;
            };

            // Row 1: Status Bar, Auto Reconnect, Swap Mouse, Remote Key Map
            dropdownPaneContent.Controls.Add(createToggleItem("Status Bar", paneStatusBarToggleSwitch, sidePadding, yOffset));
            dropdownPaneContent.Controls.Add(createToggleItem("Auto\nReconnect", paneAutoReconnectToggleSwitch, sidePadding + itemWidth + itemSpacing, yOffset));
            dropdownPaneContent.Controls.Add(createToggleItem("Swap\nMouse", paneSwapMouseToggleSwitch, sidePadding + (itemWidth + itemSpacing) * 2, yOffset));
            dropdownPaneContent.Controls.Add(createToggleItem("Remote\nKey Map", paneRemoteKeyMapToggleSwitch, sidePadding + (itemWidth + itemSpacing) * 3, yOffset));

            yOffset += itemHeight + itemSpacing;

            // Row 2: Sync Clipboard
            dropdownPaneContent.Controls.Add(createToggleItem("Sync\nClipboard", paneSyncClipboardToggleSwitch, sidePadding, yOffset));

            yOffset += itemHeight + topPadding;

            // Finalize and show the dropdown pane (same pattern as other panes)
            ps.FinalizePane(dropdownPane, dropdownPaneLabel, dropdownPaneContent,
                yOffset, this.Width, titleBarPanel.Bottom, paneWidth);
        }

        private void ShowDropdownPane(string title, params Control[] contentControls)
        {
            // If clicking the same pane that's already open, close it
            if (dropdownPane.Visible && dropdownPaneLabel.Text == title)
            {
                HideDropdownPane();
                return;
            }

            // Set the title
            dropdownPaneLabel.Text = title;

            // Clear existing content and add new controls
            dropdownPaneContent.Controls.Clear();
            foreach (var control in contentControls)
            {
                dropdownPaneContent.Controls.Add(control);
            }

            // Calculate pane height based on content
            int contentHeight = contentControls.Length * 32 + 8;
            dropdownPane.Size = new Size(150, 28 + contentHeight);
            dropdownPaneContent.Size = new Size(148, contentHeight);

            // Position the dropdown centered under the center panel area
            int centerX = (this.Width - dropdownPane.Width) / 2;
            int paneY = titleBarPanel.Bottom;
            dropdownPane.Location = new Point(centerX, paneY);

            // Show and bring to front
            dropdownPane.Visible = true;
            dropdownPane.BringToFront();

            // Apply theme to the pane
            UpdateDropdownPaneTheme();
        }

        private void ShowDropdownPane(string title, params DropdownSection[] sections)
        {
            // If clicking the same pane that's already open, close it
            if (dropdownPane.Visible && dropdownPaneLabel.Text == title)
            {
                HideDropdownPane();
                return;
            }

            // Set the title
            dropdownPaneLabel.Text = title;

            // Clear existing content
            dropdownPaneContent.Controls.Clear();

            var ps = new DropdownPaneStyle();
            int yOffset = 0;

            foreach (var section in sections)
            {
                // Add section header
                Panel sectionHeader = new Panel();
                sectionHeader.Location = new Point(0, yOffset);
                sectionHeader.Size = new Size(DropdownPaneStyle.PaneWidth - 2, DropdownPaneStyle.SectionHeaderHeight);
                sectionHeader.BackColor = Color.Transparent;

                Label sectionLabel = new Label();
                sectionLabel.Text = section.Title;
                sectionLabel.Font = DropdownPaneStyle.SectionHeaderFont;
                sectionLabel.ForeColor = ps.LabelColor;
                sectionLabel.Location = new Point(8, 4);
                sectionLabel.AutoSize = true;
                sectionHeader.Controls.Add(sectionLabel);

                // Add info icon if present
                if (!string.IsNullOrEmpty(section.InfoIcon))
                {
                    Label infoLabel = new Label();
                    infoLabel.Text = section.InfoIcon;
                    infoLabel.Font = DropdownPaneStyle.SmallFont;
                    infoLabel.ForeColor = ps.LabelColor;
                    infoLabel.AutoSize = true;
                    infoLabel.Location = new Point(DropdownPaneStyle.PaneWidth - 25, 4);
                    sectionHeader.Controls.Add(infoLabel);
                }

                dropdownPaneContent.Controls.Add(sectionHeader);
                yOffset += DropdownPaneStyle.SectionHeaderHeight;

                // Add items in a row layout for this section
                int itemsPerRow = section.Items.Count <= 4 ? section.Items.Count : 4;
                int itemWidth = (DropdownPaneStyle.PaneWidth - 16) / itemsPerRow;
                int xOffset = 4;
                int itemsInCurrentRow = 0;

                foreach (var item in section.Items)
                {
                    Button itemButton = new Button();
                    ps.ApplyFlatButtonStyle(itemButton, item.IsSelected);
                    itemButton.FlatAppearance.BorderSize = item.IsSelected ? 1 : 0;
                    itemButton.Font = DropdownPaneStyle.ItemFont;
                    itemButton.Location = new Point(xOffset, yOffset);
                    itemButton.Size = new Size(itemWidth, DropdownPaneStyle.ItemHeight);
                    itemButton.TextAlign = ContentAlignment.MiddleCenter;
                    itemButton.Tag = item.Tag;

                    // Set text with icon if present
                    if (!string.IsNullOrEmpty(item.Icon))
                    {
                        itemButton.Text = item.Icon + "\n" + item.Label;
                    }
                    else
                    {
                        itemButton.Text = item.Label;
                    }

                    if (item.ClickHandler != null)
                    {
                        itemButton.Click += item.ClickHandler;
                    }

                    dropdownPaneContent.Controls.Add(itemButton);

                    xOffset += itemWidth;
                    itemsInCurrentRow++;

                    if (itemsInCurrentRow >= itemsPerRow)
                    {
                        xOffset = 4;
                        yOffset += DropdownPaneStyle.ItemHeight + 2;
                        itemsInCurrentRow = 0;
                    }
                }

                // If we didn't complete a row, move to next line
                if (itemsInCurrentRow > 0)
                {
                    yOffset += DropdownPaneStyle.ItemHeight + 2;
                }

                yOffset += DropdownPaneStyle.SectionSpacing;
            }

            // Size and show the dropdown pane
            ps.FinalizePane(dropdownPane, dropdownPaneLabel, dropdownPaneContent,
                           yOffset, this.Width, titleBarPanel.Bottom);
        }

        /// <summary>
        /// Shows a dropdown pane with a grid-based layout containing groups of sections
        /// </summary>
        private void ShowDropdownPane(string title, DropdownPaneLayout layout, params DropdownGroup[] groups)
        {
            // If clicking the same pane that's already open, close it
            if (dropdownPane.Visible && dropdownPaneLabel.Text == title)
            {
                HideDropdownPane();
                return;
            }

            // Set the title
            dropdownPaneLabel.Text = title;

            // Clear existing content
            dropdownPaneContent.Controls.Clear();

            var ps = new DropdownPaneStyle();

            // Calculate pane dimensions based on layout
            int paneWidth = ps.CalculateGridPaneWidth(layout);
            int groupWidth = layout.CalculateGroupWidth(paneWidth);

            // Track row heights for proper grid placement
            int[] rowHeights = new int[layout.RowsPerPane];
            for (int i = 0; i < rowHeights.Length; i++) rowHeights[i] = 0;

            // Place each group in the grid (left-to-right, top-to-bottom reading order)
            for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
            {
                var group = groups[groupIndex];
                int col = groupIndex % layout.ColumnsPerPane;
                int row = groupIndex / layout.ColumnsPerPane;

                if (row >= layout.RowsPerPane) break; // Exceeded max rows

                // Calculate group position
                int groupX = 4 + col * (groupWidth + DropdownPaneStyle.GroupSpacing);
                int groupY = 0;
                for (int r = 0; r < row; r++) groupY += rowHeights[r] + DropdownPaneStyle.GroupSpacing;

                int yOffsetInGroup = 0;

                // Render each section within the group
                foreach (var section in group.Sections)
                {
                    // Add section header (skip if title is empty)
                    if (!string.IsNullOrEmpty(section.Title))
                    {
                        Panel sectionHeader = new Panel();
                        sectionHeader.Location = new Point(groupX, groupY + yOffsetInGroup);
                        sectionHeader.Size = new Size(groupWidth, DropdownPaneStyle.SectionHeaderHeight);
                        sectionHeader.BackColor = Color.Transparent;

                        Label sectionLabel = new Label();
                        sectionLabel.Text = section.Title;
                        sectionLabel.Font = DropdownPaneStyle.SectionHeaderFont;
                        sectionLabel.ForeColor = ps.LabelColor;
                        sectionLabel.Location = new Point(4, 4);
                        sectionLabel.AutoSize = true;
                        sectionHeader.Controls.Add(sectionLabel);

                        // Add info icon if present
                        if (!string.IsNullOrEmpty(section.InfoIcon))
                        {
                            Label infoLabel = new Label();
                            infoLabel.Text = section.InfoIcon;
                            infoLabel.Font = DropdownPaneStyle.SmallFont;
                            infoLabel.ForeColor = ps.LabelColor;
                            infoLabel.AutoSize = true;
                            infoLabel.Location = new Point(groupWidth - 20, 4);
                            sectionHeader.Controls.Add(infoLabel);
                        }

                        dropdownPaneContent.Controls.Add(sectionHeader);
                        yOffsetInGroup += DropdownPaneStyle.SectionHeaderHeight;
                    }

                    // Add items in rows based on layout configuration
                    int itemsPerRow = Math.Min(layout.ItemsPerGroupRow, section.Items.Count);
                    int itemWidth = (groupWidth - 8) / itemsPerRow;
                    int xOffsetInGroup = 4;
                    int itemsInCurrentRow = 0;

                    foreach (var item in section.Items)
                    {
                        Button itemButton = new Button();
                        ps.ApplyFlatButtonStyle(itemButton, item.IsSelected);
                        itemButton.Font = DropdownPaneStyle.ItemFont;
                        itemButton.Location = new Point(groupX + xOffsetInGroup, groupY + yOffsetInGroup);
                        itemButton.Size = new Size(itemWidth, DropdownPaneStyle.ItemHeight);
                        itemButton.TextAlign = ContentAlignment.MiddleLeft;
                        itemButton.TextImageRelation = TextImageRelation.ImageBeforeText;
                        itemButton.Tag = item.Tag;

                        // Set text with icon based on IconPosition
                        if (!string.IsNullOrEmpty(item.Icon))
                        {
                            switch (item.IconPosition)
                            {
                                case IconPosition.Left:
                                    itemButton.Text = "  " + item.Icon + " " + item.Label;
                                    break;
                                case IconPosition.Right:
                                    itemButton.Text = "  " + item.Label + " " + item.Icon;
                                    break;
                                case IconPosition.None:
                                default:
                                    itemButton.Text = "  " + item.Label;
                                    break;
                            }
                        }
                        else
                        {
                            itemButton.Text = "  " + item.Label;
                        }

                        if (item.ClickHandler != null)
                        {
                            itemButton.Click += item.ClickHandler;
                        }

                        dropdownPaneContent.Controls.Add(itemButton);

                        // Add helper tooltip control if present - as a child of the button
                        if (item.HasHelperTooltip && item.HelperControl != null)
                        {
                            // Position at the button's actual top right corner using button's real width
                            item.HelperControl.Location = new Point(itemButton.Width - item.HelperControl.Width - 1, 2);
                            item.HelperControl.BringToFront();
                            itemButton.Controls.Add(item.HelperControl);
                        }

                        xOffsetInGroup += itemWidth;
                        itemsInCurrentRow++;

                        if (itemsInCurrentRow >= itemsPerRow)
                        {
                            xOffsetInGroup = 4;
                            yOffsetInGroup += DropdownPaneStyle.ItemHeight + 2;
                            itemsInCurrentRow = 0;
                        }
                    }

                    // If we didn't complete a row, move to next line
                    if (itemsInCurrentRow > 0)
                    {
                        yOffsetInGroup += DropdownPaneStyle.ItemHeight + 2;
                    }

                    yOffsetInGroup += DropdownPaneStyle.SectionSpacing;
                }

                // Update the row height for this group
                if (yOffsetInGroup > rowHeights[row])
                {
                    rowHeights[row] = yOffsetInGroup;
                }
            }

            // Calculate total pane height based on all row heights
            int contentHeight = 0;
            for (int r = 0; r < layout.RowsPerPane; r++)
            {
                contentHeight += rowHeights[r];
                if (r < layout.RowsPerPane - 1) contentHeight += DropdownPaneStyle.GroupSpacing;
            }

            // Size and show the dropdown pane
            ps.FinalizePane(dropdownPane, dropdownPaneLabel, dropdownPaneContent,
                           contentHeight, this.Width, titleBarPanel.Bottom, paneWidth);
        }

        private void HideDropdownPane()
        {
            dropdownPane.Visible = false;
        }

        // Auto-close dropdown pane when clicking on the KVM viewer area
        private void KvmControl_MouseDown_ClosePane(object sender, MouseEventArgs e)
        {
            if (dropdownPane.Visible)
            {
                HideDropdownPane();
            }
        }

        // Keep old method name for compatibility
        private void HideSettingsFlyout()
        {
            HideDropdownPane();
        }

        private void minimizeButton_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void TitleBarPanel_Resize(object sender, EventArgs e)
        {
            PositionTitleBarButtons();
            CenterTitleBarControls();
        }

        private void maximizeButton_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
                resizeKvmControl.CenterKvmControl(true);
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
            UpdateMaximizeButtonIcon();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void statusBarToggleSwitch_CheckedChanged(object sender, EventArgs e)
        {
            // Get checked state from the appropriate control
            bool isChecked = (sender is SettingsToggleItem item) ? item.Checked : paneStatusBarToggleSwitch.Checked;
            mainStatusStrip.Visible = isChecked;
            Settings.SetRegValue("kvmStatusBarVisible", isChecked ? "1" : "0");
            // Keep pane toggle in sync
            if (paneStatusBarToggleSwitch.Checked != isChecked) paneStatusBarToggleSwitch.Checked = isChecked;
        }

        private void autoReconnectToggleSwitch_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = (sender is SettingsToggleItem item) ? item.Checked : paneAutoReconnectToggleSwitch.Checked;
            kvmControl.AutoReconnect = isChecked;
            Settings.SetRegValue("kvmAutoReconnect", isChecked ? "1" : "0");
            if (paneAutoReconnectToggleSwitch.Checked != isChecked) paneAutoReconnectToggleSwitch.Checked = isChecked;
        }

        private void swapMouseToggleSwitch_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = (sender is SettingsToggleItem item) ? item.Checked : paneSwapMouseToggleSwitch.Checked;
            kvmControl.SwamMouseButtons = isChecked;
            Settings.SetRegValue("kvmSwamMouseButtons", isChecked ? "1" : "0");
            if (paneSwapMouseToggleSwitch.Checked != isChecked) paneSwapMouseToggleSwitch.Checked = isChecked;
        }

        private void remoteKeyMapToggleSwitch_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = (sender is SettingsToggleItem item) ? item.Checked : paneRemoteKeyMapToggleSwitch.Checked;
            kvmControl.RemoteKeyboardMap = isChecked;
            Settings.SetRegValue("kvmRemoteKeyboardMap", isChecked ? "1" : "0");
            if (paneRemoteKeyMapToggleSwitch.Checked != isChecked) paneRemoteKeyMapToggleSwitch.Checked = isChecked;
        }

        private void syncClipboardToggleSwitch_CheckedChanged(object sender, EventArgs e)
        {
            bool isChecked = (sender is SettingsToggleItem item) ? item.Checked : paneSyncClipboardToggleSwitch.Checked;
            kvmControl.AutoSendClipboard = isChecked;
            Settings.SetRegValue("kvmAutoClipboard", isChecked ? "1" : "0");
            if (paneSyncClipboardToggleSwitch.Checked != isChecked) paneSyncClipboardToggleSwitch.Checked = isChecked;
            if (isChecked) { Parent_ClipboardChanged(); }
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            UpdateTheme();
        }

        private void UpdateTheme()
        {
            ThemeManager theme = ThemeManager.Instance;
            Color titleBarColor = theme.GetTitleBarColor();
            Color titleBarTextColor = theme.GetTitleBarTextColor();

            // Update title bar
            titleBarPanel.BackColor = titleBarColor;
            titleLabel.ForeColor = titleBarTextColor;

            // Update center panel color (darker/lighter shade for contrast)
            if (theme.IsDarkMode)
            {
                titleBarPanel.CenterColor = Color.FromArgb(65, 65, 65); // Slightly lighter shade for better contrast in dark mode
            }
            else
            {
                titleBarPanel.CenterColor = Color.FromArgb(200, 200, 200); // Slightly darker in light mode
            }

            // Update title bar buttons
            themeButton.BackColor = titleBarColor;
            themeButton.ForeColor = titleBarTextColor;
            themeButton.FlatAppearance.MouseOverBackColor = theme.GetButtonHoverColor();
            minimizeButton.BackColor = titleBarColor;
            minimizeButton.ForeColor = titleBarTextColor;
            minimizeButton.FlatAppearance.MouseOverBackColor = theme.GetButtonHoverColor();
            maximizeButton.BackColor = titleBarColor;
            maximizeButton.ForeColor = titleBarTextColor;
            maximizeButton.FlatAppearance.MouseOverBackColor = theme.GetButtonHoverColor();
            closeButton.BackColor = titleBarColor;
            closeButton.ForeColor = titleBarTextColor;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);

            // Update chat button in title bar (icon button style, same as gear/display/info buttons)
            chatButton.BackColor = titleBarColor;
            chatButton.ForeColor = titleBarTextColor;
            chatButton.FlatAppearance.MouseOverBackColor = theme.GetButtonHoverColor();
            chatButton.Image = GetTintedIcon(Properties.Resources.Chat20, titleBarTextColor);
            chatButton.Text = "";

            // Update files button in title bar (icon button style, same as gear/display/info buttons)
            openRemoteFilesButton.BackColor = titleBarColor;
            openRemoteFilesButton.ForeColor = titleBarTextColor;
            openRemoteFilesButton.FlatAppearance.MouseOverBackColor = theme.GetButtonHoverColor();
            openRemoteFilesButton.Image = GetTintedIcon(Properties.Resources.Files20, titleBarTextColor);
            openRemoteFilesButton.Text = "";

            // Update connect button in title bar (same style as chat)
            connectButton.FillColor = theme.IsDarkMode ? Color.FromArgb(65, 65, 65) : Color.FromArgb(200, 200, 200);
            connectButton.ForeColor = titleBarTextColor;

            // Update chat separator color
            chatSeparator.BackColor = theme.IsDarkMode ? Color.FromArgb(100, 100, 100) : Color.FromArgb(180, 180, 180);

            // Update dropdown pane
            UpdateDropdownPaneTheme();

            // Update gear button in title bar (centered)
            gearButton.BackColor = titleBarColor;
            gearButton.ForeColor = titleBarTextColor;
            gearButton.FlatAppearance.MouseOverBackColor = theme.GetButtonHoverColor();
            // Use a tinted Material icon so it stays readable in both themes
            gearButton.Image = GetTintedIcon(Properties.Resources.Gear20, titleBarTextColor);
            gearButton.Text = "";

            // Update display button in title bar (to the left of gear button)
            displayButton.BackColor = titleBarColor;
            displayButton.ForeColor = titleBarTextColor;
            displayButton.FlatAppearance.MouseOverBackColor = theme.GetButtonHoverColor();
            displayButton.Image = GetTintedIcon(Properties.Resources.Display20, titleBarTextColor);
            displayButton.Text = "";

            // Update other button in title bar (to the right of display button)
            otherButton.BackColor = titleBarColor;
            otherButton.ForeColor = titleBarTextColor;
            otherButton.FlatAppearance.MouseOverBackColor = theme.GetButtonHoverColor();
            otherButton.Image = GetTintedIcon(Properties.Resources.Wrench20, titleBarTextColor);
            otherButton.Text = "";

            // Update info button in title bar (to the right of gear button)
            infoButton.BackColor = titleBarColor;
            infoButton.ForeColor = titleBarTextColor;
            infoButton.FlatAppearance.MouseOverBackColor = theme.GetButtonHoverColor();
            infoButton.Image = GetTintedIcon(Properties.Resources.Statistics20, titleBarTextColor);
            infoButton.Text = "";

            // Update theme button icon based on current theme (use Material Design icons)
            if (theme.IsDarkMode)
            {
                themeButton.Image = GetTintedIcon(Properties.Resources.SunDark20, titleBarTextColor);
            }
            else
            {
                themeButton.Image = GetTintedIcon(Properties.Resources.MoonDark20, titleBarTextColor);
            }
            themeButton.Text = "";

            // Update zoom button icon with theme-aware tinting
            zoomButton.BackColor = titleBarColor;
            zoomButton.FlatAppearance.MouseOverBackColor = theme.GetButtonHoverColor();
            zoomButton.Image = GetTintedIcon(Properties.Resources.ZoomToFit, titleBarTextColor);
        }

        private void UpdateDropdownPaneTheme()
        {
            var ps = new DropdownPaneStyle();

            // Update dropdown pane container
            ps.ApplyPaneTheme(dropdownPane, dropdownPaneLabel, dropdownPaneContent);

            // Update settings pane buttons with theme-aware tinted icons
            settingsPaneSettingsButton.BackColor = ps.PaneBgColor;
            settingsPaneSettingsButton.ForeColor = ps.PaneTextColor;
            settingsPaneSettingsButton.Image = GetTintedIcon(Properties.Resources.Gear20, ps.PaneTextColor);
            settingsPaneSettingsButton.FlatAppearance.MouseOverBackColor = ps.PaneHoverColor;

            settingsPaneStatsButton.BackColor = ps.PaneBgColor;
            settingsPaneStatsButton.ForeColor = ps.PaneTextColor;
            settingsPaneStatsButton.Image = GetTintedIcon(Properties.Resources.Statistics20, ps.PaneTextColor);
            settingsPaneStatsButton.FlatAppearance.MouseOverBackColor = ps.PaneHoverColor;

            // Update settings pane toggle items and action button with new theme
            bool isDark = ThemeManager.Instance.IsDarkMode;
            settingsConnectionButton?.UpdateTheme(isDark);
            settingsStatusBarItem?.UpdateTheme(isDark);
            settingsAutoReconnectItem?.UpdateTheme(isDark);
            settingsSwapMouseItem?.UpdateTheme(isDark);
            settingsRemoteKeyMapItem?.UpdateTheme(isDark);
            settingsSyncClipboardItem?.UpdateTheme(isDark);
        }

        /* ===== USAGE EXAMPLE FOR GRID-BASED DROPDOWN PANE =====
         * 
         * The modularized dropdown pane system allows you to organize items into a grid layout
         * with configurable rows, columns, and grouping.
         * 
         * Example: Creating a 2x2 grid with 3 items per row in each group
         * 
         * // 1. Define the layout (2 columns, 2 rows, 3 items per row in headers)
         * var layout = new DropdownPaneLayout(
         *     columnsPerPane: 2,  // Number of group columns in the pane
         *     rowsPerPane: 2,     // Number of group rows in the pane
         *     itemsPerGroupRow: 3 // Items per row within each group
         * );
         * 
         * // 2. Create sections with items
         * var displaySection = new DropdownSection("Display", "ℹ")
         *     .AddItem("🖥️", "Fullscreen", (s, e) => { /* handler * / })
         *     .AddItem("↔️", "Stretch", (s, e) => { /* handler * / })
         *     .AddItem("⊞", "Fit", (s, e) => { /* handler * / });
         * 
         * var inputSection = new DropdownSection("Input")
         *     .AddItem("⌨️", "Keyboard", (s, e) => { /* handler * / })
         *     .AddItem("🖱️", "Mouse", (s, e) => { /* handler * / });
         * 
         * // 3. Create groups (each group occupies one grid cell)
         * var group1 = new DropdownGroup(displaySection);
         * var group2 = new DropdownGroup(inputSection);
         * var group3 = new DropdownGroup(new DropdownSection("Network")
         *     .AddItem("📊", "Stats", (s, e) => { /* handler * / }));
         * var group4 = new DropdownGroup(new DropdownSection("Settings")
         *     .AddItem("⚙️", "Options", (s, e) => { /* handler * / }));
         * 
         * // 4. Show the dropdown pane
         * ShowDropdownPane("Advanced Settings", layout, group1, group2, group3, group4);
         * 
         * // Advanced features:
         * 
         * // Setting icon position
         * var item = new DropdownItem("🎯", "Target", clickHandler);
         * item.IconPosition = IconPosition.Right; // Icon appears to the right
         * 
         * // Adding helper tooltip control
         * var itemWithHelp = new DropdownItem("Option", clickHandler);
         * itemWithHelp.HasHelperTooltip = true;
         * itemWithHelp.HelperControl = new Label() { Text = "?", Width = 20 };
         * 
         * // Marking items as selected
         * var selectedItem = new DropdownItem("Active", clickHandler);
         * selectedItem.IsSelected = true; // Shows with blue border
         * 
         * // Multiple sections in one group
         * var multiSectionGroup = new DropdownGroup()
         *     .AddSection(new DropdownSection("Section 1")
         *         .AddItem("Item A", null)
         *         .AddItem("Item B", null))
         *     .AddSection(new DropdownSection("Section 2")
         *         .AddItem("Item C", null));
         * 
         * ===== END USAGE EXAMPLE =====
         */
    }
}
