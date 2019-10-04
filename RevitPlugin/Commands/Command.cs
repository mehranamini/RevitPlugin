using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using Autodesk.Revit.Attributes;
using RevitPlugin.Model;

namespace RevitPlugin.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Transaction tran = new Transaction(commandData.Application.ActiveUIDocument.Document, "Get rooms information");
            try
            {
                tran.Start();
                RoomsViewModel roomsData = new RoomsViewModel(commandData);

                ElementCalculator form = new ElementCalculator(commandData.Application.Application, roomsData);
                form.WindowState = System.Windows.WindowState.Maximized;
                form.Show();
                form.BringIntoView();

                tran.Commit();
            }
            catch (Exception ex)
            {
                tran.RollBack();
            }

            
            return Result.Succeeded;
        }
    }
}
