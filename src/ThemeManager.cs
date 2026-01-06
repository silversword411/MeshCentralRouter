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
using System.Windows.Forms;
using System.Drawing;

namespace MeshCentralRouter
{
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private bool _isDarkMode;
        
        public event EventHandler ThemeChanged;

        private ThemeManager()
        {
            // Load theme preference from registry
            _isDarkMode = Settings.GetRegValue("DarkMode", false);
        }

        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ThemeManager();
                }
                return _instance;
            }
        }

        public bool IsDarkMode
        {
            get { return _isDarkMode; }
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    Settings.SetRegValue("DarkMode", _isDarkMode);
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public Color GetTitleBarColor()
        {
            return _isDarkMode ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
        }

        public Color GetTitleBarTextColor()
        {
            return _isDarkMode ? Color.White : SystemColors.ControlText;
        }

        public Color GetBackgroundColor()
        {
            return _isDarkMode ? Color.FromArgb(30, 30, 30) : Color.White;
        }

        public Color GetForegroundColor()
        {
            return _isDarkMode ? Color.White : Color.Black;
        }

        public Color GetButtonHoverColor()
        {
            return _isDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(240, 240, 240);
        }
    }
}
