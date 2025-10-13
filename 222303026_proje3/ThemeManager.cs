// ArtFusion - The AI-enabled simple image editor for everyone, born as a school project by GeniusPilot2016
// Copyright (C) 2025 GeniusPilot2016
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

namespace _222303026_proje3
{
    public static class ThemeManager
    {
        // Event that will be raised when theme changes
        public static event EventHandler ThemeChanged;

        // List to keep track of all open forms
        private static readonly List<WeakReference<Form>> openForms = new();

        // Method to register a form with the theme manager
        public static void RegisterForm(Form form)
        {
            // Remove any dead references
            CleanupDeadReferences();

            // Add the new form
            openForms.Add(new WeakReference<Form>(form));

            // Subscribe to form's FormClosed event to remove it from the list
            form.FormClosed += (s, e) => CleanupDeadReferences();
        }

        // Method to apply theme to all registered forms
        public static void ApplyThemeToAllForms()
        {
            // Notify all subscribers
            ThemeChanged?.Invoke(null, EventArgs.Empty);

            // Apply theme to all registered forms
            foreach (var weakRef in openForms.ToArray())
            {
                if (weakRef.TryGetTarget(out Form form) && !form.IsDisposed && form.IsHandleCreated)
                {
                    // Invoke SetTheme method if it exists
                    var method = form.GetType().GetMethod("SetTheme", System.Reflection.BindingFlags.Instance |
                                                                      System.Reflection.BindingFlags.Public |
                                                                      System.Reflection.BindingFlags.NonPublic);
                    if (method != null)
                    {
                        // Ensure we call the method on the UI thread
                        if (form.InvokeRequired)
                        {
                            form.Invoke(() => method.Invoke(form, null));
                        }
                        else
                        {
                            method.Invoke(form, null);
                        }
                    }
                }
            }
        }

        // Remove references to closed/disposed forms
        private static void CleanupDeadReferences()
        {
            for (int i = openForms.Count - 1; i >= 0; i--)
            {
                if (!openForms[i].TryGetTarget(out Form form) || form.IsDisposed)
                {
                    openForms.RemoveAt(i);
                }
            }
        }
    }
}