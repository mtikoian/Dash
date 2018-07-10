using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace Dash.Models
{
    public class ExportData : BaseModel
    {
        public string ContentType { get; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public string FileName { get; set; }
        public Report Report { get; set; }

        public string FormattedFileName
        {
            get
            {
                var formattedName = FileName;
                Array.ForEach(Path.GetInvalidFileNameChars(), c => formattedName = formattedName.Replace(c.ToString(), String.Empty));
                return $"{formattedName}.xlsx";
            }
        }

        /// <summary>
        /// Create the spreadsheet.
        /// </summary>
        public byte[] Stream()
        {
            if (!Report.ReportColumn.Any(c => c.SortDirection != null))
            {
                // make sure at least one column is sorted
                Report.ReportColumn[0].SortDirection = "asc";
                Report.ReportColumn[0].SortOrder = 1;
                DbContext.Save(Report.ReportColumn[0]);
            }

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(Report.Name);
                var columns = Report.Dataset.DatasetColumn.ToDictionary(j => j.Id, j => j);
                var table = new DataTable();
                Report.ReportColumn.ForEach(x => table.Columns.Add(columns[x.ColumnId]?.Title ?? "", typeof(string)));
                FileName = FileName.IsEmpty() ? Report.Name : FileName;

                dynamic result = Report.GetData(AppConfig, 0, Int32.MaxValue, false);
                foreach (IDictionary<string, object> row in result.Rows)
                {
                    var dataRow = table.NewRow();
                    Report.ReportColumn.ForEach(x => dataRow[columns[x.ColumnId].Title] = row[columns[x.ColumnId].Alias] == null ? "" : row[columns[x.ColumnId].Alias].ToString());
                    table.Rows.Add(dataRow);
                }
                worksheet.Cells["A1"].LoadFromDataTable(table, true);

                // format the header
                using (var rng = worksheet.Cells[String.Format("A1:{0}1", Report.ReportColumn.Count.ToExcelColumn())])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;  // Set Pattern for the background to Solid
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));  // Set color to dark blue
                    rng.Style.Font.Color.SetColor(Color.White);
                }

                worksheet.Cells["A1:" + Report.ReportColumn.Count.ToExcelColumn() + table.Rows.Count].AutoFitColumns();

                return package.GetAsByteArray();
            }
        }
    }
}
