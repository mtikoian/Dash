using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;

namespace Dash.Models
{
    public class ExportData : BaseModel
    {
        public Report Report { get; set; }

        /// <summary>
        /// Create the spreadsheet and stream to the response.
        /// </summary>
        public void Stream()
        {
            if (!Report.ReportColumn.Any(c => c.SortDirection != null))
            {
                // make sure at least one column is sorted
                Report.ReportColumn[0].SortDirection = "asc";
                Report.ReportColumn[0].SortOrder = 1;
                DbContext.Save(Report.ReportColumn[0]);
            }

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(Report.Name);

                var columns = Report.Dataset.DatasetColumn.ToDictionary(j => j.Id, j => j);
                DataTable table = new DataTable();
                Report.ReportColumn.ForEach(x => table.Columns.Add(columns[x.ColumnId]?.Title ?? "", typeof(string)));

                dynamic result = Report.GetData(0, Int32.MaxValue);
                foreach (IDictionary<string, object> row in result.Rows)
                {
                    var dataRow = table.NewRow();
                    Report.ReportColumn.ForEach(x => dataRow[columns[x.ColumnId].Title] = row[columns[x.ColumnId].Alias] == null ? "" : row[columns[x.ColumnId].Alias].ToString());
                    table.Rows.Add(dataRow);
                }
                worksheet.Cells["A1"].LoadFromDataTable(table, true);

                // format the header
                using (ExcelRange rng = worksheet.Cells[String.Format("A1:{0}1", Report.ReportColumn.Count.ToExcelColumn())])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;  // Set Pattern for the background to Solid
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));  // Set color to dark blue
                    rng.Style.Font.Color.SetColor(Color.White);
                }

                worksheet.Cells["A1:" + Report.ReportColumn.Count.ToExcelColumn() + table.Rows.Count].AutoFitColumns();

                var fileName = Report.Name;
                Array.ForEach(Path.GetInvalidFileNameChars(), c => fileName = fileName.Replace(c.ToString(), String.Empty));

                // Write it back to the client
                HttpContext.Current.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                HttpContext.Current.Response.AddHeader("content-disposition", "attachment; filename=" + fileName + ".xlsx");
                HttpContext.Current.Response.BinaryWrite(package.GetAsByteArray());

                // Short-circuit this ASP.NET request and end. Short-circuiting prevents other modules from adding/interfering with the output.
                HttpContext.Current.ApplicationInstance.CompleteRequest();
                HttpContext.Current.Response.End();
            }
        }
    }
}