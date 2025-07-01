﻿using System;

namespace Nyerguds.Util
{
    public class SaveOption
    {
        public SaveOption(String code, SaveOptionType type, String UiString, String saveData)
            : this(code, type, UiString, null, saveData) { }

        public SaveOption(String code, SaveOptionType type, String UiString, String initValue, String saveData)
        {
            this.Code = code;
            this.Type = type;
            this.UiString = UiString;
            this.InitValue = initValue;
            this.SaveData = saveData;
        }

        /// <summary>Code to easily retrieve this option</summary>
        public String Code { get; private set; }
        /// <summary>Data type</summary>
        public SaveOptionType Type { get; private set; }
        /// <summary>String to show on the UI for this option</summary>
        public String UiString { get; private set; }
        /// <summary>Initialisation value. Used differently by all types.</summary>
        public String InitValue { get; private set; }
        /// <summary>The value of this option. Fill this in in advance to give a default value.</summary>
        public String SaveData { get; set; }

        public static String GetSaveOptionValue(SaveOption[] list, String code)
        {
            Int32 listLen = list.Length;
            for (Int32 i = 0; i < listLen; i++)
            {
                SaveOption option = list[i];
                if (String.Equals(option.Code, code, StringComparison.InvariantCultureIgnoreCase))
                    return option.SaveData;
            }
            return null;
        }
    }

    public enum SaveOptionType
    {
        /// <summary>Simple numeric input. InitValue can be left empty, or give a comma-separated minimum and/or maximum in the format "min,max".</summary>
        Number,
        /// <summary>Checkbox. Data value should always be either "0" and "1".</summary>
        Boolean,
        /// <summary>Free text field. If InitValue is specified, it limits the possible input characters to the characters inside the given string.</summary>
        String,
        /// <summary>Dropdown. Use InitValue to set a comma-separated list of options. Returns the chosen index (0-based) as string. SaveData can be used to set a default index.</summary>
        ChoicesList,
        /// <summary>File selector. Use InitValue to specify a File Open mask.</summary>
        FileOpen,
        /// <summary>Additional file to be written. Use InitValue to specify a File Save mask.</summary>
        FileSave,
        /// <summary>Folder selector.</summary>
        Folder,
    }
}
