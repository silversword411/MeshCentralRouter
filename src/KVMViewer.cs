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

        // Title bar dragging support
        private bool isDragging = false;
        private Point dragOffset;

        // Stats
        public long bytesIn = 0;
        public long bytesInCompressed = 0;
        public long bytesOut = 0;
        public long bytesOutCompressed = 0;

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
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
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
            CenterTitleBarControls();
            PositionTitleBarButtons();
        }

        private void CenterTitleBarControls()
        {
            // Center the gear and info buttons in the title bar (with small spacing between them)
            int spacing = 4;
            int totalWidth = gearButton.Width + spacing + infoButton.Width;
            int startX = (titleBarPanel.Width - totalWidth) / 2;
            gearButton.Location = new Point(startX, gearButton.Location.Y);
            infoButton.Location = new Point(startX + gearButton.Width + spacing, infoButton.Location.Y);

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
            int separatorSpacing = 8; // Spacing around the separator
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

            // Vertical separator line (80% of title bar height, centered vertically)
            int separatorHeight = (int)(titleBarPanel.Height * 0.8);
            int separatorY = (titleBarPanel.Height - separatorHeight) / 2;
            x -= separatorSpacing;
            chatSeparator.Size = new Size(1, separatorHeight);
            chatSeparator.Location = new Point(x, separatorY);

            // Chat button with spacing after separator (centered vertically)
            x -= separatorSpacing + chatButton.Width;
            int chatY = (titleBarPanel.Height - chatButton.Height) / 2;
            chatButton.Location = new Point(x, chatY);

            // Files button to the left of chat (with small spacing)
            x -= spacing + openRemoteFilesButton.Width;
            int filesY = (titleBarPanel.Height - openRemoteFilesButton.Height) / 2;
            openRemoteFilesButton.Location = new Point(x, filesY);
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

            ThemeManager theme = ThemeManager.Instance;
            Color paneBgColor = theme.IsDarkMode ? Color.FromArgb(45, 45, 45) : Color.FromArgb(250, 250, 250);
            Color paneTextColor = theme.IsDarkMode ? Color.White : Color.Black;
            Color labelColor = theme.IsDarkMode ? Color.FromArgb(180, 180, 180) : Color.FromArgb(100, 100, 100);

            int yOffset = 4;
            int rowHeight = 20;
            int paneWidth = 220;
            int labelWidth = 90;
            int valueWidth = paneWidth - labelWidth - 16;

            // Helper function to create a stats row
            Action<string, Label> addStatsRow = (labelText, valueLabel) =>
            {
                Label lbl = new Label();
                lbl.Text = labelText;
                lbl.Font = new Font("Segoe UI", 8.5F);
                lbl.ForeColor = labelColor;
                lbl.Location = new Point(8, yOffset);
                lbl.Size = new Size(labelWidth, rowHeight);
                lbl.TextAlign = ContentAlignment.MiddleLeft;
                dropdownPaneContent.Controls.Add(lbl);

                valueLabel.Font = new Font("Segoe UI", 8.5F);
                valueLabel.ForeColor = paneTextColor;
                valueLabel.Location = new Point(labelWidth + 8, yOffset);
                valueLabel.Size = new Size(valueWidth, rowHeight);
                valueLabel.TextAlign = ContentAlignment.MiddleRight;
                dropdownPaneContent.Controls.Add(valueLabel);

                yOffset += rowHeight + 2;
            };

            // Data Transfer section header
            Label dataHeader = new Label();
            dataHeader.Text = "Data Transfer";
            dataHeader.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dataHeader.ForeColor = paneTextColor;
            dataHeader.Location = new Point(8, yOffset);
            dataHeader.Size = new Size(paneWidth - 16, rowHeight);
            dropdownPaneContent.Controls.Add(dataHeader);
            yOffset += rowHeight + 4;

            // Create value labels
            statsBytesInValueLabel = new Label();
            statsBytesOutValueLabel = new Label();
            statsCompInValueLabel = new Label();
            statsCompOutValueLabel = new Label();
            statsInRatioValueLabel = new Label();
            statsOutRatioValueLabel = new Label();

            addStatsRow("Bytes In:", statsBytesInValueLabel);
            addStatsRow("Bytes Out:", statsBytesOutValueLabel);

            yOffset += 6; // Add spacing before compression section

            // Compression section header
            Label compHeader = new Label();
            compHeader.Text = "Compression";
            compHeader.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            compHeader.ForeColor = paneTextColor;
            compHeader.Location = new Point(8, yOffset);
            compHeader.Size = new Size(paneWidth - 16, rowHeight);
            dropdownPaneContent.Controls.Add(compHeader);
            yOffset += rowHeight + 4;

            addStatsRow("Compressed In:", statsCompInValueLabel);
            addStatsRow("Compressed Out:", statsCompOutValueLabel);
            addStatsRow("In Ratio:", statsInRatioValueLabel);
            addStatsRow("Out Ratio:", statsOutRatioValueLabel);

            yOffset += 4;

            // Calculate pane size
            int contentHeight = yOffset;
            dropdownPane.Size = new Size(paneWidth, 28 + contentHeight);
            dropdownPaneContent.Size = new Size(paneWidth - 2, contentHeight);

            // Position the dropdown centered under the info button
            int centerX = (this.Width - dropdownPane.Width) / 2;
            int paneY = titleBarPanel.Bottom;
            dropdownPane.Location = new Point(centerX, paneY);

            // Show and bring to front
            dropdownPane.Visible = true;
            dropdownPane.BringToFront();

            // Apply theme to the container
            dropdownPane.BackColor = paneBgColor;
            dropdownPaneLabel.ForeColor = paneTextColor;
            dropdownPaneContent.BackColor = paneBgColor;

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

        private void gearButton_Click(object sender, EventArgs e)
        {
            // If clicking the same pane that's already open, close it
            if (dropdownPane.Visible && dropdownPaneLabel.Text == "Settings")
            {
                HideDropdownPane();
                return;
            }

            // Set the title
            dropdownPaneLabel.Text = "Settings";

            // Clear existing content
            dropdownPaneContent.Controls.Clear();

            ThemeManager theme = ThemeManager.Instance;
            Color paneBgColor = theme.IsDarkMode ? Color.FromArgb(45, 45, 45) : Color.FromArgb(250, 250, 250);
            Color paneTextColor = theme.IsDarkMode ? Color.White : Color.Black;
            Color paneHoverColor = theme.IsDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230);
            Color borderColor = theme.IsDarkMode ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200);

            int yOffset = 4;
            int itemHeight = 28;
            int paneWidth = 180;

            // First row: View Settings button
            Button settingsBtn = new Button();
            settingsBtn.FlatStyle = FlatStyle.Flat;
            settingsBtn.FlatAppearance.BorderSize = 1;
            settingsBtn.FlatAppearance.BorderColor = borderColor;
            settingsBtn.Font = new Font("Segoe UI", 8.5F);
            settingsBtn.ForeColor = paneTextColor;
            settingsBtn.BackColor = paneBgColor;
            settingsBtn.FlatAppearance.MouseOverBackColor = paneHoverColor;
            settingsBtn.Location = new Point(4, yOffset);
            settingsBtn.Size = new Size(paneWidth - 10, itemHeight);
            settingsBtn.TextAlign = ContentAlignment.MiddleLeft;
            settingsBtn.Image = GetTintedIcon(Properties.Resources.Gear20, paneTextColor);
            settingsBtn.ImageAlign = ContentAlignment.MiddleLeft;
            settingsBtn.Text = "     View Settings";
            settingsBtn.Click += settingsToolStripMenuItem_Click;
            dropdownPaneContent.Controls.Add(settingsBtn);
            yOffset += itemHeight + 4;

            // Second row: Status Bar toggle with label and switch
            Panel statusBarRow = new Panel();
            statusBarRow.Location = new Point(4, yOffset);
            statusBarRow.Size = new Size(paneWidth - 10, itemHeight);
            statusBarRow.BackColor = paneBgColor;

            Label statusBarLabel = new Label();
            statusBarLabel.Text = "Status Bar";
            statusBarLabel.Font = new Font("Segoe UI", 8.5F);
            statusBarLabel.ForeColor = paneTextColor;
            statusBarLabel.Location = new Point(4, 6);
            statusBarLabel.AutoSize = true;
            statusBarRow.Controls.Add(statusBarLabel);

            // Position the toggle switch on the right side of the row
            paneStatusBarToggleSwitch.Location = new Point(paneWidth - 60, 4);
            paneStatusBarToggleSwitch.Size = new Size(40, 20);
            // Update toggle colors based on theme
            if (theme.IsDarkMode)
            {
                paneStatusBarToggleSwitch.OffColor = Color.FromArgb(90, 90, 90);
                paneStatusBarToggleSwitch.OnColor = Color.FromArgb(76, 175, 80);
                paneStatusBarToggleSwitch.ThumbColor = Color.FromArgb(235, 235, 235);
                paneStatusBarToggleSwitch.BackColor = paneBgColor;
            }
            else
            {
                paneStatusBarToggleSwitch.OffColor = Color.LightGray;
                paneStatusBarToggleSwitch.OnColor = Color.FromArgb(76, 175, 80);
                paneStatusBarToggleSwitch.ThumbColor = Color.White;
                paneStatusBarToggleSwitch.BackColor = paneBgColor;
            }
            statusBarRow.Controls.Add(paneStatusBarToggleSwitch);

            dropdownPaneContent.Controls.Add(statusBarRow);
            yOffset += itemHeight + 4;

            // Calculate pane size
            int contentHeight = yOffset;
            dropdownPane.Size = new Size(paneWidth, 28 + contentHeight);
            dropdownPaneContent.Size = new Size(paneWidth - 2, contentHeight);

            // Position the dropdown centered under the gear button
            int centerX = (this.Width - dropdownPane.Width) / 2;
            int paneY = titleBarPanel.Bottom;
            dropdownPane.Location = new Point(centerX, paneY);

            // Show and bring to front
            dropdownPane.Visible = true;
            dropdownPane.BringToFront();

            // Apply theme to the container
            dropdownPane.BackColor = paneBgColor;
            dropdownPaneLabel.ForeColor = paneTextColor;
            dropdownPaneContent.BackColor = paneBgColor;
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

            ThemeManager theme = ThemeManager.Instance;
            Color paneBgColor = theme.IsDarkMode ? Color.FromArgb(45, 45, 45) : Color.FromArgb(250, 250, 250);
            Color paneTextColor = theme.IsDarkMode ? Color.White : Color.Black;
            Color sectionHeaderColor = theme.IsDarkMode ? Color.FromArgb(180, 180, 180) : Color.FromArgb(100, 100, 100);
            Color paneHoverColor = theme.IsDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230);
            Color selectedColor = theme.IsDarkMode ? Color.FromArgb(70, 130, 180) : Color.FromArgb(200, 220, 240);

            int yOffset = 0;
            int itemHeight = 28;
            int sectionHeaderHeight = 22;
            int sectionSpacing = 8;
            int paneWidth = 180;

            foreach (var section in sections)
            {
                // Add section header
                Panel sectionHeader = new Panel();
                sectionHeader.Location = new Point(0, yOffset);
                sectionHeader.Size = new Size(paneWidth - 2, sectionHeaderHeight);
                sectionHeader.BackColor = Color.Transparent;

                Label sectionLabel = new Label();
                sectionLabel.Text = section.Title;
                sectionLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                sectionLabel.ForeColor = sectionHeaderColor;
                sectionLabel.Location = new Point(8, 4);
                sectionLabel.AutoSize = true;
                sectionHeader.Controls.Add(sectionLabel);

                // Add info icon if present
                if (!string.IsNullOrEmpty(section.InfoIcon))
                {
                    Label infoLabel = new Label();
                    infoLabel.Text = section.InfoIcon;
                    infoLabel.Font = new Font("Segoe UI", 8F);
                    infoLabel.ForeColor = sectionHeaderColor;
                    infoLabel.AutoSize = true;
                    infoLabel.Location = new Point(paneWidth - 25, 4);
                    sectionHeader.Controls.Add(infoLabel);
                }

                dropdownPaneContent.Controls.Add(sectionHeader);
                yOffset += sectionHeaderHeight;

                // Add items in a row layout for this section
                int itemsPerRow = section.Items.Count <= 4 ? section.Items.Count : 4;
                int itemWidth = (paneWidth - 16) / itemsPerRow;
                int xOffset = 4;
                int itemsInCurrentRow = 0;

                foreach (var item in section.Items)
                {
                    Button itemButton = new Button();
                    itemButton.FlatStyle = FlatStyle.Flat;
                    itemButton.FlatAppearance.BorderSize = item.IsSelected ? 1 : 0;
                    itemButton.FlatAppearance.BorderColor = theme.IsDarkMode ? Color.FromArgb(100, 149, 237) : Color.FromArgb(70, 130, 180);
                    itemButton.Font = new Font("Segoe UI", 8.5F);
                    itemButton.ForeColor = paneTextColor;
                    itemButton.BackColor = item.IsSelected ? selectedColor : paneBgColor;
                    itemButton.FlatAppearance.MouseOverBackColor = paneHoverColor;
                    itemButton.Location = new Point(xOffset, yOffset);
                    itemButton.Size = new Size(itemWidth, itemHeight);
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
                        yOffset += itemHeight + 2;
                        itemsInCurrentRow = 0;
                    }
                }

                // If we didn't complete a row, move to next line
                if (itemsInCurrentRow > 0)
                {
                    yOffset += itemHeight + 2;
                }

                yOffset += sectionSpacing;
            }

            // Calculate pane size
            int contentHeight = yOffset;
            dropdownPane.Size = new Size(paneWidth, 28 + contentHeight);
            dropdownPaneContent.Size = new Size(paneWidth - 2, contentHeight);

            // Position the dropdown centered under the center panel area
            int centerX = (this.Width - dropdownPane.Width) / 2;
            int paneY = titleBarPanel.Bottom;
            dropdownPane.Location = new Point(centerX, paneY);

            // Show and bring to front
            dropdownPane.Visible = true;
            dropdownPane.BringToFront();

            // Apply theme to the container
            dropdownPane.BackColor = paneBgColor;
            dropdownPaneLabel.ForeColor = paneTextColor;
            dropdownPaneContent.BackColor = paneBgColor;
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

            ThemeManager theme = ThemeManager.Instance;
            Color paneBgColor = theme.IsDarkMode ? Color.FromArgb(45, 45, 45) : Color.FromArgb(250, 250, 250);
            Color paneTextColor = theme.IsDarkMode ? Color.White : Color.Black;
            Color sectionHeaderColor = theme.IsDarkMode ? Color.FromArgb(180, 180, 180) : Color.FromArgb(100, 100, 100);
            Color paneHoverColor = theme.IsDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230);
            Color selectedColor = theme.IsDarkMode ? Color.FromArgb(70, 130, 180) : Color.FromArgb(200, 220, 240);

            int itemHeight = 28;
            int sectionHeaderHeight = 22;
            int sectionSpacing = 8;
            int groupSpacing = 8;

            // Calculate pane dimensions based on layout
            int paneWidth = Math.Max(180, layout.ColumnsPerPane * 100 + (layout.ColumnsPerPane - 1) * groupSpacing + 8);
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
                int groupX = 4 + col * (groupWidth + groupSpacing);
                int groupY = 0;
                for (int r = 0; r < row; r++) groupY += rowHeights[r] + groupSpacing;

                int yOffsetInGroup = 0;

                // Render each section within the group
                foreach (var section in group.Sections)
                {
                    // Add section header (skip if title is empty)
                    if (!string.IsNullOrEmpty(section.Title))
                    {
                        Panel sectionHeader = new Panel();
                        sectionHeader.Location = new Point(groupX, groupY + yOffsetInGroup);
                        sectionHeader.Size = new Size(groupWidth, sectionHeaderHeight);
                        sectionHeader.BackColor = Color.Transparent;

                        Label sectionLabel = new Label();
                        sectionLabel.Text = section.Title;
                        sectionLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                        sectionLabel.ForeColor = sectionHeaderColor;
                        sectionLabel.Location = new Point(4, 4);
                        sectionLabel.AutoSize = true;
                        sectionHeader.Controls.Add(sectionLabel);

                        // Add info icon if present
                        if (!string.IsNullOrEmpty(section.InfoIcon))
                        {
                            Label infoLabel = new Label();
                            infoLabel.Text = section.InfoIcon;
                            infoLabel.Font = new Font("Segoe UI", 8F);
                            infoLabel.ForeColor = sectionHeaderColor;
                            infoLabel.AutoSize = true;
                            infoLabel.Location = new Point(groupWidth - 20, 4);
                            sectionHeader.Controls.Add(infoLabel);
                        }

                        dropdownPaneContent.Controls.Add(sectionHeader);
                        yOffsetInGroup += sectionHeaderHeight;
                    }

                    // Add items in rows based on layout configuration
                    int itemsPerRow = Math.Min(layout.ItemsPerGroupRow, section.Items.Count);
                    int itemWidth = (groupWidth - 8) / itemsPerRow;
                    int xOffsetInGroup = 4;
                    int itemsInCurrentRow = 0;

                    foreach (var item in section.Items)
                    {
                        Button itemButton = new Button();
                        itemButton.FlatStyle = FlatStyle.Flat;
                        itemButton.FlatAppearance.BorderSize = 1; // Always show border for bounding box
                        itemButton.FlatAppearance.BorderColor = item.IsSelected ? 
                            (theme.IsDarkMode ? Color.FromArgb(100, 149, 237) : Color.FromArgb(70, 130, 180)) :
                            (theme.IsDarkMode ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200));
                        itemButton.Font = new Font("Segoe UI", 8.5F);
                        itemButton.ForeColor = paneTextColor;
                        itemButton.BackColor = item.IsSelected ? selectedColor : paneBgColor;
                        itemButton.FlatAppearance.MouseOverBackColor = paneHoverColor;
                        itemButton.Location = new Point(groupX + xOffsetInGroup, groupY + yOffsetInGroup);
                        itemButton.Size = new Size(itemWidth, itemHeight);
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
                            yOffsetInGroup += itemHeight + 2;
                            itemsInCurrentRow = 0;
                        }
                    }

                    // If we didn't complete a row, move to next line
                    if (itemsInCurrentRow > 0)
                    {
                        yOffsetInGroup += itemHeight + 2;
                    }

                    yOffsetInGroup += sectionSpacing;
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
                if (r < layout.RowsPerPane - 1) contentHeight += groupSpacing;
            }

            dropdownPane.Size = new Size(paneWidth, 28 + contentHeight);
            dropdownPaneContent.Size = new Size(paneWidth - 2, contentHeight);

            // Position the dropdown centered under the center panel area
            int centerX = (this.Width - dropdownPane.Width) / 2;
            int paneY = titleBarPanel.Bottom;
            dropdownPane.Location = new Point(centerX, paneY);

            // Show and bring to front
            dropdownPane.Visible = true;
            dropdownPane.BringToFront();

            // Apply theme to the container
            dropdownPane.BackColor = paneBgColor;
            dropdownPaneLabel.ForeColor = paneTextColor;
            dropdownPaneContent.BackColor = paneBgColor;
        }

        private void HideDropdownPane()
        {
            dropdownPane.Visible = false;
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
            mainStatusStrip.Visible = paneStatusBarToggleSwitch.Checked;
            Settings.SetRegValue("kvmStatusBarVisible", paneStatusBarToggleSwitch.Checked ? "1" : "0");
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

            // Update chat button in title bar (use center panel color for pill background)
            chatButton.FillColor = theme.IsDarkMode ? Color.FromArgb(65, 65, 65) : Color.FromArgb(200, 200, 200);
            chatButton.ForeColor = titleBarTextColor;

            // Update files button in title bar (same style as chat)
            openRemoteFilesButton.FillColor = theme.IsDarkMode ? Color.FromArgb(65, 65, 65) : Color.FromArgb(200, 200, 200);
            openRemoteFilesButton.ForeColor = titleBarTextColor;

            // Update connect button in title bar (same style as chat)
            connectButton.FillColor = theme.IsDarkMode ? Color.FromArgb(65, 65, 65) : Color.FromArgb(200, 200, 200);
            connectButton.ForeColor = titleBarTextColor;

            // Update CAD button in title bar (same style as chat)
            cadButton.FillColor = theme.IsDarkMode ? Color.FromArgb(65, 65, 65) : Color.FromArgb(200, 200, 200);
            cadButton.ForeColor = titleBarTextColor;

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
        }

        private void UpdateDropdownPaneTheme()
        {
            ThemeManager theme = ThemeManager.Instance;
            Color paneBgColor = theme.IsDarkMode ? Color.FromArgb(45, 45, 45) : Color.FromArgb(250, 250, 250);
            Color paneTextColor = theme.IsDarkMode ? Color.White : Color.Black;
            Color paneHoverColor = theme.IsDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230);

            // Update dropdown pane container
            dropdownPane.BackColor = paneBgColor;
            dropdownPaneLabel.ForeColor = paneTextColor;
            dropdownPaneContent.BackColor = paneBgColor;

            // Update settings pane buttons with theme-aware tinted icons
            settingsPaneSettingsButton.BackColor = paneBgColor;
            settingsPaneSettingsButton.ForeColor = paneTextColor;
            settingsPaneSettingsButton.Image = GetTintedIcon(Properties.Resources.Gear20, paneTextColor);
            settingsPaneSettingsButton.FlatAppearance.MouseOverBackColor = paneHoverColor;
            
            settingsPaneStatsButton.BackColor = paneBgColor;
            settingsPaneStatsButton.ForeColor = paneTextColor;
            settingsPaneStatsButton.Image = GetTintedIcon(Properties.Resources.Statistics20, paneTextColor);
            settingsPaneStatsButton.FlatAppearance.MouseOverBackColor = paneHoverColor;
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
