using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using RApplication = Autodesk.Revit.ApplicationServices.Application;
using System.Collections.ObjectModel;
using Autodesk.Revit.DB.Architecture;
using RevitPlugin.Model;
using System.Text;
using Autodesk.Revit.UI.Selection;

namespace RevitPlugin.Commands
{

    public partial class ElementCalculator : Window, IExternalEventHandler
    {
        #region Variables

        
        int startElementPagingIndex { get; set; } = 0;
        PreviewControl revitPreview { get; set; } = null;

        public bool IsWallHide
        {
            get
            {
                return isWallHide;
            }
            set
            {
                isWallHide = value;
                btnHideWalls.Content = isWallHide ? "Show Walls" : "Hide Walls";
            }
        }

        ElementId currentViewMod = null;
        RoomsViewModel currentRoomData;
        Document dbDocument;
        UIApplication currentUIApplication;
        ExternalEvent exEvent;
        ExternalCommandType currentCommandType;
        bool isWallHide = false;
        ObservableCollection<RoomsDisplayViewModel> RoomInfoList = new ObservableCollection<RoomsDisplayViewModel>();
        BuiltInCategory[] enumKeys = null;
        
        #endregion

        public ElementCalculator(RApplication application, RoomsViewModel roomsData)
        {
            InitializeComponent();

            exEvent = ExternalEvent.Create(this);
            currentRoomData = roomsData;
            currentUIApplication = new UIApplication(application);
            dbDocument = currentUIApplication.ActiveUIDocument.Document;

            setViewOptions(null);
            loadFillPatern();
            loadItemsList();

            //Get all rooms information
            DisplayRooms(currentRoomData.RoomsWithoutTag, false);
            DisplayRooms(currentRoomData.RoomsWithTag, true);
        }

        #region Initialization

        /// <summary>
        /// Create a tree contains categories and element belong to those categories
        /// to show Revit items structure
        /// </summary>
        private void loadItemsList()
        {
            List<TreeViewModel> treeNodes = new List<TreeViewModel>();
            enumKeys = (BuiltInCategory[])Enum.GetValues(typeof(BuiltInCategory));
            
            decimal totalPage = Math.Round((decimal)enumKeys.Length / 50);
            decimal currentPage = Math.Round((decimal)startElementPagingIndex / 50);
            lblPageInfo.Content = string.Format("Page Size: 50, Page {0} from {1} ", currentPage, totalPage);

            // Use paging to increase loading speed 
            for (int i = startElementPagingIndex; i < enumKeys.Length && i < (startElementPagingIndex + 50); i++)
            {
                try
                {
                    BuiltInCategory categoryID = enumKeys[i];
                    string categoryName = categoryID.ToString();

                    // Search for any items exist in current category
                    FilteredElementCollector collector = new FilteredElementCollector(dbDocument);
                    ElementCategoryFilter filter = new ElementCategoryFilter(categoryID);
                    IList<Element> items = collector.WherePasses(filter)
                        .WhereElementIsNotElementType().ToElements();

                    TreeViewModel parentNode = new TreeViewModel(categoryName, new ElementId(categoryID));
                    foreach (Element child in items)
                    {
                        parentNode.Children.Add(new TreeViewModel(child.Name == "" ? "No Name" : child.Name, child));
                    }

                    treeNodes.Add(parentNode);
                    parentNode.Initialize();
                    parentNode.IsChecked = true;
                }
                catch (Exception ex)
                {
                    showMessage(ex.Message);
                }
            }

            trvItems.ItemsSource = treeNodes;
        }

