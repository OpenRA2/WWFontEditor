﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Nyerguds.Util.Csv
{
    public class CsvParser
    {
        public static List<T> ParseCsvInfo<T>(List<String[]> split) where T : new()
        {
            // No template row, or only a template row but no data. Abort.
            if (split.Count < 2)
                return new List<T>();
            String[] templateRow = split[0];
            // Create a dictionary of rows and their index in the file data.
            Dictionary<String, Int32> columnIndexing = new Dictionary<String, Int32>();
            Int32 templateLength = templateRow.Length;
            for (Int32 i = 0; i < templateLength; ++i)
            {
                // ToUpperInvariant is optional, of course. You could have case sensitive headers.
                String colHeader = templateRow[i].Trim().ToUpperInvariant();
                if (!columnIndexing.ContainsKey(colHeader))
                    columnIndexing.Add(colHeader, i);
            }
            // Prepare the arrays of property parse info. We set the length
            // so the highest found column index exists in it.
            Int32 numCols = columnIndexing.Values.Max() + 1;
            // Actual property to fill in
            PropertyInfo[] properties = new PropertyInfo[numCols];
            // Regex to validate the string before parsing
            Regex[] propValidators = new Regex[numCols];
            // Type converters for automatic parsing
            TypeConverter[] propconverters = new TypeConverter[numCols];
            // go over the properties of the given type, see which ones have a
            // CsvColumnAttribute, and put these in the list at their CSV index.
            PropertyInfo[] propInfo = typeof(T).GetProperties();
            Int32 propInfoLen = propInfo.Length;
            for (Int32 i = 0; i < propInfoLen; ++i)
            {
                PropertyInfo prop = propInfo[i];
                Object[] attrs = prop.GetCustomAttributes(true);
                Int32 attrLen = attrs.Length;
                for (Int32 j = 0; j < attrLen; ++j)
                {
                    CsvColumnAttribute csvAttr = attrs[j] as CsvColumnAttribute;
                    if (csvAttr == null)
                        continue;
                    Int32 index;
                    if (!columnIndexing.TryGetValue(csvAttr.Name.ToUpperInvariant(), out index))
                    {
                        // If no valid column is found, and the regex for this property
                        // does not allow an empty value, then all lines are invalid.
                        if (!csvAttr.ValidationRegex.IsMatch(String.Empty))
                            return new List<T>();
                        // No valid column found: ignore this property.
                        break;
                    }
                    properties[index] = prop;
                    propValidators[index] = csvAttr.ValidationRegex;
                    // Automatic type converter. This function could be enhanced by giving a
                    // list of custom converters as extra argument and checking those first.
                    propconverters[index] = TypeDescriptor.GetConverter(prop.PropertyType);
                    break; // Only handle one CsvColumnAttribute per property.
                }
            }
            List<T> objList = new List<T>();
            // start from 1 since the first line is the template with the column names
            Int32 splitCount = split.Count;
            for (Int32 i = 1; i < splitCount; ++i)
            {
                Boolean abortLine = false;
                String[] line = split[i];
                // make new object of the given type
                T obj = new T();
                Int32 propLen = properties.Length;
                for (Int32 col = 0; col < propLen; ++col)
                {
                    PropertyInfo prop = properties[col];
                    // this can be null if the column was not found but wasn't required.
                    if (prop == null)
                        continue;
                    // It is possible a line is not long enough to contain all columns.
                    String curVal = col < line.Length ? line[col] : String.Empty;
                    // check validity. Abort buildup of this object if not valid.
                    Boolean valid = propValidators[col].IsMatch(curVal);
                    if (!valid)
                    {
                        // Add logging here? We have the line and column index.
                        abortLine = true;
                        break;
                    }
                    // Automated parsing. Always use nullable types for nullable properties.
                    Object value = propconverters[col].ConvertFromString(curVal);
                    prop.SetValue(obj, value, null);
                }
                if (!abortLine)
                    objList.Add(obj);
            }
            return objList;
        } 
    }
}