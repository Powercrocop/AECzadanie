#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace AECzadanie
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        private UIApplication _uiapp;
        private UIDocument _uidoc;
        Autodesk.Revit.ApplicationServices.Application _app;
        Document _doc;
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            _uiapp = commandData.Application;
            _uidoc = _uiapp.ActiveUIDocument;
            _app = _uiapp.Application;
            _doc = _uidoc.Document;

            MainFunction();

            return Result.Succeeded;
        }

        private void MainFunction()
        {
            double startPointX = 0;
            double startPointY = 0;
            double startPointZ = 0;

            List<Line> coveredLines = new List<Line>();
            List<Line> sortedLines = new List<Line>();
            List<Line> sortedLinesToNumeration = new List<Line>();

            foreach (Line line in GetSortedLinesByConnection())
            {
                if (sortedLines.Count == 0)
                {
                    startPointX = Math.Round(line.GetEndPoint(0).X, 10);
                    startPointY = Math.Round(line.GetEndPoint(0).Y, 10);
                    startPointZ = Math.Round(line.GetEndPoint(0).Z, 10);

                    coveredLines = GetCoveredLines(line, GetAllLinesFromModelLines());
                    sortedLines = coveredLines.OrderBy(l => l.Distance(new XYZ(startPointX, startPointY, startPointZ))).ToList();
                    sortedLinesToNumeration.AddRange(sortedLines);

                    if (Math.Round(line.GetEndPoint(0).X, 10) == startPointX &&
                        Math.Round(line.GetEndPoint(0).Y, 10) == startPointY &&
                        Math.Round(line.GetEndPoint(0).Z, 10) == startPointZ)
                    {
                        startPointX = Math.Round(line.GetEndPoint(1).X, 10);
                        startPointY = Math.Round(line.GetEndPoint(1).Y, 10);
                        startPointZ = Math.Round(line.GetEndPoint(1).Z, 10);
                    }
                    else
                    {
                        startPointX = Math.Round(line.GetEndPoint(0).X, 10);
                        startPointY = Math.Round(line.GetEndPoint(0).Y, 10);
                        startPointZ = Math.Round(line.GetEndPoint(0).Z, 10);
                    }
                }
                else
                {
                    coveredLines = GetCoveredLines(line, GetAllLinesFromModelLines());
                    sortedLines = coveredLines.OrderBy(l => l.Distance(new XYZ(startPointX, startPointY, startPointZ))).ToList();
                    sortedLinesToNumeration.AddRange(sortedLines);

                    if (Math.Round(line.GetEndPoint(0).X, 10) == startPointX &&
                        Math.Round(line.GetEndPoint(0).Y, 10) == startPointY &&
                        Math.Round(line.GetEndPoint(0).Z, 10) == startPointZ)
                    {
                        startPointX = Math.Round(line.GetEndPoint(1).X, 10);
                        startPointY = Math.Round(line.GetEndPoint(1).Y, 10);
                        startPointZ = Math.Round(line.GetEndPoint(1).Z, 10);
                    }
                    else
                    {
                        startPointX = Math.Round(line.GetEndPoint(0).X, 10);
                        startPointY = Math.Round(line.GetEndPoint(0).Y, 10);
                        startPointZ = Math.Round(line.GetEndPoint(0).Z, 10);
                    }
                }
            }

            CreateTextNotes(sortedLinesToNumeration);
        }
        private List<Line> GetSortedLinesByConnection()
        {
            double startPointX = 0;
            double startPointY = 0;
            double startPointZ = 0;

            List<Line> filledRegionLines = GetAllLinesFromFilledRegion();
            List<Line> sortedLines = new List<Line>();
            List<int> sortedLinesId = new List<int>();

            foreach (Line line1 in filledRegionLines)
            {
                if (sortedLines.Count == 0)
                {
                    sortedLines.Add(filledRegionLines[0]);
                    sortedLinesId.Add(filledRegionLines[0].Id);

                    startPointX = Math.Round(line1.GetEndPoint(1).X, 10);
                    startPointY = Math.Round(line1.GetEndPoint(1).Y, 10);
                    startPointZ = Math.Round(line1.GetEndPoint(1).Z, 10);
                }
                if (sortedLines.Count > 0)
                {
                    foreach (Line line2 in filledRegionLines)
                    {
                        if ((Math.Round(line2.GetEndPoint(0).X, 10) == Math.Round(startPointX, 10) &&
                            (Math.Round(line2.GetEndPoint(0).Y, 10) == Math.Round(startPointY, 10) &&
                            (Math.Round(line2.GetEndPoint(0).Z, 10) == Math.Round(startPointZ, 10) &&
                            !sortedLinesId.Contains(line2.Id)) ||
                            ((Math.Round(line2.GetEndPoint(1).X, 10) == Math.Round(startPointX, 10) &&
                           (Math.Round(line2.GetEndPoint(1).Y, 10) == Math.Round(startPointY, 10) &&
                            (Math.Round(line2.GetEndPoint(1).Z, 10) == Math.Round(startPointZ, 10) &&
                             !sortedLinesId.Contains(line2.Id))))))))
                        {
                            sortedLines.Add(line2);
                            sortedLinesId.Add(line2.Id);

                            if (Math.Round(line2.GetEndPoint(0).X, 10) == startPointX &&
                                Math.Round(line2.GetEndPoint(0).Y, 10) == startPointY &&
                                Math.Round(line2.GetEndPoint(0).Z, 10) == startPointZ)
                            {
                                startPointX = Math.Round(line2.GetEndPoint(1).X, 10);
                                startPointY = Math.Round(line2.GetEndPoint(1).Y, 10);
                                startPointZ = Math.Round(line2.GetEndPoint(1).Z, 10);
                            }
                            else
                            {
                                startPointX = Math.Round(line2.GetEndPoint(0).X, 10);
                                startPointY = Math.Round(line2.GetEndPoint(0).Y, 10);
                                startPointZ = Math.Round(line2.GetEndPoint(0).Z, 10);
                            }
                        }
                    }
                }
            }
            return sortedLines;
        }
        private List<Line> GetAllLinesFromFilledRegion()
        {
            List<Line> listOfLines = new List<Line>();

            FilteredElementCollector filledRegionCollector = new FilteredElementCollector(
            _doc, _doc.ActiveView.Id).WherePasses(new ElementClassFilter(typeof(FilledRegion)));
            foreach (FilledRegion filledRegion in filledRegionCollector)
            {
                foreach (CurveLoop curveLoop in filledRegion.GetBoundaries())
                {
                    foreach (Line line in curveLoop)
                    {
                        listOfLines.Add(line);
                    }
                }
            }
            return listOfLines;
        }
        private List<Line> GetCoveredLines(Line coverLine, List<Line> lines)
        {
            List<Line> coveredLines = new List<Line>();

            foreach (Line line in lines)
            {
                XYZ coveredLineStartPoint = line.GetEndPoint(0);
                XYZ coveredLineEndPoint = line.GetEndPoint(1);
                if (coverLine.Distance(coveredLineStartPoint) < 0.001 &&
                coverLine.Distance(coveredLineEndPoint) < 0.001)
                {
                    coveredLines.Add(line);
                }
            }
            return coveredLines;
        }
        private List<Line> GetAllLinesFromModelLines()
        {
            List<Line> listOfLines = new List<Line>();

            FilteredElementCollector curveElementCollector = new FilteredElementCollector(
         _doc, _doc.ActiveView.Id).WherePasses(new ElementClassFilter(typeof(CurveElement)));
            foreach (CurveElement curveElement in curveElementCollector)
            {
                Line line = curveElement.GeometryCurve as Line;
                listOfLines.Add(line);
            }
            return listOfLines;
        }
        private void CreateTextNotes(List<Line> lines)
        {
            ElementId elementTypeId = new ElementId(361);
            Transaction trans = new Transaction(_doc, "Lines numerations");
            trans.Start();
            for (int i = 0; i < lines.Count; i++)
            {
                var midpoint = GetMidpointOfLine(lines[i]);
                TextNote.Create(_doc, _doc.ActiveView.Id, midpoint, i.ToString(), elementTypeId);
            }
            trans.Commit();
        }
        private XYZ GetMidpointOfLine(Line line)
        {
            XYZ startPoint = line.GetEndPoint(0);
            XYZ endPoint = line.GetEndPoint(1);
            XYZ midPointOfLine = new XYZ((startPoint.X + endPoint.X) / 2,
                                       (startPoint.Y + endPoint.Y) / 2,
                                       (startPoint.Z + endPoint.Z) / 2);
            return midPointOfLine;

        }

    }
}



