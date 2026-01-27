/*
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

namespace MeshCentralRouter
{
    /// <summary>
    /// Composite control used in the KVM title bar: left half is a vertical toggle (auto sync),
    /// right half shows stacked up/down buttons for manual clipboard send/receive when auto sync is off.
    /// </summary>
    public class ClipboardButtonControl : Panel
    {
        private readonly VerticalToggleSwitch toggle;
        private readonly Button upArrowButton;
        private readonly Button downArrowButton;
        private readonly Panel arrowPanel;

        public event EventHandler ToggleCheckedChanged;
        public event EventHandler UpArrowClick;
        public event EventHandler DownArrowClick;

        public ClipboardButtonControl()
        {
            Size = new Size(32, 24);
            BackColor = Color.Transparent;

            // Left: vertical toggle
            toggle = new VerticalToggleSwitch
            {
                Size = new Size(13, 24),
                Location = new Point(1, 0),
                TabStop = false
            };
            toggle.CheckedChanged += Toggle_CheckedChanged;
            Controls.Add(toggle);

            // Right: arrow stack - make it wider to fit arrows
            arrowPanel = new Panel
            {
                Size = new Size(18, 24),
                Location = new Point(14, 0),
                BackColor = Color.Transparent
            };
            Controls.Add(arrowPanel);

            upArrowButton = CreateArrowButton(new Point(1, 0), UpArrowButton_Click);
            arrowPanel.Controls.Add(upArrowButton);

            downArrowButton = CreateArrowButton(new Point(1, 12), DownArrowButton_Click);
            arrowPanel.Controls.Add(downArrowButton);

            UpdateArrowVisibility();
        }

        public bool ToggleChecked
        {
            get { return toggle.Checked; }
            set { toggle.Checked = value; }
        }

        public void SetToggleColors(Color onColor, Color offColor, Color thumbColor)
        {
            toggle.OnColor = onColor;
            toggle.OffColor = offColor;
            toggle.ThumbColor = thumbColor;
        }

        public void SetArrowIcons(Image upIcon, Image downIcon)
        {
            // Use built-in clipboard icons: Out = send (up), In = receive (down)
            upArrowButton.Image = Properties.Resources.iconClipboardOut;
            downArrowButton.Image = Properties.Resources.iconClipboardIn;
            upArrowButton.ImageAlign = ContentAlignment.MiddleCenter;
            downArrowButton.ImageAlign = ContentAlignment.MiddleCenter;
            upArrowButton.Text = string.Empty;
            downArrowButton.Text = string.Empty;
        }

        public void SetButtonBackColor(Color color)
        {
            BackColor = color;
            arrowPanel.BackColor = color;
            upArrowButton.BackColor = color;
            downArrowButton.BackColor = color;
            toggle.BackColor = color;
        }

        private Button CreateArrowButton(Point location, EventHandler handler)
        {
            var btn = new Button
            {
                Size = new Size(16, 12),
                Location = location,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabStop = false,
                BackColor = Color.Transparent,
                UseVisualStyleBackColor = false,
                AutoSize = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 255, 255, 255);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 255, 255, 255);
            btn.Click += handler;
            return btn;
        }

        private void Toggle_CheckedChanged(object sender, EventArgs e)
        {
            UpdateArrowVisibility();
            ToggleCheckedChanged?.Invoke(this, e);
        }

        private void UpdateArrowVisibility()
        {
            // Show arrows only when auto-sync is off
            arrowPanel.Visible = !toggle.Checked;
        }

        private void UpArrowButton_Click(object sender, EventArgs e)
        {
            UpArrowClick?.Invoke(this, e);
        }

        private void DownArrowButton_Click(object sender, EventArgs e)
        {
            DownArrowClick?.Invoke(this, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                toggle.CheckedChanged -= Toggle_CheckedChanged;
                upArrowButton.Click -= UpArrowButton_Click;
                downArrowButton.Click -= DownArrowButton_Click;
            }
            base.Dispose(disposing);
        }
    }
}