        /// <summary>
        /// Set plot Rendering mod
        /// </summary>
        /// <param name="Id"> Target view mod identity </param>
        private void setViewOptions(ElementId viewId)
        {
            FilteredElementCollector collecotr = new FilteredElementCollector(dbDocument);
            collecotr.OfClass(typeof(View));
            IEnumerable<View> activeViews = from Element q in collecotr
                                            where (q as View).CanBePrinted == true
                                            select q as View;

            // Creat list of accessable view for current plot dynamically
            DBViewModel activeItem = null;
            foreach (View view in activeViews)
            {
                activeItem = new DBViewModel(view, dbDocument);
                RadioButton rbtn = new RadioButton();
                rbtn.Tag = activeItem;
                rbtn.Content = activeItem.Name;
                rbtn.Width = 150;
                rbtn.Checked += RbtnView_Checked;
                if (view.Id == viewId)
                    rbtn.IsChecked = true;

                pnlViewOptions.Children.Add(rbtn);
            }

            if ((viewId == null || viewId.IntegerValue == -1) && pnlViewOptions.Children.Count != 0)
                ((RadioButton)pnlViewOptions.Children[0]).IsChecked = true;
        }

        /// <summary>
        /// Load Revit fill paterns
        /// </summary>
        private void loadFillPatern()
        {
            List<FillPatternElement> lstFillPatterns = FilterElement<FillPatternElement>();
            lstPatern.ItemsSource = lstFillPatterns;
        }

        /// <summary>
        /// Load rooms information from model to data grid
        /// </summary>
        /// <param name="roomList"></param>
        /// <param name="isHaveTag"></param>
        private void DisplayRooms(ReadOnlyCollection<Room> roomList, bool isHaveTag)
        {
            RoomInfoList = new ObservableCollection<RoomsDisplayViewModel>();
            if (currentRoomData == null)
                return;

            foreach (Room revitRoomData in roomList)
            {
                // make sure the room has Level, that's it locates at level.
                if (revitRoomData.Document.GetElement(revitRoomData.LevelId) == null)
                {
                    continue;
                }

                // Set grid display model
                RoomsDisplayViewModel item = new RoomsDisplayViewModel()
                {
                    Name = revitRoomData.Name,
                    Number = revitRoomData.Number,
                    Level = (revitRoomData.Document.GetElement(revitRoomData.LevelId) as Level).Name,
                    DepartmentName = currentRoomData.GetProperty(revitRoomData, BuiltInParameter.ROOM_DEPARTMENT),
                    Area = Double.Parse(currentRoomData.GetProperty(revitRoomData, BuiltInParameter.ROOM_AREA)),
                };

                RoomInfoList.Add(item);
            }

            grdRoomInfo.ItemsSource = RoomInfoList;
        }

        #endregion

        #region Functionality

        /// <summary>
        /// retrive all elements in T type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private List<T> FilterElement<T>()
        {
            ElementClassFilter elementFilter = new ElementClassFilter(typeof(T));
            FilteredElementCollector collector = new FilteredElementCollector(dbDocument);
            collector = collector.WherePasses(elementFilter);
            return collector.Cast<T>().ToList();
        }

        private void showMessage(string message)
        {
            lblNotification.Content = string.Format("{0}, at {1}", message, DateTime.Now.ToLongTimeString());
        }

        /// <summary>
        /// Find all WALL elements and apply current selected patern
        /// </summary>
        private void fillPatern()
        {
            FilteredElementCollector collector = new FilteredElementCollector(dbDocument);
            ElementClassFilter filter = new ElementClassFilter(typeof(Wall));
            Transaction trans = new Transaction(dbDocument);

            var patternID = ((FillPatternElement)lstPatern.SelectedItem).Id;
            trans.Start("Apply fillpattern to Wall");
            IList<Element> exWalls = collector.WherePasses(filter)
                .WhereElementIsNotElementType().ToElements();

            foreach (Wall targetWall in exWalls)
            {
                try
                {
                    var materialID = targetWall.GetMaterialIds(false).FirstOrDefault<ElementId>();
                    if (materialID == null)
                        continue;

                    Material targetMaterial = dbDocument.GetElement(materialID) as Material;

                    targetMaterial.SurfaceForegroundPatternId = patternID;
                    targetMaterial.SurfaceBackgroundPatternId = patternID;
                }
                catch (Exception ex)
                {
                    showMessage(ex.Message);
                }
            }

            trans.Commit();
        }

