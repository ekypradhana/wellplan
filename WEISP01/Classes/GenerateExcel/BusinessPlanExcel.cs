using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.IO;
using System.Data;
using System.Drawing;

using Aspose.Cells;
using ECIS.Client.WEIS;
using ECIS.Core;

namespace ECIS.AppServer.Classes.GenerateExcel
{

    public class BusinessPlanExcel
    {

        private Style HeaderStyle(Workbook wb, Color BackgroundColor)
        {
            Style stlHeader;
            //stlHeader = wb.Styles[wb.Styles.Add()];
            stlHeader = wb.CreateStyle();
            stlHeader.Font.Name = "Calibri";
            stlHeader.Font.IsBold = true;
            stlHeader.Font.Size = 11;
            stlHeader.Pattern = BackgroundType.Solid;
            stlHeader.ForegroundColor = BackgroundColor;
            stlHeader.HorizontalAlignment = TextAlignmentType.Center;
            stlHeader.VerticalAlignment = TextAlignmentType.Bottom;
            stlHeader.Borders.SetColor(Color.Black);
            stlHeader.Borders.SetStyle(CellBorderType.Medium);
            stlHeader.Borders.DiagonalStyle = CellBorderType.None;
            stlHeader.IsTextWrapped = true;

            return stlHeader;
        }

        private Style NumericStyle(Workbook wb)
        {
            Style stlNumContent;
            stlNumContent = wb.CreateStyle() ;
            stlNumContent.Font.Name = "Calibri";
            stlNumContent.Font.IsBold = false;
            stlNumContent.Font.Size = 11;
            stlNumContent.HorizontalAlignment = TextAlignmentType.Right;
            stlNumContent.VerticalAlignment = TextAlignmentType.Center;
            stlNumContent.Borders.SetColor(Color.Black);
            stlNumContent.Borders.SetStyle(CellBorderType.Thin);
            stlNumContent.Borders.DiagonalStyle = CellBorderType.None;
            stlNumContent.IsTextWrapped = true;

            return stlNumContent;
        }

        private Style CharStyle(Workbook wb)
        {
            Style stlCharContent;
            stlCharContent = wb.CreateStyle();
            stlCharContent.Font.Name = "Calibri";
            stlCharContent.Font.IsBold = false;
            stlCharContent.Font.Size = 11;
            stlCharContent.HorizontalAlignment = TextAlignmentType.Left;
            stlCharContent.VerticalAlignment = TextAlignmentType.Center;
            stlCharContent.Borders.SetColor(Color.Black);
            stlCharContent.Borders.SetStyle(CellBorderType.Thin);
            stlCharContent.Borders.DiagonalStyle = CellBorderType.None;
            stlCharContent.IsTextWrapped = true;

            return stlCharContent;
        }

        private string NumericFormatting(object Value, bool isCurrency = false)
        {
            if (Value == null)
            {
                if (isCurrency)
                    return String.Format("{0:C0}", "-");
                else
                    return String.Format("{0:N0}", "-");
            }
            else
            {
                if (Value.ToString() == string.Empty)
                {
                    if (isCurrency)
                        return String.Format("{0:C0}", "-");
                    else
                        return String.Format("{0:N0}", "-");
                }
                else
                {
                    if (isCurrency)
                        return String.Format("{0:C0}", Value);
                    else
                        return String.Format("{0:N0}", Value);
                }
            }
        }

        enum ColumnWidth
        {
            XSmall,
            Small,
            Medium,
            Large,
            XLarge
        }

        private double SetColumnWidth(ColumnWidth e)
        {
            switch (e)
            {
                case ColumnWidth.XSmall: return 7; 
                case ColumnWidth.Small: return 12; 
                case ColumnWidth.Medium: return 18;
                case ColumnWidth.Large: return 25; 
                case ColumnWidth.XLarge: return 32;
                default: return 18; 
            }
        }

        private StyleFlag FlagStyle()
        {
            return new StyleFlag() { 
                Font = true,
                CellShading = true,
                Borders = true,
                WrapText = true,
                HorizontalAlignment = true,
                VerticalAlignment = true
            };            
        }

