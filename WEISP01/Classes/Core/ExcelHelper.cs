using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MongoDB.Bson;
using System.Dynamic;
using Aspose.Cells;

namespace ECIS.Core
{
    public class ExcelHelper
    {
        public static Cell FindString(string value, Worksheet ws, int Column, int Row, string Direction, int LimitColumn=0, int LimitRow=0, string stopvalue=null)
        {
            Cell ret = null;
            bool find = true;
            bool notEnd = true;
            int iRow = Row;
            int iCol = Column;
            Cell lastCell = ws.Cells.LastCell;
            while (find && notEnd)
            {
                Cell cell = ws.Cells[iRow, iCol];
                string Evaluate = cell.StringValue;
                if (Evaluate.Equals(value))
                {
                    find = false;
                    ret = cell;
                }
                else if (stopvalue != null && Evaluate.Equals(stopvalue))
                {
                    notEnd = false;
                }
                else
                {
                    switch (Direction.ToLower())
                    {
                        case "row":
                            iRow++;
                            break;

                        case "column":
                            iCol++;
                            break;

                        default:
                            iCol++;
                            if (LimitColumn > 0 && (iCol - Column).CompareTo(LimitColumn) > 0)
                            {
                                iCol = Column;
                                iRow++;
                            }
                            if (LimitColumn == 0 && iCol == lastCell.Column){
                                iCol = Column;
                                iRow++;
                            }
                            break;
                    }

                    bool reachEndCol = (LimitColumn>0 && (iCol - Column).CompareTo(LimitColumn)>0) || iCol>lastCell.Column;
                    bool reachEndRow = (LimitRow > 0 && (iRow - Row).CompareTo(LimitRow) > 0) || iRow>lastCell.Row;
                    notEnd = !((Direction.Equals("") && reachEndRow) ||
                        (Direction.Equals("column") && reachEndCol) ||
                        (Direction.Equals("row") && reachEndRow));
                }
            }
            return ret;
        }
    }
}