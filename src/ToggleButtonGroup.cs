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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MeshCentralRouter
{
    /// <summary>
    /// A group of toggle buttons that act as radio buttons - only one can be selected at a time.
    /// Follows the ButtonControl pattern from panel_design_notes.md with IsToggle=true and GroupName behavior.
    /// </summary>
    public class ToggleButtonGroup : Panel
    {
        private List<ToggleButton> _buttons = new List<ToggleButton>();
        private object _selectedValue = null;
        private int _buttonSpacing = 4;
        private int _sidePadding = 8;

        public event EventHandler<ToggleButtonValueChangedEventArgs> SelectedValueChanged;

        public ToggleButtonGroup()
        {
            this.BackColor = Color.Transparent;
        }

        /// <summary>
        /// Spacing between buttons in pixels
        /// </summary>
        public int ButtonSpacing
        {
            get { return _buttonSpacing; }
            set { _buttonSpacing = value; LayoutButtons(); }
        }

        /// <summary>
        /// Padding on the left and right sides
        /// </summary>
        public int SidePadding
        {
            get { return _sidePadding; }
            set { _sidePadding = value; LayoutButtons(); }
        }

        /// <summary>
        /// The currently selected value
        /// </summary>
        public object SelectedValue
        {
            get { return _selectedValue; }
            set
            {
                if (_selectedValue != value)
                {
                    _selectedValue = value;
                    UpdateButtonStates();
                    SelectedValueChanged?.Invoke(this, new ToggleButtonValueChangedEventArgs(value));
                }
            }
        }

        /// <summary>
        /// Adds a toggle button option to the group
        /// </summary>
        /// <param name="label">Display text for the button</param>
        /// <param name="value">Value associated with this button</param>
        public ToggleButton AddButton(string label, object value)
        {
            var button = new ToggleButton
            {
                Text = label,
                Value = value,
                IsSelected = (value != null && value.Equals(_selectedValue))
            };
            button.Click += Button_Click;
            _buttons.Add(button);
            this.Controls.Add(button);
            LayoutButtons();
            return button;
        }

        /// <summary>
        /// Clears all buttons from the group
        /// </summary>
        public void ClearButtons()
        {
            foreach (var button in _buttons)
            {
                button.Click -= Button_Click;
                this.Controls.Remove(button);
                button.Dispose();
            }
            _buttons.Clear();
        }

        /// <summary>
        /// Updates the theme colors for all buttons
        /// </summary>
        public void UpdateTheme(Color backgroundColor, Color textColor, Color selectedColor,
            Color borderColor, Color selectedBorderColor, Color hoverColor)
        {
            foreach (var button in _buttons)
            {
                button.BackgroundColor = backgroundColor;
                button.TextColor = textColor;
                button.SelectedColor = selectedColor;
                button.BorderColor = borderColor;
                button.SelectedBorderColor = selectedBorderColor;
                button.HoverColor = hoverColor;
                button.UpdateVisualState();
            }
        }

        private void Button_Click(object sender, EventArgs e)
        {
            var clickedButton = sender as ToggleButton;
            if (clickedButton != null)
            {
                SelectedValue = clickedButton.Value;
            }
        }

        private void UpdateButtonStates()
        {
            foreach (var button in _buttons)
            {
                button.IsSelected = (button.Value != null && button.Value.Equals(_selectedValue));
                button.UpdateVisualState();
            }
        }

        private void LayoutButtons()
        {
            if (_buttons.Count == 0) return;

            int availableWidth = this.Width - (_sidePadding * 2) - (_buttonSpacing * (_buttons.Count - 1));
            int buttonWidth = availableWidth / _buttons.Count;
            int buttonHeight = this.Height;

            for (int i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].Location = new Point(_sidePadding + (i * (buttonWidth + _buttonSpacing)), 0);
                _buttons[i].Size = new Size(buttonWidth, buttonHeight);
            }
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            LayoutButtons();
        }
    }

    /// <summary>
    /// Event args for when the selected value changes
    /// </summary>
    public class ToggleButtonValueChangedEventArgs : EventArgs
    {
        public object Value { get; }

        public ToggleButtonValueChangedEventArgs(object value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// A single toggle button within a ToggleButtonGroup.
    /// Styled with flat appearance and visual feedback for selected state.
    /// </summary>
    public class ToggleButton : Button
    {
        private bool _isSelected = false;
        private object _value;
        private Color _backgroundColor = Color.FromArgb(45, 45, 45);
        private Color _textColor = Color.White;
        private Color _selectedColor = Color.FromArgb(70, 130, 180);
        private Color _borderColor = Color.FromArgb(80, 80, 80);
        private Color _selectedBorderColor = Color.FromArgb(100, 149, 237);
        private Color _hoverColor = Color.FromArgb(60, 60, 60);

        public ToggleButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 1;
            this.Font = new Font("Segoe UI", 8F);
            this.TextAlign = ContentAlignment.MiddleCenter;
            this.Cursor = Cursors.Hand;
            UpdateVisualState();
        }

        /// <summary>
        /// The value associated with this button
        /// </summary>
        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// Whether this button is currently selected
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                UpdateVisualState();
            }
        }

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set { _backgroundColor = value; }
        }

        public Color TextColor
        {
            get { return _textColor; }
            set { _textColor = value; this.ForeColor = value; }
        }

        public Color SelectedColor
        {
            get { return _selectedColor; }
            set { _selectedColor = value; }
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; }
        }

        public Color SelectedBorderColor
        {
            get { return _selectedBorderColor; }
            set { _selectedBorderColor = value; }
        }

        public Color HoverColor
        {
            get { return _hoverColor; }
            set { _hoverColor = value; this.FlatAppearance.MouseOverBackColor = value; }
        }

        /// <summary>
        /// Updates the visual appearance based on current state and theme
        /// </summary>
        public void UpdateVisualState()
        {
            this.BackColor = _isSelected ? _selectedColor : _backgroundColor;
            this.FlatAppearance.BorderColor = _isSelected ? _selectedBorderColor : _borderColor;
            this.FlatAppearance.MouseOverBackColor = _hoverColor;
            this.ForeColor = _textColor;
        }
    }
}
