using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using BTD_Backend;
using BTD6_Mod_Manager.Classes;
using BTD6_Mod_Manager.UserControls;

namespace BTD6_Mod_Manager
{
    /// <summary>
    /// Interaction logic for ModItem_UserControl.xaml
    /// </summary>
    public partial class ModItem_UserControl : UserControl
    {
        public string modName = "";
        public string modPath = "";

        public ModItem_UserControl()
        {
            InitializeComponent();
            Mods_UserControl.instance.SelectedMods_ListBox.FontSize = 19;
        }

        private void CheckBox_Clicked(object sender, RoutedEventArgs e)
        {
            if (TempGuard.IsDoingWork(MainWindow.workType))
                return;

            CheckBox cb = (CheckBox)(sender);

            if (cb.IsChecked == true)
            {
                if (!Mods_UserControl.instance.SelectedMods_ListBox.Items.Contains(modName))
                {
                    Mods_UserControl.instance.AddToSelectedModLB(modPath);
                    if (modPath.EndsWith(Mods_UserControl.instance.disabledKey))
                        modPath = modPath.Replace(Mods_UserControl.instance.disabledKey, "");
                }
            }
            else
            {
                if (Mods_UserControl.instance.SelectedMods_ListBox.Items.Contains(modName))
                {
                    Mods_UserControl.instance.RemoveFromSelectedLB(modPath);
                    if (!modPath.EndsWith(Mods_UserControl.instance.disabledKey))
                        modPath += Mods_UserControl.instance.disabledKey;
                }
            }
        }

        public override string ToString() => modPath;

        private void DeleteMod_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(modPath))
            {
                Mods_UserControl.instance.PopulateMods(SessionData.CurrentGame);
                return;
            }

            System.Windows.Forms.DialogResult diag = System.Windows.Forms.MessageBox.Show("Are you sure you want to delete this mod?", "Delete mod?", System.Windows.Forms.MessageBoxButtons.YesNo);
            if (diag == System.Windows.Forms.DialogResult.No)
                return;

            File.Delete(modPath);
            Mods_UserControl.instance.PopulateMods(SessionData.CurrentGame);
        }

        private void OpenMod_Button_Click(object sender, RoutedEventArgs e)
        {
            FileInfo f = new FileInfo(modPath);

            if (!Directory.Exists(f.Directory.FullName))
                return;

            Process.Start(f.Directory.FullName);
        }
    }
}
