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

using Microsoft.Win32;
using System.Collections.Generic;
using System;

namespace MeshCentralRouter
{
    public static class Settings
    {
        public static void SetRegValue(string name, string value)
        {
            try { Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Open Source\MeshCentral Router", name, value); } catch (Exception) { }
        }
        public static void SetRegValue(string name, bool value)
        {
            try { Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Open Source\MeshCentral Router", name, value.ToString()); } catch (Exception) { }
        }
        public static void SetRegValue(string name, int value)
        {
            try { Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Open Source\MeshCentral Router", name, value.ToString()); } catch (Exception) { }
        }
        /// <summary>
        /// This function querys the registry. If the key is found it returns the value as a string
        /// </summary>
        /// <param name="name">Keyname</param>
        /// <param name="value">Return on fail</param>
        /// <returns></returns>
        public static string GetRegValue(string name, string value)
        {
            try {
                String v = (String)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Open Source\MeshCentral Router", name, value);
                if (v == null) return value;
                return v.ToString();
            } catch (Exception) { return value; }
        }
        /// <summary>
        /// This function querys the registry. If the key is found it returns the value as a boolean
        /// </summary>
        /// <param name="name">Keyname</param>
        /// <param name="value">Return on fail</param>
        /// <returns></returns>
        public static bool GetRegValue(string name, bool value)
        {
            try { return bool.Parse(GetRegValue(name, value.ToString())); } catch (Exception) { return value; }
        }

        public static int GetRegValue(string name, int value)
        {
            try { return int.Parse(GetRegValue(name, value.ToString())); } catch (Exception) { return value; }
        }

        public static void SetApplications(List<string[]> apps)
        {
            ClearApplications();
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Open Source\MeshCentral Router\Applications", true))
            {
                foreach (string[] app in apps)
                {
                    using (RegistryKey skey = key.CreateSubKey(app[0]))
                    {
                        skey.SetValue("Protocol", app[1]);
                        skey.SetValue("Command", app[2]);
                        skey.SetValue("Arguments", app[3]);
                    }
                }
            }
        }

        public static List<string[]> GetApplications()
        {
            List<string[]> apps = new List<string[]>();
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Open Source\MeshCentral Router\Applications\", false))
            {
                string[] keys = key.GetSubKeyNames();
                foreach (string k in keys)
                {
                    using (RegistryKey key2 = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Open Source\MeshCentral Router\Applications\" + k, false))
                    {
                        string protocol = (string)key2.GetValue("Protocol");
                        string command = (string)key2.GetValue("Command");
                        string args = (string)key2.GetValue("Arguments");
                        String[] a = new string[4];
                        a[0] = k;
                        a[1] = protocol;
                        a[2] = command;
                        a[3] = args;
                        apps.Add(a);
                    }
                }
            }
            return apps;
        }

        public static void ClearApplications()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Open Source\MeshCentral Router", true);
            key.DeleteSubKeyTree("Applications");
            key.Close();
        }

        /// <summary>
        /// Generate a safe registry key name from a server URL
        /// </summary>
        private static string GetServerKeyName(string serverUrl)
        {
            if (string.IsNullOrEmpty(serverUrl)) return "default";
            // Use a hash to create a safe key name
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(serverUrl));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
            }
        }

        /// <summary>
        /// Save the collapsed state of groups for a specific server
        /// </summary>
        public static void SetCollapsedGroups(string serverUrl, List<string> collapsedMeshIds)
        {
            System.Diagnostics.Debug.WriteLine("SetCollapsedGroups called with " + collapsedMeshIds.Count + " collapsed groups for server: " + serverUrl);
            try
            {
                string serverKey = GetServerKeyName(serverUrl);
                string keyPath = @"SOFTWARE\Open Source\MeshCentral Router\Servers\" + serverKey;
                System.Diagnostics.Debug.WriteLine("SetCollapsedGroups using registry path: " + keyPath);
                
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath, true))
                {
                    if (key != null)
                    {
                        // Store the actual server URL for reference
                        key.SetValue("ServerUrl", serverUrl);
                        // Store collapsed groups as a comma-separated string
                        string collapsedStr = string.Join(",", collapsedMeshIds.ToArray());
                        System.Diagnostics.Debug.WriteLine("SetCollapsedGroups writing CollapsedGroups = '" + collapsedStr + "'");
                        key.SetValue("CollapsedGroups", collapsedStr);
                        // Mark that state has been saved (to distinguish from no saved state)
                        key.SetValue("StateSaved", "true");
                        System.Diagnostics.Debug.WriteLine("SetCollapsedGroups DONE - wrote to registry");
                    }
                }
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine("SetCollapsedGroups ERROR: " + ex.Message);
            }
        }

        /// <summary>
        /// Load the collapsed state of groups for a specific server
        /// Returns null if no state has been saved, or a list (possibly empty) if state exists
        /// </summary>
        public static List<string> GetCollapsedGroups(string serverUrl)
        {
            try
            {
                string serverKey = GetServerKeyName(serverUrl);
                string keyPath = @"SOFTWARE\Open Source\MeshCentral Router\Servers\" + serverKey;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, false))
                {
                    if (key != null)
                    {
                        // Check if state has been saved for this server
                        string stateSaved = key.GetValue("StateSaved", "").ToString();
                        if (stateSaved == "true")
                        {
                            // State exists - return the list (may be empty if all groups are expanded)
                            List<string> result = new List<string>();
                            string collapsedStr = key.GetValue("CollapsedGroups", "").ToString();
                            if (!string.IsNullOrEmpty(collapsedStr))
                            {
                                string[] meshIds = collapsedStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                result.AddRange(meshIds);
                            }
                            return result;
                        }
                    }
                }
            }
            catch (Exception) { }
            return null; // No saved state
        }
    }
}