        public bool GenerateExcel(IList<BizPlanActivity> Activities, string AbsolutePath)
        {
            try
            {
                Workbook wb = new Workbook();
                Worksheet ws = wb.Worksheets[0];
                ws.Name = "Rig Activities";
                ws.Cells.StandardWidth = SetColumnWidth(ColumnWidth.Small);

                #region header
                Range range = ws.Cells.CreateRange("A3", "A3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.XSmall);
                range.Value = "Region";

                range = ws.Cells.CreateRange("B3", "B3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Rig Type";

                range = ws.Cells.CreateRange("C3", "C3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Medium);
                range.Value = "Rig Name";

                range = ws.Cells.CreateRange("D3", "D3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Operating Unit";

                range = ws.Cells.CreateRange("E3", "E3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Performance Unit";

                range = ws.Cells.CreateRange("F3", "F3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "GM Engr";

                range = ws.Cells.CreateRange("G3", "G3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "GM Ops";

                range = ws.Cells.CreateRange("H3", "H3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Responsible Engineer";

                range = ws.Cells.CreateRange("I3", "I3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Asset Name";

                range = ws.Cells.CreateRange("J3", "J3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Large);
                range.Value = "Project Name";

                range = ws.Cells.CreateRange("K3", "K3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.XLarge);
                range.Value = "Well Name";

                range = ws.Cells.CreateRange("L3", "L3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.XSmall);
                range.Value = "Working Interest";

                range = ws.Cells.CreateRange("M3", "M3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.XSmall);
                range.Value = "Firm or Option";

                range = ws.Cells.CreateRange("N3", "N3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Large);
                range.Value = "Activity Type \n (PTW Bucket)";

                range = ws.Cells.CreateRange("O3", "O3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Large);
                range.Value = "Activity Desc. \n (Rig Sec desc)";

                range = ws.Cells.CreateRange("P3", "P3");
                range.ApplyStyle(HeaderStyle(wb, Color.OrangeRed), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.XSmall);
                range.Value = "Risk Flag";

                range = ws.Cells.CreateRange("Q3", "Q3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.XSmall);
                range.Value = "Activity Desc \n (PTW Est name)";

                range = ws.Cells.CreateRange("R2", "S2");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.Merge();
                range.Value = "UA Rig Sequence";

                range = ws.Cells.CreateRange("T2", "V2");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.Merge();
                range.Value = "OPS Sequence - Hicks";

                range = ws.Cells.CreateRange("W2", "Z2");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.Merge();
                range.Value = "Planning Sequence - Tran";

                range = ws.Cells.CreateRange("AA2", "AB2");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.Merge();
                range.Value = "Cost Phasing Seq. - Sirmon";

                range = ws.Cells.CreateRange("AE2", "AH2");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.Merge();
                range.Value = "Start & Finish (Using Planning Sequence)";

                range = ws.Cells.CreateRange("AI2", "AL2");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.Merge();
                range.Value = "Start & Finish (Using Operations Seq)";

                range = ws.Cells.CreateRange("R3", "R3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "ID";

                range = ws.Cells.CreateRange("S3", "S3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Medium);
                range.Value = "Description";

                range = ws.Cells.CreateRange("T3", "T3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "ops Dur";

                range = ws.Cells.CreateRange("U3", "U3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Ops Start";

                range = ws.Cells.CreateRange("V3", "V3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Ops Finish";

                range = ws.Cells.CreateRange("W3", "W3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "PS Start";

                range = ws.Cells.CreateRange("X3", "X3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "PS Finish";

                range = ws.Cells.CreateRange("Y3", "Y3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "PS Dur";

                range = ws.Cells.CreateRange("Z3", "Z3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Delta";

                range = ws.Cells.CreateRange("AA3", "AA3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Ph Start";

                range = ws.Cells.CreateRange("AB3", "AB3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Ph Finish";

                range = ws.Cells.CreateRange("AC3", "AC3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Dur Calc";

                range = ws.Cells.CreateRange("AD3", "AD3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Dur Check";

                range = ws.Cells.CreateRange("AE3", "AE3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Start Year (PI)";

                range = ws.Cells.CreateRange("AF3", "AF3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Start Month (PI)";

                range = ws.Cells.CreateRange("AG3", "AG3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Finish Year (PI)";

                range = ws.Cells.CreateRange("AH3", "AH3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Finish Quarter/Year (PI)";

                range = ws.Cells.CreateRange("AI3", "AI3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Start Year (Ops)";

                range = ws.Cells.CreateRange("AJ3", "AJ3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Start Month (Ops)";

                range = ws.Cells.CreateRange("AK3", "AK3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Finish Year (Ops)";

                range = ws.Cells.CreateRange("AL3", "AL3");
                range.ApplyStyle(HeaderStyle(wb, Color.LightSkyBlue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Finish Quarter/Year (Ops)";

                range = ws.Cells.CreateRange("AM3", "AM3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Medium);
                range.Value = "Funding Type";

                range = ws.Cells.CreateRange("AN3", "AN3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Grouping (Expl, Major Proj, Other)";

                range = ws.Cells.CreateRange("AO3", "AO3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Medium);
                range.Value = "Escalation Group";

                range = ws.Cells.CreateRange("AP3", "AP3");
                range.ApplyStyle(HeaderStyle(wb, Color.Gold), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "CSO Group";

                range = ws.Cells.CreateRange("AQ3", "AQ3");
                range.ApplyStyle(HeaderStyle(wb, Color.Blue), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Level Of Estimate";

                range = ws.Cells.CreateRange("AR2", "AY2");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "Duration";

                range = ws.Cells.CreateRange("AR3", "AR3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(150,54,52)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Total OP14 Duration";

                range = ws.Cells.CreateRange("AS3", "AS3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(218, 150, 148)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Trouble Free";

                range = ws.Cells.CreateRange("AT3", "AT3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(218, 150, 148)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Trouble";

                range = ws.Cells.CreateRange("AU3", "AU3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(218, 150, 148)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Contingency";

                range = ws.Cells.CreateRange("AV3", "AV3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(230, 184, 183)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "TQ Duration";

                range = ws.Cells.CreateRange("AW3", "AW3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(230, 184, 183)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "BIC Duration";

                range = ws.Cells.CreateRange("AX3", "AX3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(242, 220, 219)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "LTA2 Duration";

                range = ws.Cells.CreateRange("AY3", "AY3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(242, 220, 219)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "New (Since LTA2)";

                range = ws.Cells.CreateRange("AZ2", "AZ2");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Value = "Burn Rate";

                range = ws.Cells.CreateRange("AZ3", "AZ3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(226, 107, 10)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Burn Rate";

                range = ws.Cells.CreateRange("BA2", "BI2");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "Cost";

                range = ws.Cells.CreateRange("BA3", "BA3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(79, 98, 40)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Total OP14 Cost";

                range = ws.Cells.CreateRange("BB3", "BB3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(79, 98, 40)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Total Cost SS";

                range = ws.Cells.CreateRange("BC3", "BC3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(118, 147, 60)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Trouble Free";

                range = ws.Cells.CreateRange("BD3", "BD3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(118, 147, 60)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Trouble";

                range = ws.Cells.CreateRange("BE3", "BE3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(118, 147, 60)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Contingency";

                range = ws.Cells.CreateRange("BF3", "BF3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(196, 215, 155)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Escalation & Inflation";

                range = ws.Cells.CreateRange("BG3", "BG3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(216, 228, 188)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "CSO";

                range = ws.Cells.CreateRange("BH3", "BH3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(235, 241, 222)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Total Cost (Incl Portfolio Risk)";

                range = ws.Cells.CreateRange("BI3", "BI3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(235, 241, 222)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "LTA2 Cost";

                range = ws.Cells.CreateRange("BJ3", "BJ3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(235, 241, 222)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "TQ Cost";


                range = ws.Cells.CreateRange("BK3", "BK3");
                range.ApplyStyle(HeaderStyle(wb, Color.FromArgb(235, 241, 222)), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "BIC Cost";

                range = ws.Cells.CreateRange("BL2", "BM2");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "Long Leads";

                range = ws.Cells.CreateRange("BL3", "BL3");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "LL Month";

                range = ws.Cells.CreateRange("BM3", "BM3");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "LL Amount";

                range = ws.Cells.CreateRange("BN3", "BN3");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Total Cost " + DateTime.Now.Year.ToString();

                range = ws.Cells.CreateRange("BO3", "BO3");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Total Cost (SS) " + DateTime.Now.Year.ToString();

                range = ws.Cells.CreateRange("BP3", "BP3");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "LE Duration";

                range = ws.Cells.CreateRange("BQ3", "BQ3");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Total Cost LE";

                range = ws.Cells.CreateRange("BR3", "BR3");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "LE Cost " + DateTime.Now.Year.ToString() + " (SS)";

                range = ws.Cells.CreateRange("BS3", "BS3");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "LE Cost " + DateTime.Now.Year.ToString();

                range = ws.Cells.CreateRange("BT3", "BT3");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Total Cost " + (DateTime.Now.Year + 3).ToString();

                range = ws.Cells.CreateRange("BU3", "BU3");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Total Cost " + (DateTime.Now.Year + 4).ToString();

                range = ws.Cells.CreateRange("BV3", "BV3");
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.Small);
                range.Value = "Total Cost " + (DateTime.Now.Year + 5).ToString();

                range = ws.Cells.CreateRange("BW3", "BW3");
                range.ApplyStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                range.ColumnWidth = SetColumnWidth(ColumnWidth.XSmall);
                range.Value = string.Empty;

                int year = (DateTime.Now.Year - 1);
                int colCount = ws.Cells.CreateRange("A1", "BW1").ColumnCount;
                Cell cell;
                for (int i = 0; i < 10; i++)
                {
                    cell = ws.Cells[0, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue("Unescalated Cost");

                    cell = ws.Cells[1, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue((year + i).ToString());

                    cell = ws.Cells[2, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue("Unescalated Cost " + (year + i).ToString());

                    colCount++;
                }

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                cell.PutValue("Check");

                colCount++;

                for (int i = 0; i < 10; i++)
                {
                    for (int j = 1; j <= 12; j++)
                    {
                        cell = ws.Cells[0, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue("Unescalated Cost");

                        cell = ws.Cells[1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue((year + i).ToString());

                        cell = ws.Cells[2, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue(new DateTime(year + i,j,1).ToString("MMM"));

                        colCount++;
                    }
                }

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                cell.PutValue("Cost (Escalated & Inflated)");

                colCount++;

                for (int i = 0; i < 10; i++)
                {
                    cell = ws.Cells[0, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue("Escalated Cost");

                    cell = ws.Cells[1, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue((year + i).ToString());

                    cell = ws.Cells[2, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue("Escalated Cost " + (year + i).ToString());

                    colCount++;
                }

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                cell.PutValue("Check");

                colCount++;

                for (int i = 0; i < 10; i++)
                {
                    for (int j = 1; j <= 12; j++)
                    {
                        cell = ws.Cells[0, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue("Escalated Cost");

                        cell = ws.Cells[1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue((year + i).ToString());

                        cell = ws.Cells[2, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue(new DateTime(year + i, j, 1).ToString("MMM"));

                        colCount++;
                    }
                }

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                cell.PutValue(string.Empty);

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                cell.PutValue("Total Cost (With Esc, Infl, & CSO)");

                colCount++;

                for (int i = 0; i < 10; i++)
                {
                    cell = ws.Cells[0, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue("Total Cost");

                    cell = ws.Cells[1, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue((year + i).ToString());

                    cell = ws.Cells[2, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue("Total Cost " + (year + i).ToString());

                    colCount++;
                }

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                cell.PutValue("Check");

                colCount++;

                for (int i = 0; i < 10; i++)
                {
                    for (int j = 1; j <= 12; j++)
                    {
                        cell = ws.Cells[0, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue("Total Cost");

                        cell = ws.Cells[1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue((year + i).ToString());

                        cell = ws.Cells[2, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue(new DateTime(year + i, j, 1).ToString("MMM"));

                        colCount++;
                    }
                }

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                cell.PutValue("Check");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                cell.PutValue("Check");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                cell.PutValue(string.Empty);

                colCount++;

                range = ws.Cells.CreateRange(1, colCount, 1, 6);
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "Top Quartile Measures";

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("TQ Duration");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("BIC Duration");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("TQ Cost");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("BIC Cost");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("TQ/Target Per PIP?");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("BIC Per PIP?");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                cell.PutValue(string.Empty);

                colCount++;

                range = ws.Cells.CreateRange(0, colCount, 1, 13);
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "AFE";

                range = ws.Cells.CreateRange(1, colCount, 1, 4);
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "AFE - Duration";

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(150, 54, 52)), FlagStyle());
                cell.PutValue("Total Duration");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(218, 150, 148)), FlagStyle());
                cell.PutValue("Trouble Free");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("Trouble");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("Contingency");

                colCount++;

                range = ws.Cells.CreateRange(1, colCount, 1, 1);
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "Burn Rate";

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(226, 107, 10)), FlagStyle());
                cell.PutValue("Burn Rate");

                colCount++;

                range = ws.Cells.CreateRange(1, colCount, 1, 6);
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "AFE - Cost";

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(79, 98, 40)), FlagStyle());
                cell.PutValue("Total Cost");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(118, 147, 60)), FlagStyle());
                cell.PutValue("Trouble Free");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("Trouble");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("Contingency");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(196, 215, 155)), FlagStyle());
                cell.PutValue("Escalation / Inflation");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(216, 228, 188)), FlagStyle());
                cell.PutValue("CSO");

                colCount++;

                range = ws.Cells.CreateRange(1, colCount, 1, 2);
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "AFE - Schedule";

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(141, 180, 226)), FlagStyle());
                cell.PutValue("Start");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(141, 180, 226)), FlagStyle());
                cell.PutValue("Finish");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                cell.PutValue(string.Empty);


                colCount++;

                range = ws.Cells.CreateRange(0, colCount, 1, 13);
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "Latest Estimate";

                range = ws.Cells.CreateRange(1, colCount, 1, 4);
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "LE - Duration";

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(150, 54, 52)), FlagStyle());
                cell.PutValue("Total Duration");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(218, 150, 148)), FlagStyle());
                cell.PutValue("Trouble Free");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("Trouble");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("Contingency");

                colCount++;

                range = ws.Cells.CreateRange(1, colCount, 1, 1);
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "Burn Rate";

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(226, 107, 10)), FlagStyle());
                cell.PutValue("Burn Rate");

                colCount++;

                range = ws.Cells.CreateRange(1, colCount, 1, 6);
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "LE - Cost";

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(79, 98, 40)), FlagStyle());
                cell.PutValue("Total Cost");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(118, 147, 60)), FlagStyle());
                cell.PutValue("Trouble Free");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("Trouble");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(177, 160, 199)), FlagStyle());
                cell.PutValue("Contingency");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(196, 215, 155)), FlagStyle());
                cell.PutValue("Escalation / Inflation");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(216, 228, 188)), FlagStyle());
                cell.PutValue("CSO");

                colCount++;

                range = ws.Cells.CreateRange(1, colCount, 1, 2);
                range.ApplyStyle(HeaderStyle(wb, Color.White), FlagStyle());
                range.Merge();
                range.Value = "LE - Schedule";

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(141, 180, 226)), FlagStyle());
                cell.PutValue("Start");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(141, 180, 226)), FlagStyle());
                cell.PutValue("Finish");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                cell.PutValue(string.Empty);

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.FromArgb(191, 191, 191)), FlagStyle());
                cell.PutValue("In OP14");

                colCount++;

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                cell.PutValue(string.Empty);

                colCount++;

                for (int i = 0; i < 10; i++)
                {
                    cell = ws.Cells[0, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue("LE Cost");

                    cell = ws.Cells[1, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue((year + i).ToString());

                    cell = ws.Cells[2, colCount];
                    cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                    cell.PutValue("LE Cost " + (year + i).ToString());

                    colCount++;
                }

                cell = ws.Cells[2, colCount];
                cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                cell.PutValue("Check");

                colCount++;

                for (int i = 0; i < 10; i++)
                {
                    for (int j = 1; j <= 12; j++)
                    {
                        cell = ws.Cells[0, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue("LE Cost Per Month");

                        cell = ws.Cells[1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue((year + i).ToString());

                        cell = ws.Cells[2, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.White), FlagStyle());
                        cell.PutValue(new DateTime(year + i, j, 1).ToString("MMM"));

                        colCount++;
                    }
                }

                #endregion

                #region content

                int startRow = 4;

                foreach (BizPlanActivity plan in Activities)
                {
                    foreach (BizPlanActivityPhase phase in plan.Phases)
                    {
                        range = ws.Cells.CreateRange("A" + startRow.ToString(), "A" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.Region;

                        range = ws.Cells.CreateRange("B" + startRow.ToString(), "B" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.RigType;

                        range = ws.Cells.CreateRange("C" + startRow.ToString(), "C" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.RigName;

                        range = ws.Cells.CreateRange("D" + startRow.ToString(), "D" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.OperatingUnit;

                        range = ws.Cells.CreateRange("E" + startRow.ToString(), "E" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.PerformanceUnit;

                        range = ws.Cells.CreateRange("F" + startRow.ToString(), "F" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.WellEngineer;

                        range = ws.Cells.CreateRange("G" + startRow.ToString(), "G" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.CWIEngineer;

                        range = ws.Cells.CreateRange("H" + startRow.ToString(), "H" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = "N/A";

                        range = ws.Cells.CreateRange("I" + startRow.ToString(), "I" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.AssetName;

                        range = ws.Cells.CreateRange("J" + startRow.ToString(), "J" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.ProjectName;

                        range = ws.Cells.CreateRange("K" + startRow.ToString(), "K" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.WellName;

                        range = ws.Cells.CreateRange("L" + startRow.ToString(), "L" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(String.Format("{0:P2}", Tools.Div(plan.WorkingInterest, 100)), true, true);

                        range = ws.Cells.CreateRange("M" + startRow.ToString(), "M" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.FirmOrOption;

                        range = ws.Cells.CreateRange("N" + startRow.ToString(), "N" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = phase.ActivityType;

                        range = ws.Cells.CreateRange("O" + startRow.ToString(), "O" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = phase.ActivityDesc;

                        range = ws.Cells.CreateRange("P" + startRow.ToString(), "P" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = phase.RiskFlag;

                        range = ws.Cells.CreateRange("Q" + startRow.ToString(), "Q" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = "N/A";

                        range = ws.Cells.CreateRange("R" + startRow.ToString(), "R" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.UARigSequenceId;

                        range = ws.Cells.CreateRange("S" + startRow.ToString(), "S" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = plan.UARigDescription;

                        //var OPSDur = plan.OpsSchedule.Finish.Subtract(plan.OpsSchedule.Start).TotalDays;
                        double? OPSDur = phase.Estimate == null ? null : 
                            (phase.Estimate.EstimatePeriod.Start.Year == 1900 ? (double?)null : 
                                phase.Estimate.EstimatePeriod.Finish.Subtract(phase.Estimate.EstimatePeriod.Start).TotalDays);
                        range = ws.Cells.CreateRange("T" + startRow.ToString(), "T" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(OPSDur, false), true, true);

                        range = ws.Cells.CreateRange("U" + startRow.ToString(), "U" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(plan.OpsSchedule.Start.ToString("MM/dd/yyyy"), true, true);
                        range.PutValue(phase.Estimate == null ? "-" : (phase.Estimate.EstimatePeriod.Start.Year == 1900 ? "-" : phase.Estimate.EstimatePeriod.Start.ToString("MM/dd/yyyy")), true, true);

                        range = ws.Cells.CreateRange("V" + startRow.ToString(), "V" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(plan.OpsSchedule.Finish.ToString("MM/dd/yyyy"), true, true);
                        range.PutValue(phase.Estimate == null ? "-" : (phase.Estimate.EstimatePeriod.Finish.Year == 1900 ? "-" : phase.Estimate.EstimatePeriod.Finish.ToString("MM/dd/yyyy")), true, true);

                        range = ws.Cells.CreateRange("W" + startRow.ToString(), "W" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(phase.PlanSchedule.Start.ToString("MM/dd/yyyy"), true, true);
                        range.PutValue(phase.PlanSchedule == null ? "-" : (phase.PlanSchedule.Start.Year == 1900 ? "-" : phase.PlanSchedule.Start.ToString("MM/dd/yyyy")), true, true);

                        range = ws.Cells.CreateRange("X" + startRow.ToString(), "X" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(phase.PlanSchedule.Finish.ToString("MM/dd/yyyy"), true, true);                        
                        range.PutValue(phase.PlanSchedule == null ? "-" : (phase.PlanSchedule.Finish.Year == 1900 ? "-" : phase.PlanSchedule.Finish.ToString("MM/dd/yyyy")), true, true);

                        //var PSDur = phase.PlanSchedule.Finish.Subtract(phase.PlanSchedule.Start).TotalDays;
                        double? PSDur = phase.PlanSchedule == null ? (double?)null : (phase.PlanSchedule.Start.Year == 1900 ? (double?)null : phase.PlanSchedule.Finish.Subtract(phase.PlanSchedule.Start).TotalDays);
                        range = ws.Cells.CreateRange("Y" + startRow.ToString(), "Y" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(PSDur, false), true, true);

                        range = ws.Cells.CreateRange("Z" + startRow.ToString(), "Z" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(OPSDur - PSDur, false), true, true);

                        range = ws.Cells.CreateRange("AA" + startRow.ToString(), "AA" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(phase.PhSchedule.Start.ToString("MM/dd/yyyy"), true, true);
                        range.PutValue(phase.PhSchedule == null ? "-" : (phase.PhSchedule.Start.Year == 1900 ? "-" : phase.PhSchedule.Start.ToString("MM/dd/yyyy")), true, true);

                        range = ws.Cells.CreateRange("AB" + startRow.ToString(), "AB" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(phase.PhSchedule.Finish.ToString("MM/dd/yyyy"), true, true);
                        range.PutValue(phase.PhSchedule == null ? "-" : (phase.PhSchedule.Finish.Year == 1900 ? "-" : phase.PhSchedule.Finish.ToString("MM/dd/yyyy")), true, true);

                        var PHDur = phase.PhSchedule.Finish.Subtract(phase.PhSchedule.Start).TotalDays;
                        range = ws.Cells.CreateRange("AC" + startRow.ToString(), "AC" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(PHDur, false), true, true);

                        range = ws.Cells.CreateRange("AD" + startRow.ToString(), "AD" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(phase.Estimate.NewTroubleFree.Days + phase.Estimate.CurrentTroubleFree.Days + phase.PhaseInfo.Contigency.Days - PHDur, false), true, true); 

                        range = ws.Cells.CreateRange("AE" + startRow.ToString(), "AE" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(phase.PhSchedule.Start.Year.ToString(), true, true);
                        range.PutValue(phase.PhSchedule == null ? "-" : (phase.PhSchedule.Start.Year == 1900 ? "-" : phase.PhSchedule.Start.Year.ToString()), true, true);

                        range = ws.Cells.CreateRange("AF" + startRow.ToString(), "AF" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(phase.PhSchedule.Start.ToString("yyyy-MM"), true, true);
                        range.PutValue(phase.PhSchedule == null ? "-" : (phase.PhSchedule.Start.Year == 1900 ? "-" : phase.PhSchedule.Start.ToString("yyyy-MM")), true, true);

                        range = ws.Cells.CreateRange("AG" + startRow.ToString(), "AG" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(phase.PhSchedule.Finish.Year.ToString(), true, true);
                        range.PutValue(phase.PhSchedule == null ? "-" : (phase.PhSchedule.Start.Year == 1900 ? "-" : phase.PhSchedule.Start.Year.ToString()), true, true);

                        range = ws.Cells.CreateRange("AH" + startRow.ToString(), "AH" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(phase.PhSchedule.Finish.ToString("yyyy-MM"), true, true);
                        range.PutValue(phase.PhSchedule == null ? "-" : (phase.PhSchedule.Finish.Year == 1900 ? "-" : phase.PhSchedule.Finish.ToString("yyyy-MM")), true, true);

                        range = ws.Cells.CreateRange("AI" + startRow.ToString(), "AI" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(plan.OpsSchedule.Start.Year.ToString(), true, true);
                        range.PutValue(phase.Estimate == null ? "-" : (phase.Estimate.EstimatePeriod.Start.Year == 1900 ? "-" : phase.Estimate.EstimatePeriod.Start.Year.ToString()), true, true);

                        range = ws.Cells.CreateRange("AJ" + startRow.ToString(), "AJ" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(plan.OpsSchedule.Start.ToString("yyyy-MM"), true, true);
                        range.PutValue(phase.Estimate == null ? "-" : (phase.Estimate.EstimatePeriod.Start.Year == 1900 ? "-" : phase.Estimate.EstimatePeriod.Start.ToString("yyyy-MM")), true, true);

                        range = ws.Cells.CreateRange("AK" + startRow.ToString(), "AK" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(plan.OpsSchedule.Finish.Year.ToString(), true, true);
                        range.PutValue(phase.Estimate == null ? "-" : (phase.Estimate.EstimatePeriod.Start.Year == 1900 ? "-" : phase.Estimate.EstimatePeriod.Finish.Year.ToString()), true, true);

                        range = ws.Cells.CreateRange("AL" + startRow.ToString(), "AL" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.PutValue(plan.OpsSchedule.Finish.ToString("yyyy-MM"), true, true);
                        range.PutValue(phase.Estimate == null ? "-" : (phase.Estimate.EstimatePeriod.Start.Year == 1900 ? "-" : phase.Estimate.EstimatePeriod.Finish.ToString("yyyy-MM")), true, true);

                        range = ws.Cells.CreateRange("AM" + startRow.ToString(), "AM" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = phase.FundingType;

                        range = ws.Cells.CreateRange("AN" + startRow.ToString(), "AN" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = phase.Grouping;

                        range = ws.Cells.CreateRange("AO" + startRow.ToString(), "AO" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = phase.EscalationGroup;

                        range = ws.Cells.CreateRange("AP" + startRow.ToString(), "AP" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = phase.CSOGroup;

                        range = ws.Cells.CreateRange("AQ" + startRow.ToString(), "AQ" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = phase.LevelOfEstimate;

                        range = ws.Cells.CreateRange("AR" + startRow.ToString(), "AR" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(phase.Estimate.NewTroubleFree.Days + phase.Estimate.CurrentTroubleFree.Days + phase.PhaseInfo.Contigency.Days, false), true, true);

                        range = ws.Cells.CreateRange("AS" + startRow.ToString(), "AS" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(phase.Estimate.NewTroubleFree.Days, false), true, true);

                        range = ws.Cells.CreateRange("AT" + startRow.ToString(), "AT" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.Value = phase.Estimate.CurrentTroubleFree.Days;
                        range.PutValue(NumericFormatting(phase.Estimate.CurrentTroubleFree.Days, false), true, true);

                        range = ws.Cells.CreateRange("AU" + startRow.ToString(), "AU" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(phase.PhaseInfo.Contigency.Days, false), true, true);

                        range = ws.Cells.CreateRange("AV" + startRow.ToString(), "AV" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(phase.TQ.Days, false), true, true);

                        range = ws.Cells.CreateRange("AW" + startRow.ToString(), "AW" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(phase.PhaseInfo.BIC.Days, false), true, true);

                        range = ws.Cells.CreateRange("AX" + startRow.ToString(), "AX" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(phase.PhaseInfo.LTA2.Days, false), true, true);

                        range = ws.Cells.CreateRange("AY" + startRow.ToString(), "AY" + startRow.ToString());
                        range.ApplyStyle(CharStyle(wb), FlagStyle());
                        range.Value = phase.PhaseInfo.SinceLTA2;

                        range = ws.Cells.CreateRange("AZ" + startRow.ToString(), "AZ" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(phase.Estimate.BurnRate, true), true, true);

                        range = ws.Cells.CreateRange("BA" + startRow.ToString(), "BA" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Total OP14 Cost";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BB" + startRow.ToString(), "BB" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Total Cost SS";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BC" + startRow.ToString(), "BC" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Trouble Free";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BD" + startRow.ToString(), "BD" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Trouble";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BE" + startRow.ToString(), "BE" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Contingency";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BF" + startRow.ToString(), "BF" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Escalation & Inflation";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BG" + startRow.ToString(), "BG" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "CSO";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BH" + startRow.ToString(), "BH" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Total Cost (Incl Portfolio Risk)";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BI" + startRow.ToString(), "BI" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "LTA2 Cost";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BJ" + startRow.ToString(), "BJ" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        range.PutValue(NumericFormatting(phase.TQ.Cost, true), true, true);

                        range = ws.Cells.CreateRange("BK" + startRow.ToString(), "BK" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = phase.;
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BL" + startRow.ToString(), "BL" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "LL Month";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BM" + startRow.ToString(), "BM" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "LL Amount";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BN" + startRow.ToString(), "BN" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Total Cost " + DateTime.Now.Year.ToString();
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BO" + startRow.ToString(), "BO" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Total Cost (SS) " + DateTime.Now.Year.ToString();
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BP" + startRow.ToString(), "BP" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "LE Duration";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BQ" + startRow.ToString(), "BQ" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Total Cost LE";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BR" + startRow.ToString(), "BR" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "LE Cost " + DateTime.Now.Year.ToString() + " (SS)";
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);


                        range = ws.Cells.CreateRange("BS" + startRow.ToString(), "BS" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "LE Cost " + DateTime.Now.Year.ToString();
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BT" + startRow.ToString(), "BT" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Total Cost " + (DateTime.Now.Year + 3).ToString();
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BU" + startRow.ToString(), "BU" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Total Cost " + (DateTime.Now.Year + 4).ToString();
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BV" + startRow.ToString(), "BV" + startRow.ToString());
                        range.ApplyStyle(NumericStyle(wb), FlagStyle());
                        //range.Value = "Total Cost " + (DateTime.Now.Year + 5).ToString();
                        range.PutValue(NumericFormatting(string.Empty, true), true, true);

                        range = ws.Cells.CreateRange("BW" + startRow.ToString(), "BW" + startRow.ToString());
                        range.ApplyStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                        range.Value = string.Empty;

                        year = (DateTime.Now.Year - 1);
                        colCount = ws.Cells.CreateRange("A1", "BW1").ColumnCount;

                        for (int i = 0; i < 10; i++)
                        {
                            cell = ws.Cells[startRow-1, colCount];
                            cell.SetStyle(NumericStyle(wb), FlagStyle());
                            //cell.PutValue("Unescalated Cost " + (year + i).ToString());
                            cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                            colCount++;
                        }

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                        //cell.PutValue("Check");
                        cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                        colCount++;

                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 1; j <= 12; j++)
                            {
                                cell = ws.Cells[startRow - 1, colCount];
                                cell.SetStyle(NumericStyle(wb), FlagStyle());
                                //cell.PutValue(new DateTime(year + i, j, 1).ToString("MMM"));
                                cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                                colCount++;
                            }
                        }

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Cost (Escalated & Inflated)");
                        cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                        colCount++;

                        for (int i = 0; i < 10; i++)
                        {
                            cell = ws.Cells[startRow - 1, colCount];
                            cell.SetStyle(NumericStyle(wb), FlagStyle());
                            //cell.PutValue("Escalated Cost " + (year + i).ToString());
                            cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                            colCount++;
                        }

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                        //cell.PutValue("Check");
                        cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                        colCount++;

                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 1; j <= 12; j++)
                            {
                                cell = ws.Cells[startRow - 1, colCount];
                                cell.SetStyle(NumericStyle(wb), FlagStyle());
                                //cell.PutValue(new DateTime(year + i, j, 1).ToString("MMM"));
                                cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                                colCount++;
                            }
                        }

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                        cell.PutValue(string.Empty);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Total Cost (With Esc, Infl, & CSO)");
                        cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                        colCount++;

                        for (int i = 0; i < 10; i++)
                        {
                            cell = ws.Cells[startRow - 1, colCount];
                            cell.SetStyle(NumericStyle(wb), FlagStyle());
                            //cell.PutValue("Total Cost " + (year + i).ToString());
                            cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                            colCount++;
                        }

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                        //cell.PutValue("Check");
                        cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                        colCount++;

                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 1; j <= 12; j++)
                            {
                                cell = ws.Cells[startRow - 1, colCount];
                                cell.SetStyle(NumericStyle(wb), FlagStyle());
                                //cell.PutValue(new DateTime(year + i, j, 1).ToString("MMM"));
                                cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                                colCount++;
                            }
                        }

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                        //cell.PutValue("Check");
                        cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                        //cell.PutValue("Check");
                        cell.PutValue(NumericFormatting(string.Empty, true), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                        cell.PutValue(string.Empty);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        cell.PutValue(NumericFormatting(phase.PhaseInfo.TQ.Days, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        cell.PutValue(NumericFormatting(phase.PhaseInfo.BIC.Days, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        cell.PutValue(NumericFormatting(phase.PhaseInfo.TQ.Cost, true), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        cell.PutValue(NumericFormatting(phase.PhaseInfo.BIC.Cost, true), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        cell.PutValue(phase.PhaseInfo.TQMeasures.TQTargetperPIP);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        cell.PutValue(phase.PhaseInfo.TQMeasures.BICperPIP);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                        cell.PutValue(string.Empty);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Total Duration");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Trouble Free");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Trouble");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Contingency");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Burn Rate");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Total Cost");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Trouble Free");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Trouble");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Contingency");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Escalation / Inflation");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("CSO");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Start");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Finish");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                        cell.PutValue(string.Empty);


                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Total Duration");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Trouble Free");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Trouble");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Contingency");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Burn Rate");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Total Cost");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Trouble Free");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Trouble");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Contingency");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Escalation / Inflation");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("CSO");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Start");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("Finish");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                        cell.PutValue(string.Empty);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(NumericStyle(wb), FlagStyle());
                        //cell.PutValue("In OP14");
                        cell.PutValue(string.Empty);

                        colCount++;

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.Black), FlagStyle());
                        cell.PutValue(string.Empty);

                        colCount++;

                        for (int i = 0; i < 10; i++)
                        {
                            cell = ws.Cells[startRow - 1, colCount];
                            cell.SetStyle(NumericStyle(wb), FlagStyle());
                            //cell.PutValue("LE Cost " + (year + i).ToString());
                            cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                            colCount++;
                        }

                        cell = ws.Cells[startRow - 1, colCount];
                        cell.SetStyle(HeaderStyle(wb, Color.LightGray), FlagStyle());
                        //cell.PutValue("Check");
                        cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                        colCount++;

                        for (int i = 0; i < 10; i++)
                        {
                            for (int j = 1; j <= 12; j++)
                            {
                                cell = ws.Cells[startRow - 1, colCount];
                                cell.SetStyle(NumericStyle(wb), FlagStyle());
                                //cell.PutValue(new DateTime(year + i, j, 1).ToString("MMM"));
                                cell.PutValue(NumericFormatting(string.Empty, false), true, true);

                                colCount++;
                            }
                        }
                        startRow++;
                    }
                }
                #endregion

                wb.Save(AbsolutePath, SaveFormat.Xlsx);

                return true;
            }
            catch(Exception ex)
            {
                Core.LogWriter log= new Core.LogWriter();
                log.Write(ex.Message, ex.StackTrace);
                return false;
            }
        }
    }
}