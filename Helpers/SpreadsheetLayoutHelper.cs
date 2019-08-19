using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace SawyerSight.Web.Helpers
{
    public static class SpreadsheetLayoutHelper
    {
        public static int FormatWaterFiscalYearHeader(Telerik.Web.Spreadsheet.Worksheet currentSheet, int cellOffsetCounter, Dictionary<string, List<string>> years, string categoryName)
        {
            int revStart = cellOffsetCounter;
            foreach (var year in years)
            {
                currentSheet.Rows[2].Cells.Add(new Telerik.Web.Spreadsheet.Cell() { Value = categoryName, Index = cellOffsetCounter, Bold = true, TextAlign = "center", BorderBottom = new Telerik.Web.Spreadsheet.BorderStyle() { Color = "black", Size = 2 } });
                if (year.Value.Count<12)
                {
                    currentSheet.Rows[3].Cells[cellOffsetCounter].Value = "YTD" + GetMonthNameByMonthNumber(year.Value.Select(x=>int.Parse(x)).ToList().Max())+year.Key.ToString().Substring(2);
                }
                else
                {
                    currentSheet.Rows[3].Cells[cellOffsetCounter].Value = "FY" + year.Key.ToString().Substring(2);
                }
                currentSheet.Rows[3].Cells[cellOffsetCounter].Bold = true;
                currentSheet.Rows[3].Cells[cellOffsetCounter].TextAlign = "center";
                cellOffsetCounter++;
            }
            var startLetter = SawyerSight.Web.Helpers.SpreadsheetLayoutHelper.GetExcelColumnName(revStart + 1);
            var endLetter = SawyerSight.Web.Helpers.SpreadsheetLayoutHelper.GetExcelColumnName(cellOffsetCounter);
            currentSheet.AddMergedCells($"{startLetter}3:{endLetter}3");

            return cellOffsetCounter;
        }

        public static string GetMonthNameByMonthNumber(int month)
        {
            return new DateTime(1900, month, 1).ToString("MMM", CultureInfo.InvariantCulture);
        }

        public static string GetExcelColumnName(int columnNumber)
        {
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }
    }
}