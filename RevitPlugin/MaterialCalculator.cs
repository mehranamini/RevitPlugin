using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace RevitPlugin
{
    public class MaterialCalculator : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            application.CreateRibbonTab("RevitPlugin");

            string path = Assembly.GetExecutingAssembly().Location;
            PushButtonData button = new PushButtonData("btnCalcMaterial", "My Tools", path, "RevitPlugin.Commands.Command");

            RibbonPanel panel = application.CreateRibbonPanel("RevitPlugin", "Commands");

            PushButton pushButton = panel.AddItem(button) as PushButton;
            pushButton.LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitPlugin;component/Images/floor.png", 
                UriKind.RelativeOrAbsolute));

            return Result.Succeeded;
        }
    }
}
