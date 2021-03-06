﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using htslCore;
using System.Xml;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace htslMain
{
    /// <summary>
    /// Main Entry point
    /// </summary>
    class Program
    {
        /// <summary>
        /// Mains the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            var runner = new htslCore.Main.HtslConverter();
            //var isValidHtml = runner.AssertHTMLSource("<html><head></html>");
            var stream = runner.ConvertToExcel(HtmlTemplateStore.SampleTemplate_With_RowStyle.Replace("\r\n", ""));
            var memoryStream = new MemoryStream(stream);
            var fileStream = new FileStream(Directory.GetCurrentDirectory() + "\\sample.xlsx", FileMode.Create, FileAccess.Write);

            memoryStream.WriteTo(fileStream);
            fileStream.Close();
            memoryStream.Close();
        }
    }
}
