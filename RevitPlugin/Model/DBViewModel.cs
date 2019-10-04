using Autodesk.Revit.DB;
using System;

using RView = Autodesk.Revit.DB.View;

namespace RevitPlugin.Model
{
    public class DBViewModel
    {
        public DBViewModel(RView dbView, Document dbDoc)
        {
            ElementType viewType = dbDoc.GetElement(dbView.GetTypeId()) as ElementType;
            Name = viewType.Name + " " + dbView.Name;
            Id = dbView.Id;
            UniqueId = dbView.UniqueId;
        }

        public override String ToString()
        {
            return Name;
        }

        public String Name { get; set; }

        public ElementId Id { get; set; }

        public String UniqueId { get; set; }
    }
}
