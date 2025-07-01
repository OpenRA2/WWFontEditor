﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Nyerguds.ImageManipulation;
using Nyerguds.Ini;
using Nyerguds.Util.UI;
using Nyerguds.Util.UI.Wrappers;

namespace WWFontEditor.UI
{
    public partial class FrmManagePalettes : Form
    {
        private const String CREATENEW = "Create new...";
        private const String INISECTION = "Palette";
        private Int32 bpp;
        private Int32 nrOfColorsPerPal;
        private Int32 nrOfSubPalettes;
        private PaletteDropDownInfo paletteToSave;
        private readonly String appPath = Path.GetDirectoryName(Application.ExecutablePath);
        private readonly String title = FrmFontEditor.GetTitle(false);
        private Boolean immediateSave = false;
        private Dictionary<String, List<PaletteDropDownInfo>> subPalettes;
        private List<String> removedPalettes;

        public PaletteDropDownInfo PaletteToSave
        {
            get { return this.paletteToSave; }
            set
            {
                this.paletteToSave = value;
                this.palReplaceBy.Palette = value.Colors;
            }
        }

        public FrmManagePalettes(Int32 bpp)
        {
            this.InitializeComponent();
            this.bpp = bpp;
            this.nrOfColorsPerPal = 1 << bpp;
            this.nrOfSubPalettes = 256 / this.nrOfColorsPerPal;            
            this.lbSubPalettes.Items.Clear();
            this.removedPalettes = new List<String>();
            this.Init();
            this.Opacity = 0;
        }

        private void Init()
        {
            this.subPalettes = new Dictionary<String, List<PaletteDropDownInfo>>();
            FileInfo[] files = new DirectoryInfo(this.appPath).GetFiles("*.pal");
            Int32 nrOfFiles = files.Length;
            for (Int32 i = 0; i < nrOfFiles; ++i)
            {
                FileInfo file = files[i];
                if (file.Length != 0x300)
                    continue;
                String name = file.Name;
                try
                {
                    // Attempt to read as palette
                    ColorUtils.ReadSixBitPaletteFile(file.FullName);
                }
                catch (ArgumentException)
                {
                    continue;
                }
                List<PaletteDropDownInfo> currentSubPals = PaletteDropDownInfo.LoadSubPalettesInfoFromPalette(new FileInfo(file.FullName), true, true, false);
                String inipath = Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(name)) + ".ini";
                if (this.bpp == 4)
                {
                    if (!File.Exists(inipath))
                        continue;
                    // Trim all unused entries off the end
                    this.TrimSubPalettes(currentSubPals);
                }
                else if (this.bpp == 8)
                {
                    if (File.Exists(inipath))
                        continue;
                    currentSubPals = PaletteDropDownInfo.LoadSubPalettesInfoFromPalette(new FileInfo(file.FullName), true, true, false);
                }
                else
                    continue;
                this.subPalettes.Add(name, currentSubPals);
            }
            foreach (String key in this.subPalettes.Keys)
                this.cmbPalettes.Items.Add(key);
            this.cmbPalettes.Items.Add(CREATENEW);
        }

        private void TrimSubPalettes(List<PaletteDropDownInfo> subPalettes)
        {
            for (Int32 i = subPalettes.Count - 1; i >= 0; i--)
            {
                PaletteDropDownInfo curr = subPalettes[i];
                if (!String.IsNullOrEmpty(curr.Name))
                    break;
                subPalettes.RemoveAt(i);
            }
        }

