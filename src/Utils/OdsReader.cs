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
                            // Include covered cells to maintain column alignment
                            var cells = row.SelectNodes("table:table-cell | table:covered-table-cell", nsmgr);
                            List<string> rowValues = new List<string>();
                            
                            foreach (XmlNode cell in cells)
                            {
                                int repeat = 1;
                                if (cell.Attributes["table:number-columns-repeated"] != null)
                                {
                                    int.TryParse(cell.Attributes["table:number-columns-repeated"].Value, out repeat);
                                }

                                string cellValue = (cell.Name == "table:covered-table-cell") ? "" : GetCellValue(cell, nsmgr);

                                // Optimization logic...
                                if (string.IsNullOrEmpty(cellValue) && repeat > 10)
                                {
                                    if (rowValues.Count > 100) 
                                    {
                                        continue; 
                                    }
                                    if(repeat > 20) repeat = 20; 
                                }

                                if (repeat > 1000) repeat = 1000; 

                                for (int i = 0; i < repeat; i++)
                                {
                                    if (rowValues.Count >= 256) break;
                                    rowValues.Add(cellValue);
                                }
                            }

                            // Now add to DataTable
                            while (rowValues.Count > dt.Columns.Count)
                            {
                                dt.Columns.Add("Column" + (dt.Columns.Count + 1));
                            }

                            // Pad row if shorter than max columns (optional, or just add what we have)
                            DataRow dr = dt.NewRow();
                            for(int k=0; k<rowValues.Count; k++)
                            {
                                dr[k] = rowValues[k];
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
            // Robust Value Extraction
            // 1. Check for text:p (paragraphs) - join multiple lines if present
            var paragraphs = cell.SelectNodes("text:p", nsmgr);
            if (paragraphs != null && paragraphs.Count > 0)
            {
                List<string> lines = new List<string>();
                foreach (XmlNode p in paragraphs)
                {
                    // Check for nested spans too
                    lines.Add(p.InnerText);
                }
                string combined = string.Join("\n", lines).Trim();
                if (!string.IsNullOrEmpty(combined)) return combined;
            }

            // 2. Check attributes for value types (float, date, etc)
            if (cell.Attributes["office:value"] != null) return cell.Attributes["office:value"].Value;
            if (cell.Attributes["office:date-value"] != null) return cell.Attributes["office:date-value"].Value;
            if (cell.Attributes["office:time-value"] != null) return cell.Attributes["office:time-value"].Value;
            if (cell.Attributes["office:string-value"] != null) return cell.Attributes["office:string-value"].Value;
            if (cell.Attributes["office:boolean-value"] != null) return cell.Attributes["office:boolean-value"].Value;

            // 3. Last resort: InnerText (might match styles or other xml)
            return cell.InnerText.Trim();
        }
    }
}