        /// <summary>
        /// Hide current window and select user click point to create new symbol on selected location
        /// </summary>
        private void addNewElement()
        {
            Transaction trans = new Transaction(dbDocument);
            this.Hide();
            revitPreview.Dispose();

            try
            {
                trans.Start("Set active view");

                if (currentUIApplication.ActiveUIDocument.ActiveView.ViewType == ViewType.ThreeD)
                {
                    Plane plane = Plane.CreateByNormalAndOrigin(currentUIApplication.ActiveUIDocument.ActiveView.RightDirection,
                        currentUIApplication.ActiveUIDocument.ActiveView.UpDirection);
                    SketchPlane sp = SketchPlane.Create(currentUIApplication.ActiveUIDocument.Document, plane);
                    currentUIApplication.ActiveUIDocument.ActiveView.SketchPlane = sp;
                    currentUIApplication.ActiveUIDocument.ActiveView.ShowActiveWorkPlane();
                }

                XYZ newEntityPosition = currentUIApplication.ActiveUIDocument.Selection.PickPoint("Pick Symbol Position");

                // Find specefic symbol to add in view
                FilteredElementCollector collector = new FilteredElementCollector(dbDocument);
                FamilySymbol symbol = collector.OfClass(typeof(FamilySymbol))
                    .WhereElementIsElementType()
                    .Cast<FamilySymbol>()
                    .First(x => x.Name.Contains("Table"));


                if (!symbol.IsActive)
                    symbol.Activate();

                dbDocument.Create.NewFamilyInstance(newEntityPosition, symbol,
                    Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.RollBack();
                showMessage(ex.Message);
            }
            setViewOptions(currentViewMod);
            this.Show();
        }


        #endregion

        #region Events

        /// <summary>
        /// change plot display mod
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RbtnView_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (revitPreview != null)
                    revitPreview.Dispose();

                RadioButton rbtn = e.Source as RadioButton;
                currentViewMod = ((DBViewModel)rbtn.Tag).Id;
                
                revitPreview = new PreviewControl(dbDocument, currentViewMod);
                pnlContainer.Child = revitPreview;

                var fView = new FilteredElementCollector(currentUIApplication.ActiveUIDocument.Document)
                    .OfClass(typeof(View)).Cast<View>().Where(x => x.Id.IntegerValue == currentViewMod.IntegerValue).FirstOrDefault();
                IsWallHide = currentUIApplication.ActiveUIDocument.
                    ActiveView.IsInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);

