using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace DBADashGUI
{
    public class SavedViewSelectedEventArgs : EventArgs
    {
        public string Name;
        public bool IsGlobal;
        public string SerializedObject;
    }

    /// <summary>
    /// Custom ToolStripDrownDownButton that shows the saved views available for selection
    /// </summary>
    [ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip)]
    public class SavedViewMenuItem : ToolStripDropDownButton
    {
        private Dictionary<string, string> _savedViews;
        private Dictionary<string, string> _globalSavedViews;

        private static readonly string globalTag = "Global";
        private static readonly string userTag = "User";
        private static readonly string noneText = "{None}";
        private Guid connectionGUID;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public SavedView.ViewTypes Type { get; set; }

        public event EventHandler<SavedViewSelectedEventArgs> SavedViewSelected;

        public override ToolStripItemDisplayStyle DisplayStyle { get => base.DisplayStyle; set => base.DisplayStyle = ToolStripItemDisplayStyle.Text; }
        public override string Text { get => base.Text; set => base.Text = _text; }
        private string _text = "View";

        private void SetText(string text)
        {
            _text = text;
            base.Text = text;
        }

        /// <summary>
        /// Selects the default view from the menu and raises the SavedViewSelected event for it.
        /// </summary>
        public bool SelectDefault()
        {
            if (_savedViews.TryGetValue(SavedView.DefaultViewName, out var view)) // Check if user has a default view
            {
                SelectItem(SavedView.DefaultViewName, false);
                SavedViewSelected?.Invoke(this, new SavedViewSelectedEventArgs() { Name = SavedView.DefaultViewName, IsGlobal = false, SerializedObject = view });
                return true;
            }
            else if (_globalSavedViews.TryGetValue(SavedView.DefaultViewName, out var savedView)) // check for a global default view if user default view is not available
            {
                SelectItem(SavedView.DefaultViewName, true);
                SavedViewSelected?.Invoke(this, new SavedViewSelectedEventArgs() { Name = SavedView.DefaultViewName, IsGlobal = true, SerializedObject = savedView });
                return true;
            }
            else
            {
                SelectItem(noneText, true);
                SavedViewSelected?.Invoke(this, new SavedViewSelectedEventArgs() { Name = noneText, IsGlobal = true, SerializedObject = string.Empty });
                return false;
            }
        }

        public bool ContainsUserView(string name)
        {
            return _savedViews.ContainsKey(name);
        }

        public bool ContainsGlobalView(string name)
        {
            return _globalSavedViews.ContainsKey(name);
        }

        /// <summary>
        /// Load saved view menu items.  Only runs if items haven't already been loaded.
        /// </summary>
        public bool LoadItems()
        {
            if (HasDropDownItems && connectionGUID == Common.ConnectionGUID)
            {
                return false;
            }
            else
            {
                RefreshItems();
                return true;
            }
        }

        /// <summary>
        /// Load saved view menu items and select the default.  Only runs if items haven't already been loaded.
        /// </summary>
        public bool LoadItemsAndSelectDefault()
        {
            var loaded = LoadItems();
            if (loaded)
            {
                SelectDefault();
            }
            return loaded;
        }

        /// <summary>
        /// Load saved view menu items - replaces any existing items with new ones from the DB.
        /// </summary>
        public void RefreshItems()
        {
            _savedViews = SavedView.GetSavedViews(Type, DBADashUser.UserID);
            _globalSavedViews = SavedView.GetSavedViews(Type, DBADashUser.SystemUserID);
            DropDownItems.Clear();
            ToolStripMenuItem mnuNone = new()
            {
                Text = noneText,
                BackColor = DashColors.TrimbleBlueDark,
                ForeColor = Color.White,
                Tag = globalTag
            };
            mnuNone.Click += SavedView_Click;
            DropDownItems.Add(mnuNone);
            foreach (var view in _globalSavedViews)
            {
                ToolStripMenuItem mnu = new()
                {
                    Text = view.Key,
                    BackColor = DashColors.TrimbleBlueDark,
                    ForeColor = Color.White,
                    Tag = globalTag
                };
                mnu.Click += SavedView_Click;
                DropDownItems.Add(mnu);
            }
            foreach (var view in _savedViews)
            {
                ToolStripMenuItem mnu = new()
                {
                    Text = view.Key,
                    BackColor = DashColors.TrimbleYellow,
                    ForeColor = Color.Black,
                    Tag = userTag
                };
                mnu.Click += SavedView_Click;
                DropDownItems.Add(mnu);
            }
            SetText("View");
            Font = new Font(Font, FontStyle.Regular);
            SelectedSavedView = string.Empty;
            connectionGUID = Common.ConnectionGUID; // Detect if we have changed connection to the repository DB for LoadItems
        }

        public void ClearSelectedItem()
        {
            SelectItem(string.Empty, false);
        }

        /// <summary>
        /// Select a saved view in the menu by name
        /// </summary>
        public void SelectItem(string selectedItem, bool isGlobal)
        {
            foreach (ToolStripMenuItem mnu in DropDownItems)
            {
                var mnuIsGlobal = Convert.ToString(mnu.Tag) == globalTag;
                var isSelected = mnu.Text?.ToLower() == selectedItem.ToLower() && isGlobal == mnuIsGlobal;
                mnu.Checked = isSelected;
                mnu.Font = isSelected ? new Font(mnu.Font, FontStyle.Bold) : new Font(mnu.Font, FontStyle.Regular);
            }
            if (selectedItem != noneText && selectedItem != string.Empty)
            {
                SetText("View: " + selectedItem);
                Font = new Font(Font, FontStyle.Bold);
            }
            else
            {
                SetText(Text = @"View");
                Font = new Font(Font, FontStyle.Regular);
            }
            SelectedSavedView = selectedItem;
            SelectedSavedViewIsGlobal = isGlobal;
        }

        public string SelectedSavedView { get; private set; }
        public bool SelectedSavedViewIsGlobal { get; private set; }

        private void SavedView_Click(object sender, EventArgs e)
        {
            var mnu = (ToolStripMenuItem)sender;
            var isGlobal = Convert.ToString(mnu.Tag) == globalTag;
            string serializedObject;
            if (isGlobal)
            {
                serializedObject = mnu.Text == noneText ? string.Empty : _globalSavedViews[mnu.Text!];
            }
            else
            {
                serializedObject = _savedViews[mnu.Text!];
            }
            SelectItem(mnu.Text, isGlobal);
            SavedViewSelected?.Invoke(this, new SavedViewSelectedEventArgs() { Name = mnu.Text, IsGlobal = isGlobal, SerializedObject = serializedObject });
        }
    }
}