        private void CmbPalettes_SelectedIndexChanged(Object sender, EventArgs e)
        {
            if (this.cmbPalettes.SelectedIndex >= this.subPalettes.Count)
            {
                this.lbSubPalettes.Items.Clear();
                // "Create new" item was selected.
                // Give "save as" dialog? Maybe not, it only works from the program's folder. Just name + existence check, then?
                String newPaletteName = InputBox.Show("Palette name:", this.title, "newpal.pal", FormStartPosition.CenterParent);
                if (String.IsNullOrEmpty(newPaletteName))
                {
                    if (newPaletteName != null)
                        MessageBox.Show(this, "Palette needs a name!", this.title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count > 1 ? 0 : -1;
                    return;
                }
                if (newPaletteName.Contains(Path.DirectorySeparatorChar))
                {
                    this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count > 1 ? 0 : -1;
                    return;
                } 
                if (!newPaletteName.EndsWith(".pal"))
                    newPaletteName += ".pal";
                String newPath = Path.Combine(this.appPath, newPaletteName);
                List<PaletteDropDownInfo> newPalInfo;
                Int32 existingpos = this.subPalettes.Keys.ToList().IndexOf(newPaletteName);
                if (existingpos != -1)
                {
                    MessageBox.Show(this, "Palette already exists!", this.title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.cmbPalettes.SelectedIndex = existingpos;
                    return;
                }
                String barePalName = newPaletteName.Substring(0, newPaletteName.Length - 4);
                String iniPath = Path.Combine(this.appPath, barePalName + ".ini");
                Boolean iniExists = File.Exists(iniPath);
                if (File.Exists(newPath))
                {
                    if (this.bpp == 4)
                    {
                        DialogResult dr = DialogResult.Yes;
                        if (!iniExists)
                            dr = MessageBox.Show(this, "Palette already exists as 8-bit palette!\nAre you sure you want to use this for 4-bit palettes?", this.title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dr == DialogResult.Yes)
                        {
                            if (iniExists)
                            {
                                newPalInfo = PaletteDropDownInfo.LoadSubPalettesInfoFromPalette(new FileInfo(newPath), true, true, false);
                                this.TrimSubPalettes(newPalInfo);
                            }
                            else
                            {
                                newPalInfo = new List<PaletteDropDownInfo>();
                                newPalInfo.Add(new PaletteDropDownInfo(barePalName, this.bpp, Enumerable.Repeat(Color.Empty, this.nrOfColorsPerPal).ToArray(), newPaletteName, 0, true, false));
                            }
                            this.subPalettes.Add(newPaletteName, newPalInfo);
                            this.removedPalettes.Remove(newPaletteName);
                            this.cmbPalettes.Items[this.subPalettes.Count - 1] = newPaletteName;
                            this.cmbPalettes.Items.Add(CREATENEW);
                        }
                        else
                        {
                            this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count > 1 ? 0 : -1;
                        }
                    }
                    else if (this.bpp == 8)
                    {
                        DialogResult dr = DialogResult.Yes;
                        if (iniExists)
                            dr = MessageBox.Show(this, "Palette already exists as 4-bit palette!\nAre you sure you want to use this as 8-bit palette?", this.title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dr == DialogResult.Yes)
                        {
                            newPalInfo = new List<PaletteDropDownInfo>();
                            newPalInfo.Add(new PaletteDropDownInfo(newPaletteName, this.bpp, Enumerable.Repeat(Color.Empty, this.nrOfColorsPerPal).ToArray(), newPaletteName, 0, true, false));
                            this.subPalettes.Add(newPaletteName, newPalInfo);
                            this.removedPalettes.Remove(newPaletteName);
                            this.cmbPalettes.Items[this.subPalettes.Count - 1] = newPaletteName;
                            this.cmbPalettes.Items.Add(CREATENEW);
                        }
                        else
                        {
                            this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count > 1 ? 0 : -1;
                        }
                    }
                    else
                    {
                        // Can normally never happen; dialog is always for 4 or 8 bit.
                        this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count > 1 ? 0 : -1;
                    }
                }
                else
                {
                    // Finally, the normal cases.
                    newPalInfo = new List<PaletteDropDownInfo>();
                    newPalInfo.Add(new PaletteDropDownInfo(this.bpp == 8 ? newPaletteName : barePalName, this.bpp, Enumerable.Repeat(Color.Empty, this.nrOfColorsPerPal).ToArray(), newPaletteName, 0, true, false));
                    this.subPalettes.Add(newPaletteName, newPalInfo);

                    // Rename current "add new" case to the new palette
                    this.removedPalettes.Remove(newPaletteName);
                    this.cmbPalettes.Items[this.subPalettes.Count - 1] = newPaletteName;
                    this.cmbPalettes.Items.Add(CREATENEW);
                }
                return;
            }
            this.lbSubPalettes.SelectedIndex = -1;
            Int32 items = this.ListSubPalettes((this.cmbPalettes.SelectedItem ?? String.Empty).ToString());
            //this.lbSubPalettes.Focus();
            if (items > 0)
                this.lbSubPalettes.SelectedIndex = 0;
        }

        private Int32 ListSubPalettes(String filename)
        {
            this.lbSubPalettes.Items.Clear();
            List<PaletteDropDownInfo> subPalettes;
            if (!this.subPalettes.TryGetValue(filename, out subPalettes))
                return 0;
            // Trim all unused entries off the end
            for (Int32 i = subPalettes.Count - 1; i >= 0; i--)
            {
                PaletteDropDownInfo curr = subPalettes[i];
                if (!String.IsNullOrEmpty(curr.Name))
                    break;
                subPalettes.RemoveAt(i);
            }
            this.lbSubPalettes.Items.AddRange(subPalettes.Select(x => (Object)x).ToArray());
            return subPalettes.Count;
        }

        private void BtnRename_Click(Object sender, EventArgs e)
        {
            String selectedPal = (this.cmbPalettes.SelectedItem ?? String.Empty).ToString();
            if (!this.subPalettes.ContainsKey(selectedPal))
                return;
            PaletteDropDownInfo currentPal = this.lbSubPalettes.SelectedItem as PaletteDropDownInfo;
            if (currentPal == null || !currentPal.SourceFile.Equals(selectedPal))
                return;
            DialogResult dr = DialogResult.No;
            String newPaletteName;
            do
            {
                newPaletteName = InputBox.Show("Sub-palette name:", this.title, currentPal.Name, FormStartPosition.CenterParent);
                if (String.Empty.Equals(newPaletteName))
                    dr = MessageBox.Show(this, "Giving the sub-palette an empty name will remove it\nfrom the FontEditor's palette listing!\n\nAre you sure you want to do that?", this.title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                else
                    dr = DialogResult.Yes;
            }
            while (dr == DialogResult.No && String.Empty.Equals(newPaletteName));
            if (newPaletteName == null)
                return;

            String palette = currentPal.SourceFile;
            Int32 entry = currentPal.Entry;
            currentPal.Name = newPaletteName;
            this.lbSubPalettes.Items[this.lbSubPalettes.SelectedIndex] = currentPal;
        }

        private void BtnAdd_Click(Object sender, EventArgs e)
        {
            if (this.lbSubPalettes.Items.Count >= this.nrOfSubPalettes)
            {
                MessageBox.Show(this, "Can't add more than " + this.nrOfSubPalettes + " sub-palette" + (this.nrOfSubPalettes == 1 ? String.Empty : "s") + " in a " + this.bpp + " BPP palette.", this.title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            String selectedPal = (this.cmbPalettes.SelectedItem ?? String.Empty).ToString();
            if (!this.subPalettes.ContainsKey(selectedPal))
                return;
            List<PaletteDropDownInfo> subList;
            if (!this.subPalettes.TryGetValue(selectedPal, out subList))
                return;
            
            DialogResult dr = DialogResult.No;
            String newPaletteName;
            do
            {
                newPaletteName = InputBox.Show("New sub-palette name:", this.title, String.Empty, FormStartPosition.CenterParent);
                if (String.Empty.Equals(newPaletteName))
                    dr = MessageBox.Show(this, "Giving the sub-palette an empty name will remove it\nfrom the FontEditor's palette listing!\n\nAre you sure you want to do that?", this.title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                else
                    dr = DialogResult.Yes;
            }
            while (dr == DialogResult.No && String.Empty.Equals(newPaletteName));
            if (newPaletteName == null)
                return;

            Int32 pos = this.lbSubPalettes.SelectedIndex + 1;
            subList.Insert(pos, new PaletteDropDownInfo(newPaletteName, this.bpp, Enumerable.Repeat(Color.Empty, this.nrOfColorsPerPal).ToArray(), selectedPal, pos, true, false));

            Int32 subListCount = subList.Count;
            for (Int32 i = 0; i < subListCount; ++i)
            {
                PaletteDropDownInfo info = subList[i];
                info.Entry = i;
                if (this.lbSubPalettes.Items.Count > i)
                    this.lbSubPalettes.Items[i] = info;
                else
                    this.lbSubPalettes.Items.Add(info);
            }
            this.lbSubPalettes.SelectedIndex = pos;
        }

        private void BtnRemove_Click(Object sender, EventArgs e)
        {
            String selectedPal = (this.cmbPalettes.SelectedItem ?? String.Empty).ToString();
            if (!this.subPalettes.ContainsKey(selectedPal))
                return;
            List<PaletteDropDownInfo> subList;
            if (!this.subPalettes.TryGetValue(selectedPal, out subList))
                return;
            PaletteDropDownInfo currentPal = this.lbSubPalettes.SelectedItem as PaletteDropDownInfo;
            if (currentPal == null || !currentPal.SourceFile.Equals(selectedPal))
                return;

            if (this.lbSubPalettes.Items.Count == 1)
            {
                String message;
                if (this.nrOfSubPalettes == 1)
                    message = "This will remove this palette!";
                else
                    message= "Removing a palette's last entry will remove the palette!";
                DialogResult dr = MessageBox.Show(this, message + " Are you sure?", this.title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.No)
                    return;
                this.subPalettes.Remove(selectedPal);
                this.removedPalettes.Add(selectedPal);
                Int32 index = this.cmbPalettes.SelectedIndex;
                this.cmbPalettes.SelectedIndex = -1;
                this.cmbPalettes.Items.RemoveAt(index);
                this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count > 1 ? 0 : -1;
                return;
            }
            DialogResult dr2 = MessageBox.Show(this, "Are you sure you want to remove this entry?", this.title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr2 == DialogResult.No)
                return;
            Int32 entry = currentPal.Entry;
            subList.RemoveAt(this.lbSubPalettes.SelectedIndex);
            this.lbSubPalettes.Items.RemoveAt(this.lbSubPalettes.SelectedIndex);
            Int32 subListCount = subList.Count;
            for (Int32 i = 0; i < subListCount; ++i)
            {
                PaletteDropDownInfo info = subList[i];
                info.Entry = i;
                this.lbSubPalettes.Items[i] = info;
            }
            Int32 indexToSelect = entry;
            if (this.lbSubPalettes.Items.Count <= indexToSelect)
                indexToSelect--;
            this.lbSubPalettes.SelectedIndex = indexToSelect;
        }

        private void LbSubPalettes_SelectedIndexChanged(Object sender, EventArgs e)
        {
            PaletteDropDownInfo selectedSubPal = this.lbSubPalettes.SelectedItem as PaletteDropDownInfo;
            Color[] colors;
            if (selectedSubPal == null)
                colors = Enumerable.Repeat(Color.Empty, this.nrOfColorsPerPal).ToArray();
            else
                colors = selectedSubPal.Colors;
            this.palSelectedSubPal.Palette = colors;
        }

        private void BtnCancel_Click(Object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnOk_Click(Object sender, EventArgs e)
        {
            if (this.paletteToSave != null)
            {
                String selectedPal = (this.cmbPalettes.SelectedItem ?? String.Empty).ToString();
                if (!this.subPalettes.Keys.Contains(selectedPal))
                {
                    if (this.immediateSave)
                        this.Close();
                    return;
                }
                PaletteDropDownInfo currentPal = this.lbSubPalettes.SelectedItem as PaletteDropDownInfo;
                if (currentPal == null || !currentPal.SourceFile.Equals(selectedPal))
                {
                    if (this.immediateSave)
                        this.Close();
                    return;
                }
                String palette = currentPal.SourceFile;
                Int32 entry = currentPal.Entry;
                if (palette == null || entry < 0 || entry >= this.nrOfSubPalettes)
                    return;
                FileInfo palfile = new FileInfo(Path.Combine(this.appPath, palette));
                if (palfile.Exists && palfile.Length == 0x300 && !currentPal.Colors.All(c => c.IsEmpty))
                {
                    MessageBoxButtons mbb = this.immediateSave ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.YesNo;
                    String message = "Overwrite this palette entry?";
                    if (this.immediateSave)
                        message += "\n\nPress \"No\" to pick another entry to save to.";

                    DialogResult dr = MessageBox.Show(message, FrmFontEditor.GetTitle(false), mbb, MessageBoxIcon.Warning);
                    if (dr == DialogResult.Cancel)
                    {
                        // Can only happen if an "overwrite?" prompt was cancelled. Abort completely.
                        this.DialogResult = DialogResult.No;
                        this.Close();
                        return;
                    }
                    if (dr == DialogResult.No)
                    {
                        // Only case in which "immediateSave" gets disabled.
                        if (this.immediateSave)
                            this.immediateSave = false;
                        return;
                    }
                }
                currentPal.Colors = this.paletteToSave.Colors;
                currentPal.ClearRevert();
                // so underlying layer can know which entry to select after reload.
                // Null + OK means "just clear revert on current".
                if (this.immediateSave)
                    this.paletteToSave = null;
                else
                    this.paletteToSave = currentPal;
            }
            // save all changes
            foreach (String palName in this.subPalettes.Keys)
            {
                Color[] thisFullPal;
                FileInfo pfile = new FileInfo(Path.Combine(this.appPath, palName));
                if (pfile.Exists && pfile.Length == 0x300)
                    thisFullPal = ColorUtils.GetEightBitColorPalette(ColorUtils.ReadSixBitPaletteFile(pfile.FullName));
                else
                    thisFullPal = Enumerable.Repeat(Color.Black, 256).ToArray();
                List<PaletteDropDownInfo> subPals = this.subPalettes[palName];
                foreach (PaletteDropDownInfo subpal in subPals)
                    Array.Copy(subpal.Colors, 0, thisFullPal, subpal.Entry * this.nrOfColorsPerPal, this.nrOfColorsPerPal);
                ColorUtils.WriteSixBitPaletteFile(thisFullPal, pfile.FullName);
                String iniPath = Path.Combine(this.appPath, palName.Substring(0, palName.Length - 4)) + ".ini";
                if (this.bpp == 8)
                {
                    if (File.Exists(iniPath))
                        File.Delete(iniPath);
                }
                else
                {
                    IniFile inifile = new IniFile(iniPath);
                    inifile.WriteBackMode = WriteMode.WRITE_ALL;
                    inifile.ClearSectionKeys(INISECTION);
                    for (Int32 i = 0; i < this.nrOfSubPalettes; ++i)
                    {
                        PaletteDropDownInfo subPal = subPals.Find(x => x.Entry == i);
                        if (subPal != null)
                            inifile.SetStringValue(INISECTION, i.ToString(), subPal.Name);
                    }
                    inifile.WriteIni();
                }   
            }
            foreach (String pal in this.removedPalettes)
            {
                String palFile = Path.Combine(this.appPath, pal);
                String iniFile = Path.Combine(this.appPath, pal.Substring(0, pal.Length - 4)) + ".ini";
                File.Delete(palFile);
                File.Delete(iniFile);
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void FrmManagePalettes_Load(Object sender, EventArgs e)
        {
            if (this.paletteToSave != null)
            {
                this.palReplaceBy.Palette = this.paletteToSave.Colors;
                String source = this.paletteToSave.SourceFile;
                Int32 entry = this.paletteToSave.Entry;
                if (!String.IsNullOrEmpty(source) && entry != -1 && this.cmbPalettes.Items.Contains(source))
                {
                    // Save an existing palette
                    Int32 index = this.cmbPalettes.Items.IndexOf(source);
                    if (index != -1)
                    {
                        this.cmbPalettes.SelectedIndex = index;
                        this.lbSubPalettes.SelectedIndex = entry;
                        this.immediateSave = true;
                        this.BtnOk_Click(sender, e);
                        // If immediateSave is still enabled, window should close right away.
                        // Abort function so it won't go on to the palette inits, sicne they'll fail.
                        if (this.immediateSave)
                            return;
                    }
                    // Continue after "cancel" was pressed.
                    this.Opacity = 1;
                }
                else
                {
                    // Save a generated palette
                    this.Opacity = 1;
                    this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count > 1 ? 0 : -1;
                }
                PalettePanel.InitPaletteControl(this.bpp, this.palReplaceBy, this.paletteToSave.Colors, 74);
            }
            else
            {
                // No save functions. Management only.
                this.lblReplaceBy.Visible = false;
                this.palReplaceBy.Visible = false;
                this.Text = "Manage palettes";
                this.cmbPalettes.SelectedIndex = this.cmbPalettes.Items.Count > 1 ? 0 : -1;
                this.Opacity = 1;
            }
            PalettePanel.InitPaletteControl(this.bpp, this.palSelectedSubPal, this.palSelectedSubPal.Palette, 74);
        }

        private void FrmManagePalettes_Shown(Object sender, EventArgs e)
        {
            if (this.cmbPalettes.SelectedIndex != -1)
                this.lbSubPalettes.Focus();
        }
    }
}