                currentUIApplication.ActiveUIDocument.ActiveView = fView;
            }
            catch (Exception ex)
            {
                showMessage(ex.Message);
            }
        }

        private void ElementDisplayChange_CheckChange(object sender, RoutedEventArgs e)
        {
            CheckBox chkBox = (CheckBox)e.Source;
            var model = chkBox.Tag;
            dbDocument.ActiveView.EnableRevealHiddenMode();

            try
            {
                if (model is Element)
                {
                    if (dbDocument.ActiveView.CanCategoryBeHidden(((Element)model).Id))
                    {
                        if (chkBox.IsChecked != true)
                            dbDocument.ActiveView.HideCategoryTemporary(((Element)model).Id);
                        else
                            dbDocument.ActiveView.RemoveFilter(((Element)model).Id);

                        showMessage("Call hide category");
                    }
                    else
                        showMessage("Categroy can't be hide");
                }
                else
                {
                    if (chkBox.IsChecked == true)
                        dbDocument.ActiveView.HideElementTemporary((ElementId)model);
                    else
                        dbDocument.ActiveView.RemoveFilter((ElementId)model);

                    showMessage("Call hide element");
                }
            }
            catch (Exception ex)
            {
                showMessage(ex.Message);
            }
            dbDocument.ActiveView.DisableTemporaryViewMode(TemporaryViewMode.RevealHiddenElements);
        }

        /// <summary>
        /// Display selected treeview node element property 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrvItems_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null)
                return;

            var items = ((TreeViewModel)e.NewValue).DataSource;
            if (items is ElementId)
            {
                propertyGrid.SelectedObject = null;
                return;
            }
            propertyGrid.SelectedObject = items;
        }

        /// <summary>
        /// Reload treeview elements by next page data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (startElementPagingIndex >= (enumKeys.Length - 50))
                return;

            startElementPagingIndex += 50;
            loadItemsList();
        }

        /// <summary>
        /// Reload treeview elements by previous page data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (startElementPagingIndex == 0)
                return;

            startElementPagingIndex -= 50;
            loadItemsList();
        }

        /// <summary>
        /// Hide/Show all Wall elements in current view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnHideWalls_Click(object sender, RoutedEventArgs e)
        {
            
            if (IsWallHide)
            {
                TemporaryViewMode tempView = TemporaryViewMode.TemporaryHideIsolate;
                dbDocument.ActiveView.DisableTemporaryViewMode(tempView);
                IsWallHide = currentUIApplication.ActiveUIDocument.ActiveView.IsInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);

                return;
            }

            FilteredElementCollector collector = new FilteredElementCollector(dbDocument);
            ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
            IList<Element> items = collector.WherePasses(filter)
                .WhereElementIsNotElementType().ToElements();

            dbDocument.ActiveView.EnableRevealHiddenMode();
            try
            {
                foreach (Element item in items)
                {
                    dbDocument.ActiveView.HideElementTemporary(item.Id);
                }
            }
            catch (Exception ex)
            {
                showMessage(ex.Message);
            }
            IsWallHide = currentUIApplication.ActiveUIDocument.ActiveView.IsInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
            dbDocument.ActiveView.DisableTemporaryViewMode(TemporaryViewMode.RevealHiddenElements);
        }

        /// <summary>
        /// Pick new element from view and display information about this item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSelectItem_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            revitPreview.Dispose();
            try
            {

                var elements = currentUIApplication.ActiveUIDocument.Selection
                    .PickObject(ObjectType.Element, "Select your element!");
                propertyGrid.SelectedObject = elements;
            }
            catch (Exception ex)
            {
                showMessage(ex.Message);
            }
            setViewOptions(currentViewMod);
            this.Show();
        }

        /// <summary>
        /// Add new element in user selected point
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            //Create an Revit ExternalEvent 
            currentCommandType = ExternalCommandType.ADD_ENTITY;
            exEvent.Raise();
        }

        /// <summary>
        /// Change walls interior design patern
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSetPattern_Click(object sender, RoutedEventArgs e)
        {
            if (lstPatern.SelectedItem == null)
            {
                TaskDialog.Show("Error",
                    "Please select pattern to apply");
                return;
            }
            if (currentRoomData == null)
                return;

            //Create an Revit ExternalEvent 
            currentCommandType = ExternalCommandType.FILL_PATERN;
            exEvent.Raise();
        }

        #endregion


        #region IExternalEventHandler

        /// <summary>
        /// The Revit API provides an External Events framework to accommodate the use of modeless dialogs. 
        /// It is tailored for asynchronous processing and operates similarly to the Idling event with default frequency.
        /// </summary>
        /// <param name="app"></param>
        public void Execute(UIApplication app)
        {
            switch (currentCommandType)
            {
                case ExternalCommandType.ADD_ENTITY:
                    addNewElement();
                    return;
                case ExternalCommandType.FILL_PATERN:
                    fillPatern();
                    return;
            }
        }

        public string GetName()
        {
            return "RevitPlugin.Calculator.ElementCalculator";
        }

        #endregion
    }

    internal enum ExternalCommandType
    {
        FILL_PATERN = 1,
        ADD_ENTITY = 2
    }
}
