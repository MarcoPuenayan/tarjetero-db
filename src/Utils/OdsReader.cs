using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace TarjeteroApp.Utils
{
    public static class OdsReader
    {
        public static DataTable ReadOds(string filePath, string sheetName = null)
        {
            DataTable dt = new DataTable();
            
            try 
            {
                using (ZipArchive zip = ZipFile.OpenRead(filePath))
                {
                    var contentEntry = zip.GetEntry("content.xml");
                    if (contentEntry == null)
                        throw new FileNotFoundException("Invalid ODS file: content.xml not found.");

                    using (Stream stream = contentEntry.Open())
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(stream);

                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                        nsmgr.AddNamespace("office", "urn:oasis:names:tc:opendocument:xmlns:office:1.0");
                        nsmgr.AddNamespace("table", "urn:oasis:names:tc:opendocument:xmlns:table:1.0");
                        nsmgr.AddNamespace("text", "urn:oasis:names:tc:opendocument:xmlns:text:1.0");

                        // Get the requested sheet or the first one
                        XmlNode tableNode = null;
                        XmlNodeList tables = doc.SelectNodes("//table:table", nsmgr);
                        
                        if (tables.Count == 0) return dt;

                        if (!string.IsNullOrEmpty(sheetName))
                        {
                            foreach (XmlNode t in tables)
                            {
                                if (t.Attributes["table:name"]?.Value == sheetName)
                                {
                                    tableNode = t;
                                    break;
                                }
                            }
                        }
                        
                        if (tableNode == null) tableNode = tables[0]; // fallback to first

                        var rows = tableNode.SelectNodes("table:table-row", nsmgr);
                        
                        // First pass: determine max columns
                        int maxCols = 0;
                        /* 
                           Parsing ODS is tricky because of "number-columns-repeated".
                           We'll assume a simplified table structure where first row is header.
                        */

                        // To be safer, just process rows and expand DataTable as needed.
                        
                        foreach (XmlNode row in rows)
                        {
                            DataRow dr = dt.NewRow();
                            var cells = row.SelectNodes("table:table-cell", nsmgr);
                            
                            int columnIndex = 0;
                            foreach (XmlNode cell in cells)
                            {
                                int repeat = 1;
                                if (cell.Attributes["table:number-columns-repeated"] != null)
                                {
                                    int.TryParse(cell.Attributes["table:number-columns-repeated"].Value, out repeat);
                                }

                                string cellValue = GetCellValue(cell, nsmgr);

                                // Optimization: If cell is empty and repeats many times, it's likely filling the row end.
                                // We ignore massive repeats of empty content to prevent 65k columns issue.
                                if (string.IsNullOrEmpty(cellValue) && repeat > 10)
                                {
                                    // Only process up to existing columns count (fill blanks) or skip
                                    // If we are far right, just stop processing this cell's repetitions
                                    if (columnIndex > 100) 
                                    {
                                        continue; 
                                    }
                                    // Cap repeat if it's small enough to be relevant but large enough to be annoying
                                    if(repeat > 20) repeat = 20; 
                                }

                                // Hard limit for safety
                                if (repeat > 1000) repeat = 1000; 

                                for (int i = 0; i < repeat; i++)
                                {
                                    if (columnIndex >= 256) break; // Hard Limit for columns

                                    // Ensure column exists
                                    while (columnIndex >= dt.Columns.Count)
                                    {
                                        dt.Columns.Add("Column" + (dt.Columns.Count + 1));
                                    }

                                    DataRow targetRow; 
                                    // If we are just building struct, we use `dr`. 
                                    // Wait, we can't add row until it aligns with schema? 
                                    // No, DataTable allows adding row with missing values, but not extra values.
                                    // We'll dynamically add columns.
                                    
                                    dr[columnIndex] = cellValue;
                                    columnIndex++;
                                }
                            }
                            dt.Rows.Add(dr);
                        }
                    }
                }

                // Promote First Row to Header if useful
                if (dt.Rows.Count > 0)
                {
                    bool possibleHeader = true;
                    for(int i=0; i<dt.Columns.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(dt.Rows[0][i].ToString()))
                            possibleHeader = false;
                    }

                    if (possibleHeader)
                    {
                        for(int i=0; i<dt.Columns.Count; i++)
                        {
                            string header = dt.Rows[0][i].ToString();
                            if(dt.Columns.Contains(header)) header += "_"+i;
                            dt.Columns[i].ColumnName = header;
                        }
                        dt.Rows.RemoveAt(0);
                    }
                }

                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading ODS file: " + ex.Message, ex);
            }
        }

        private static string GetCellValue(XmlNode cell, XmlNamespaceManager nsmgr)
        {
            // Usually value is in office:value" or text:p child
            // Priority: text:p content.
            // But sometimes values are dates or numbers.
            
            var textNode = cell.SelectSingleNode("text:p", nsmgr);
            if (textNode != null) return textNode.InnerText;
            
            if (cell.Attributes["office:value"] != null)
                return cell.Attributes["office:value"].Value;
                
            return "";
        }
    }
}
