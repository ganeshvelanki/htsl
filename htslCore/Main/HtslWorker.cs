﻿using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using SpreadsheetLight;
using htslCore.Model;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Spreadsheet;
using System.IO;
using htslCore.Internal.Converters;

namespace htslCore.Worker
{
    /// <summary>
    /// Internal HTML To EXCEL Worker class
    /// </summary>
    internal class HtslWorker
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HtslWorker"/> class.
        /// </summary>
        public HtslWorker()
        {
            this.tableCellStyle = new Dictionary<string, Dictionary<string, HtslCellStyle>>();
            this.tableRowStyle = new Dictionary<string, Dictionary<int, HtslCellStyle>>();
            this.HtmlConverterHelper = new HtmlConverterHelper();
        }

        /// <summary>
        /// Gets or sets the table row style.
        /// </summary>
        /// <value>
        /// The table row style.
        /// </value>
        private Dictionary<string, Dictionary<int, HtslCellStyle>> tableRowStyle { get; set; }

        /// <summary>
        /// Gets or sets the table cell style.
        /// </summary>
        /// <value>
        /// The table cell style.
        /// </value>
        private Dictionary<string, Dictionary<string, HtslCellStyle>> tableCellStyle { get; set; }

        /// <summary>
        /// Gets or sets the HTML converter helper.
        /// </summary>
        /// <value>
        /// The HTML converter helper.
        /// </value>
        private HtmlConverterHelper HtmlConverterHelper { get; set; }

        /// <summary>
        /// Determines whether [is HTML valid] [the specified HTML string].
        /// </summary>
        /// <param name="htmlStr">The HTML string.</param>
        /// <returns>
        ///   <c>true</c> if [is HTML valid] [the specified HTML string]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsHtmlValid(string htmlStr)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlStr);
            return doc.ParseErrors.Count() > 0;
        }

        /// <summary>
        /// Converts the HTML to EXCEL.
        /// </summary>
        /// <returns>Converted document as byte array.</returns>
        public byte[] ConvertHTMLToXL(string htmlStr)
        {
            //Load html document
            var htmlDocument = this.HtmlConverterHelper.GetNewHtmlDocument(htmlStr);
            int rowIndex = 1, colIndex = 1;
            MemoryStream MemoryStream = new MemoryStream();

            var tables = htmlDocument.DocumentNode.Descendants("table").ToList();

            tables.ForEach(table =>
            {
                this.SetSLStyles(table);
            });

            using (var document = new SLDocument())
            {
                var rows = tables[0].Descendants("tr");

                document.RenameWorksheet(document.GetCurrentWorksheetName(), tables[0].Id);

                foreach(var row in rows)
                {
                    var cells = row.Descendants("td");
                    var rowStyles = this.tableRowStyle[tables[0].Id];
                    var cellStyles = this.tableCellStyle[tables[0].Id];

                    colIndex = 1;
                    foreach(var cell in cells)
                    {
                        HtslCellStyle style;
                        var styleExists = cellStyles.TryGetValue(string.Format(HtslConstants.RowColPlaceHolder, rowIndex, colIndex), out style);
                        document.SetCellValue(rowIndex, colIndex, cell.InnerText);

                        if (styleExists)
                        {
                            document.SetCellStyle(rowIndex, colIndex, style as SLStyle);
                        }

                        ++colIndex;
                    }

                    ++rowIndex;
                }

                document.SaveAs(MemoryStream);
            }

            return MemoryStream.ToArray();
        }

        /// <summary>
        /// Sets the sl styles.
        /// </summary>
        /// <param name="htmlTableNode">The HTML Table node.</param>
        private void SetSLStyles(HtmlNode htmlTableNode)
        {
            //Assing an Id to table if not available
            if(htmlTableNode.Id == string.Empty)
            {
                htmlTableNode.SetAttributeValue("id", Guid.NewGuid().ToString());
            }

            var rows = htmlTableNode.Descendants("tr").ToList();
            int rowIndex = 1;
            var _rowStyles = new Dictionary<int, HtslCellStyle>();
            var _cellStyles = new Dictionary<string, HtslCellStyle>();
            rows.ForEach(row =>
            {
                var rowStyle = row.GetAttributeValue("style", null);

                if(rowStyle != null)
                {
                    _rowStyles.Add(rowIndex, this.ProcessStyleProperties(rowStyle));
                }

                var cells = row.Descendants("td").ToList();

                int cellIndex = 1;

                cells.ForEach(cell =>
                {
                    var cellStyle = cell.GetAttributeValue("style", null);

                    if (cellStyle != null)
                    {
                        _cellStyles.Add(string.Format(HtslConstants.RowColPlaceHolder, rowIndex, cellIndex), this.ProcessStyleProperties(cellStyle));
                    }

                    ++cellIndex;
                });

                ++rowIndex;
            });

            this.tableRowStyle.Add(htmlTableNode.Id, _rowStyles);
            this.tableCellStyle.Add(htmlTableNode.Id, _cellStyles);
        }

        /// <summary>
        /// Processes the style properties.
        /// </summary>
        /// <param name="styleStr">The style.</param>
        private HtslCellStyle ProcessStyleProperties(string styleStr)
        {
            string[] cssProperties = styleStr.Split(';');
            HtslCellStyle slStyle = new HtslCellStyle();

            foreach (var prop in cssProperties)
            {
                var stylePair = prop.Split(':');

                switch (stylePair[0].Trim())
                {
                    case "width":
                        slStyle.Width = Convert.ToDouble(Regex.Replace(stylePair[1], HtslConstants.NumberOnlyRegex, ""));
                        break;

                    case "height":
                        slStyle.Height = Convert.ToDouble(Regex.Replace(stylePair[1], HtslConstants.NumberOnlyRegex, ""));
                        break;

                    case "border":
                        this.SetSLBorder(stylePair[1], slStyle);
                        break;
                    default:
                        break;
                }
            }

            return slStyle;
        }

        /// <summary>
        /// Sets the Cell border.
        /// </summary>
        /// <param name="borderStr">The border string.</param>
        /// <param name="slStyle">The Cell style.</param>
        private void SetSLBorder(string borderStr, HtslCellStyle slStyle)
        {
            var borderSplit = borderStr.Split(' ');
            if(borderSplit.Length == 3)
            {
                slStyle.Border.SetHorizontalBorder(BorderStyleValues.Thin, (System.Drawing.Color)System.Drawing.ColorTranslator.FromHtml(borderSplit[2]));
                slStyle.Border.SetVerticalBorder(BorderStyleValues.Thin, (System.Drawing.Color)System.Drawing.ColorTranslator.FromHtml(borderSplit[2]));
            }
        }
    }
}
