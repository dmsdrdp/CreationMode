using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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

            CreateWalls(doc, level1, level2);               //Создание стен

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
        public void CreateWalls(Document doc, Level level1, Level level2) //Метод создания стен
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
        }
    }
}
