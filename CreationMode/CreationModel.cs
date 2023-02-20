using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationMode
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            List<Level> levels = GetLevels(doc);            //Фильтр по уровням
            Level level1 = GetLevel1(levels);               //Первый этаж
            Level level2 = GetLevel2(levels);               //Второй этаж

            List<Wall> walls = CreateWalls(doc, level1, level2);    //создание стен

            AddDoor(doc, level1, walls[0]);                         //вставка двери
            AddWindows(doc, level1, walls[1]);                      //вставка окон
            AddWindows(doc, level1, walls[2]);
            AddWindows(doc, level1, walls[3]);
            AddRoof(doc, level2);

            return Result.Succeeded;
        }

        public List<Level> GetLevels(Document doc)                     //Фильтр по уровням
        {

            List<Level> listLevel = new FilteredElementCollector(doc)
                           .OfClass(typeof(Level))
                           .OfType<Level>()
                           .ToList();
            return listLevel;
        }
        public Level GetLevel1(List<Level> levels)                     //Метод поиска первого этажа
        {

            Level level1 = levels
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();

            return level1;
        }
        public Level GetLevel2(List<Level> levels)                      //Метот поиска 2 этажа
        {
            Level leve2 = levels
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();
            return leve2;
        }


        public List<Wall> CreateWalls(Document doc, Level level1, Level level2) //Метод создания стен
        {
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();

            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();

            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }

            transaction.Commit();
            return walls;
        }

        private void AddDoor(Document doc, Level level1, Wall wall)             //вставка двери
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                 .OfClass(typeof(FamilySymbol))
                 .OfCategory(BuiltInCategory.OST_Doors)
                 .OfType<FamilySymbol>()
                 .Where(x => x.Name.Equals("0915 x 2134 мм"))
                 .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                 .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            Transaction transaction = new Transaction(doc, "Двери");
            transaction.Start();

            if (!doorType.IsActive)
                doorType.Activate();


            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
            transaction.Commit();

        }
        private void AddWindows(Document doc, Level level1, Wall wall)             //вставка окна
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                 .OfClass(typeof(FamilySymbol))
                 .OfCategory(BuiltInCategory.OST_Windows)
                 .OfType<FamilySymbol>()
                 .Where(x => x.Name.Equals("0915 x 1830 мм"))
                 .Where(x => x.FamilyName.Equals("Фиксированные"))
                 .FirstOrDefault();

            double height = UnitUtils.ConvertToInternalUnits(800, UnitTypeId.Millimeters); //высота нижнего бруса

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

          
            Transaction transaction = new Transaction(doc, "Окна");
            transaction.Start();
            if (!windowType.IsActive)
                windowType.Activate();
            FamilyInstance windows = doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);
            windows.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(height);

            transaction.Commit();

        }
        private void AddRoof(Document doc, Level level)                             //Крыша
        {

            ElementId id = doc.GetDefaultElementTypeId(ElementTypeGroup.RoofType);
            RoofType type = doc.GetElement(id) as RoofType;

            double length = UnitUtils.ConvertToInternalUnits(5500, UnitTypeId.Millimeters);  //длина
            double width = UnitUtils.ConvertToInternalUnits(5500, UnitTypeId.Millimeters);  //ширина
            double height = UnitUtils.ConvertToInternalUnits(4000, UnitTypeId.Millimeters); //высота

            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(-length, -width/ 2, height), new XYZ(0, 0, height * 2)));
            curveArray.Append(Line.CreateBound(new XYZ(0, 0, height * 2), new XYZ(0, width / 2, height)));


            using (Transaction tr = new Transaction(doc))
            {
                tr.Start("Create ExtrusionRoof");
                    ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, height), new XYZ(0, width, 0), doc.ActiveView);
                    doc.Create.NewExtrusionRoof(curveArray, plane, level, type, -length, width);
                tr.Commit();
            }

        }
    }
}
