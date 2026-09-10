using DevExpress.XtraBars;
using DevExpress.XtraBars.Docking;
using DevExpress.XtraReports.UserDesigner;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SmartAuditERP
{
    partial class frm_ReportDesign
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        public XRDesignMdiController ReportDesigner1;
        public XRDesignBarManager XrDesignBarManager1;
        public Bar Bar2;
        public DesignBar DesignBar1;
        public BarSubItem msiFile;
        public CommandBarItem CommandBarItem1;
        public CommandBarItem CommandBarItem2;
        public CommandBarItem bbiOpenFile;
        public CommandBarItem bbiSaveFile;
        public CommandBarItem CommandBarItem3;
        public CommandBarItem CommandBarItem7;
        public CommandBarItem CommandBarItem11;
        public CommandBarItem CommandBarItem4;
        public BarSubItem msiEdit;
        public CommandBarItem bbiUndo;
        public CommandBarItem bbiRedo;
        public CommandBarItem bbiCut;
        public CommandBarItem bbiCopy;
        public CommandBarItem bbiPaste;
        public CommandBarItem CommandBarItem5;
        public CommandBarItem CommandBarItem6;
        public BarSubItem msiTabButtons;
        public BarReportTabButtonsListItem BarReportTabButtonsListItem1;
        public BarSubItem BarSubItem1;
        public XRBarToolbarsListItem XrBarToolbarsListItem1;
        public BarSubItem BarSubItem2;
        public BarDockPanelsListItem BarDockPanelsListItem1;
        public BarSubItem msiFormat;
        public CommandColorBarItem bbiForeColor;
        public CommandColorBarItem bbiBackColor;
        public BarSubItem msiFont;
        public CommandBarItem bbiFontBold;
        public CommandBarItem bbiFontItalic;
        public CommandBarItem bbiFontUnderline;
        public BarSubItem msiJustify;
        public CommandBarItem bbiJustifyLeft;
        public CommandBarItem bbiJustifyCenter;
        public CommandBarItem bbiJustifyRight;
        public CommandBarItem bbiJustifyJustify;
        public BarSubItem msiAlign;
        public CommandBarItem bbiAlignLeft;
        public CommandBarItem bbiAlignVerticalCenters;
        public CommandBarItem bbiAlignRight;
        public CommandBarItem bbiAlignTop;
        public CommandBarItem bbiAlignHorizontalCenters;
        public CommandBarItem bbiAlignBottom;
        public CommandBarItem bbiAlignToGrid;
        public BarSubItem msiSameSize;
        public CommandBarItem bbiSizeToControlWidth;
        public CommandBarItem bbiSizeToGrid;
        public CommandBarItem bbiSizeToControlHeight;
        public CommandBarItem bbiSizeToControl;
        public BarSubItem msiHorizontalSpacing;
        public CommandBarItem bbiHorizSpaceMakeEqual;
        public CommandBarItem bbiHorizSpaceIncrease;
        public CommandBarItem bbiHorizSpaceDecrease;
        public CommandBarItem bbiHorizSpaceConcatenate;
        public BarSubItem msiVerticalSpacing;
        public CommandBarItem bbiVertSpaceMakeEqual;
        public CommandBarItem bbiVertSpaceIncrease;
        public CommandBarItem bbiVertSpaceDecrease;
        public CommandBarItem bbiVertSpaceConcatenate;
        public BarSubItem bsiCenter;
        public CommandBarItem bbiCenterHorizontally;
        public CommandBarItem bbiCenterVertically;
        public BarSubItem msiOrder;
        public CommandBarItem bbiBringToFront;
        public CommandBarItem bbiSendToBack;
        public BarSubItem msiWindow;
        public CommandBarCheckItem msiWindowInterface;
        public CommandBarItem CommandBarItem8;
        public CommandBarItem CommandBarItem9;
        public CommandBarItem CommandBarItem10;
        public BarMdiChildrenListItem msiWindows;
        public DesignBar DesignBar2;
        public DesignBar DesignBar3;
        public BarEditItem beiFontName;
        public RecentlyUsedItemsComboBox RecentlyUsedItemsComboBox1;
        public BarEditItem beiFontSize;
        public DesignRepositoryItemComboBox DesignRepositoryItemComboBox1;
        public DesignBar DesignBar4;
        public DesignBar DesignBar5;
        public BarStaticItem bsiHint;
        public Bar Bar1;
        public CommandBarItem bbiZoomOut;
        public XRZoomBarEditItem bbiZoom;
        public DesignRepositoryItemComboBox DesignRepositoryItemComboBox2;
        public CommandBarItem bbiZoomIn;
        public BarDockControl barDockControlTop;
        public BarDockControl barDockControlBottom;
        public BarDockControl barDockControlLeft;
        public BarDockControl barDockControlRight;
        public XRDesignDockManager XrDesignDockManager1;
        public DockPanel panelContainer4;
        public GroupAndSortDockPanel GroupAndSortDockPanel1;
        public DesignControlContainer GroupAndSortDockPanel1_Container;
        public ErrorListDockPanel ErrorListDockPanel1;
        public DesignControlContainer ErrorListDockPanel1_Container;
        public DockPanel panelContainer1;
        public DockPanel panelContainer2;
        public ReportExplorerDockPanel ReportExplorerDockPanel1;
        public DesignControlContainer ReportExplorerDockPanel1_Container;
        public FieldListDockPanel FieldListDockPanel1;
        public DesignControlContainer FieldListDockPanel1_Container;
        public DockPanel panelContainer3;
        public PropertyGridDockPanel PropertyGridDockPanel1;
        public DesignControlContainer PropertyGridDockPanel1_Container;
        public ReportGalleryDockPanel ReportGalleryDockPanel1;
        public DesignControlContainer ReportGalleryDockPanel1_Container;

        void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            BarInfo barInfo1 = new BarInfo();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frm_ReportDesign));
            XRDesignPanelListener xrDesignPanelListener1 = new XRDesignPanelListener();
            XRDesignPanelListener xrDesignPanelListener2 = new XRDesignPanelListener();
            XRDesignPanelListener xrDesignPanelListener3 = new XRDesignPanelListener();
            XRDesignPanelListener xrDesignPanelListener4 = new XRDesignPanelListener();
            XRDesignPanelListener xrDesignPanelListener5 = new XRDesignPanelListener();
            XRDesignPanelListener xrDesignPanelListener6 = new XRDesignPanelListener();
            XRDesignPanelListener xrDesignPanelListener7 = new XRDesignPanelListener();
            XRDesignPanelListener xrDesignPanelListener8 = new XRDesignPanelListener();
            Bar2 = new Bar();
            XrDesignBarManager1 = new XRDesignBarManager(components);
            DesignBar1 = new DesignBar();
            msiFile = new BarSubItem();
            CommandBarItem1 = new CommandBarItem();
            CommandBarItem2 = new CommandBarItem();
            bbiOpenFile = new CommandBarItem();
            bbiSaveFile = new CommandBarItem();
            CommandBarItem3 = new CommandBarItem();
            CommandBarItem7 = new CommandBarItem();
            CommandBarItem11 = new CommandBarItem();
            CommandBarItem4 = new CommandBarItem();
            msiEdit = new BarSubItem();
            bbiUndo = new CommandBarItem();
            bbiRedo = new CommandBarItem();
            bbiCut = new CommandBarItem();
            bbiCopy = new CommandBarItem();
            bbiPaste = new CommandBarItem();
            CommandBarItem5 = new CommandBarItem();
            CommandBarItem6 = new CommandBarItem();
            msiTabButtons = new BarSubItem();
            BarReportTabButtonsListItem1 = new BarReportTabButtonsListItem();
            BarSubItem1 = new BarSubItem();
            XrBarToolbarsListItem1 = new XRBarToolbarsListItem();
            BarSubItem2 = new BarSubItem();
            BarDockPanelsListItem1 = new BarDockPanelsListItem();
            msiFormat = new BarSubItem();
            bbiForeColor = new CommandColorBarItem();
            bbiBackColor = new CommandColorBarItem();
            msiFont = new BarSubItem();
            bbiFontBold = new CommandBarItem();
            bbiFontItalic = new CommandBarItem();
            bbiFontUnderline = new CommandBarItem();
            msiJustify = new BarSubItem();
            bbiJustifyLeft = new CommandBarItem();
            bbiJustifyCenter = new CommandBarItem();
            bbiJustifyRight = new CommandBarItem();
            bbiJustifyJustify = new CommandBarItem();
            msiAlign = new BarSubItem();
            bbiAlignLeft = new CommandBarItem();
            bbiAlignVerticalCenters = new CommandBarItem();
            bbiAlignRight = new CommandBarItem();
            bbiAlignTop = new CommandBarItem();
            bbiAlignHorizontalCenters = new CommandBarItem();
            bbiAlignBottom = new CommandBarItem();
            bbiAlignToGrid = new CommandBarItem();
            msiSameSize = new BarSubItem();
            bbiSizeToControlWidth = new CommandBarItem();
            bbiSizeToGrid = new CommandBarItem();
            bbiSizeToControlHeight = new CommandBarItem();
            bbiSizeToControl = new CommandBarItem();
            msiHorizontalSpacing = new BarSubItem();
            bbiHorizSpaceMakeEqual = new CommandBarItem();
            bbiHorizSpaceIncrease = new CommandBarItem();
            bbiHorizSpaceDecrease = new CommandBarItem();
            bbiHorizSpaceConcatenate = new CommandBarItem();
            msiVerticalSpacing = new BarSubItem();
            bbiVertSpaceMakeEqual = new CommandBarItem();
            bbiVertSpaceIncrease = new CommandBarItem();
            bbiVertSpaceDecrease = new CommandBarItem();
            bbiVertSpaceConcatenate = new CommandBarItem();
            bsiCenter = new BarSubItem();
            bbiCenterHorizontally = new CommandBarItem();
            bbiCenterVertically = new CommandBarItem();
            msiOrder = new BarSubItem();
            bbiBringToFront = new CommandBarItem();
            bbiSendToBack = new CommandBarItem();
            msiWindow = new BarSubItem();
            msiWindowInterface = new CommandBarCheckItem();
            CommandBarItem8 = new CommandBarItem();
            CommandBarItem9 = new CommandBarItem();
            CommandBarItem10 = new CommandBarItem();
            msiWindows = new BarMdiChildrenListItem();
            DesignBar2 = new DesignBar();
            DesignBar3 = new DesignBar();
            beiFontName = new BarEditItem();
            RecentlyUsedItemsComboBox1 = new RecentlyUsedItemsComboBox();
            beiFontSize = new BarEditItem();
            DesignRepositoryItemComboBox1 = new DesignRepositoryItemComboBox();
            DesignBar4 = new DesignBar();
            DesignBar5 = new DesignBar();
            bsiHint = new BarStaticItem();
            Bar1 = new Bar();
            bbiZoomOut = new CommandBarItem();
            bbiZoom = new XRZoomBarEditItem();
            DesignRepositoryItemComboBox2 = new DesignRepositoryItemComboBox();
            bbiZoomIn = new CommandBarItem();
            barDockControlTop = new BarDockControl();
            barDockControlBottom = new BarDockControl();
            barDockControlLeft = new BarDockControl();
            barDockControlRight = new BarDockControl();
            XrDesignDockManager1 = new XRDesignDockManager(components);
            panelContainer1 = new DockPanel();
            panelContainer2 = new DockPanel();
            ReportExplorerDockPanel1 = new ReportExplorerDockPanel();
            ReportExplorerDockPanel1_Container = new DesignControlContainer();
            FieldListDockPanel1 = new FieldListDockPanel();
            FieldListDockPanel1_Container = new DesignControlContainer();
            panelContainer3 = new DockPanel();
            PropertyGridDockPanel1 = new PropertyGridDockPanel();
            PropertyGridDockPanel1_Container = new DesignControlContainer();
            ReportGalleryDockPanel1 = new ReportGalleryDockPanel();
            ReportGalleryDockPanel1_Container = new DesignControlContainer();
            panelContainer4 = new DockPanel();
            GroupAndSortDockPanel1 = new GroupAndSortDockPanel();
            GroupAndSortDockPanel1_Container = new DesignControlContainer();
            ErrorListDockPanel1 = new ErrorListDockPanel();
            ErrorListDockPanel1_Container = new DesignControlContainer();
            ReportDesigner1 = new XRDesignMdiController(components);
            ((System.ComponentModel.ISupportInitialize)XrDesignBarManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)RecentlyUsedItemsComboBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)DesignRepositoryItemComboBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)DesignRepositoryItemComboBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)XrDesignDockManager1).BeginInit();
            panelContainer1.SuspendLayout();
            panelContainer2.SuspendLayout();
            ReportExplorerDockPanel1.SuspendLayout();
            FieldListDockPanel1.SuspendLayout();
            panelContainer3.SuspendLayout();
            PropertyGridDockPanel1.SuspendLayout();
            ReportGalleryDockPanel1.SuspendLayout();
            panelContainer4.SuspendLayout();
            GroupAndSortDockPanel1.SuspendLayout();
            ErrorListDockPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)ReportDesigner1).BeginInit();
            SuspendLayout();
            // 
            // Bar2
            // 
            Bar2.BarName = "Toolbox";
            Bar2.DockCol = 0;
            Bar2.DockRow = 0;
            Bar2.DockStyle = BarDockStyle.Left;
            Bar2.OptionsBar.AllowQuickCustomization = false;
            Bar2.Text = "Standard Controls";
            // 
            // XrDesignBarManager1
            // 
            barInfo1.Bar = Bar2;
            barInfo1.ToolboxType = ToolboxType.Standard;
            XrDesignBarManager1.BarInfos.AddRange(new BarInfo[] { barInfo1 });
            XrDesignBarManager1.Bars.AddRange(new Bar[] { DesignBar1, DesignBar2, DesignBar3, DesignBar4, DesignBar5, Bar1, Bar2 });
            XrDesignBarManager1.DockControls.Add(barDockControlTop);
            XrDesignBarManager1.DockControls.Add(barDockControlBottom);
            XrDesignBarManager1.DockControls.Add(barDockControlLeft);
            XrDesignBarManager1.DockControls.Add(barDockControlRight);
            XrDesignBarManager1.DockManager = XrDesignDockManager1;
            XrDesignBarManager1.FontNameBox = RecentlyUsedItemsComboBox1;
            XrDesignBarManager1.FontNameEdit = beiFontName;
            XrDesignBarManager1.FontSizeBox = DesignRepositoryItemComboBox1;
            XrDesignBarManager1.FontSizeEdit = beiFontSize;
            XrDesignBarManager1.Form = this;
            XrDesignBarManager1.FormattingToolbar = DesignBar3;
            XrDesignBarManager1.HintStaticItem = bsiHint;
            XrDesignBarManager1.ImageStream = (DevExpress.Utils.ImageCollectionStreamer)resources.GetObject("XrDesignBarManager1.ImageStream");
            XrDesignBarManager1.Items.AddRange(new BarItem[] { beiFontName, beiFontSize, bbiFontBold, bbiFontItalic, bbiFontUnderline, bbiForeColor, bbiBackColor, bbiJustifyLeft, bbiJustifyCenter, bbiJustifyRight, bbiJustifyJustify, bbiAlignToGrid, bbiAlignLeft, bbiAlignVerticalCenters, bbiAlignRight, bbiAlignTop, bbiAlignHorizontalCenters, bbiAlignBottom, bbiSizeToControlWidth, bbiSizeToGrid, bbiSizeToControlHeight, bbiSizeToControl, bbiHorizSpaceMakeEqual, bbiHorizSpaceIncrease, bbiHorizSpaceDecrease, bbiHorizSpaceConcatenate, bbiVertSpaceMakeEqual, bbiVertSpaceIncrease, bbiVertSpaceDecrease, bbiVertSpaceConcatenate, bbiCenterHorizontally, bbiCenterVertically, bbiBringToFront, bbiSendToBack, CommandBarItem1, bbiOpenFile, bbiSaveFile, bbiCut, bbiCopy, bbiPaste, bbiUndo, bbiRedo, bsiHint, msiFile, msiEdit, msiTabButtons, BarReportTabButtonsListItem1, BarSubItem1, XrBarToolbarsListItem1, BarSubItem2, BarDockPanelsListItem1, msiFormat, msiFont, msiJustify, msiAlign, msiSameSize, msiHorizontalSpacing, msiVerticalSpacing, bsiCenter, msiOrder, CommandBarItem2, CommandBarItem3, CommandBarItem4, CommandBarItem5, CommandBarItem6, CommandBarItem7, msiWindow, msiWindowInterface, CommandBarItem8, CommandBarItem9, CommandBarItem10, msiWindows, CommandBarItem11, bbiZoomOut, bbiZoom, bbiZoomIn });
            XrDesignBarManager1.LayoutToolbar = DesignBar4;
            XrDesignBarManager1.MainMenu = DesignBar1;
            XrDesignBarManager1.MaxItemId = 76;
            XrDesignBarManager1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { RecentlyUsedItemsComboBox1, DesignRepositoryItemComboBox1, DesignRepositoryItemComboBox2 });
            XrDesignBarManager1.StatusBar = DesignBar5;
            XrDesignBarManager1.Toolbar = DesignBar2;
            XrDesignBarManager1.TransparentEditorsMode = DevExpress.Utils.DefaultBoolean.True;
            XrDesignBarManager1.Updates.AddRange(new string[] { "Toolbox" });
            XrDesignBarManager1.ZoomItem = bbiZoom;
            // 
            // DesignBar1
            // 
            DesignBar1.BarName = "Main Menu";
            DesignBar1.DockCol = 0;
            DesignBar1.DockRow = 0;
            DesignBar1.DockStyle = BarDockStyle.Top;
            DesignBar1.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(msiFile), new LinkPersistInfo(msiEdit), new LinkPersistInfo(msiTabButtons), new LinkPersistInfo(msiFormat), new LinkPersistInfo(msiWindow) });
            DesignBar1.OptionsBar.MultiLine = true;
            DesignBar1.OptionsBar.UseWholeRow = true;
            DesignBar1.Text = "Main Menu";
            // 
            // msiFile
            // 
            msiFile.Caption = "&File";
            msiFile.Id = 43;
            msiFile.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(CommandBarItem1), new LinkPersistInfo(CommandBarItem2), new LinkPersistInfo(bbiOpenFile), new LinkPersistInfo(bbiSaveFile, true), new LinkPersistInfo(CommandBarItem3), new LinkPersistInfo(CommandBarItem7), new LinkPersistInfo(CommandBarItem11), new LinkPersistInfo(CommandBarItem4, true) });
            msiFile.Name = "msiFile";
            // 
            // CommandBarItem1
            // 
            CommandBarItem1.Caption = "&New";
            CommandBarItem1.Command = ReportCommand.NewReport;
            CommandBarItem1.Enabled = false;
            CommandBarItem1.Hint = "Create a new blank report";
            CommandBarItem1.Id = 34;
            CommandBarItem1.ItemShortcut = new BarShortcut(Keys.Control | Keys.N);
            CommandBarItem1.Name = "CommandBarItem1";
            // 
            // CommandBarItem2
            // 
            CommandBarItem2.Caption = "New via &Wizard...";
            CommandBarItem2.Command = ReportCommand.NewReportWizard;
            CommandBarItem2.Enabled = false;
            CommandBarItem2.Hint = "Create a new report using the Wizard";
            CommandBarItem2.Id = 60;
            CommandBarItem2.ItemShortcut = new BarShortcut(Keys.Control | Keys.W);
            CommandBarItem2.Name = "CommandBarItem2";
            // 
            // bbiOpenFile
            // 
            bbiOpenFile.Caption = "&Open...";
            bbiOpenFile.Command = ReportCommand.OpenFile;
            bbiOpenFile.Enabled = false;
            bbiOpenFile.Hint = "Open a report";
            bbiOpenFile.Id = 35;
            bbiOpenFile.ItemShortcut = new BarShortcut(Keys.Control | Keys.O);
            bbiOpenFile.Name = "bbiOpenFile";
            // 
            // bbiSaveFile
            // 
            bbiSaveFile.Caption = "&Save";
            bbiSaveFile.Command = ReportCommand.SaveFile;
            bbiSaveFile.Enabled = false;
            bbiSaveFile.Hint = "Save the report";
            bbiSaveFile.Id = 36;
            bbiSaveFile.ItemShortcut = new BarShortcut(Keys.Control | Keys.S);
            bbiSaveFile.Name = "bbiSaveFile";
            // 
            // CommandBarItem3
            // 
            CommandBarItem3.Caption = "Save &As...";
            CommandBarItem3.Command = ReportCommand.SaveFileAs;
            CommandBarItem3.Enabled = false;
            CommandBarItem3.Hint = "Save the report with a new name";
            CommandBarItem3.Id = 61;
            CommandBarItem3.Name = "CommandBarItem3";
            // 
            // CommandBarItem7
            // 
            CommandBarItem7.Caption = "Save A&ll";
            CommandBarItem7.Command = ReportCommand.SaveAll;
            CommandBarItem7.Enabled = false;
            CommandBarItem7.Hint = "Save all reports";
            CommandBarItem7.Id = 65;
            CommandBarItem7.ItemShortcut = new BarShortcut(Keys.Control | Keys.L);
            CommandBarItem7.Name = "CommandBarItem7";
            // 
            // CommandBarItem11
            // 
            CommandBarItem11.Caption = "&Close";
            CommandBarItem11.Command = ReportCommand.Close;
            CommandBarItem11.Enabled = false;
            CommandBarItem11.Hint = "Close the report";
            CommandBarItem11.Id = 72;
            CommandBarItem11.ItemShortcut = new BarShortcut(Keys.Control | Keys.F4);
            CommandBarItem11.Name = "CommandBarItem11";
            // 
            // CommandBarItem4
            // 
            CommandBarItem4.Caption = "E&xit";
            CommandBarItem4.Command = ReportCommand.Exit;
            CommandBarItem4.Enabled = false;
            CommandBarItem4.Hint = "Close the designer";
            CommandBarItem4.Id = 62;
            CommandBarItem4.Name = "CommandBarItem4";
            // 
            // msiEdit
            // 
            msiEdit.Caption = "&Edit";
            msiEdit.Id = 44;
            msiEdit.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiUndo, true), new LinkPersistInfo(bbiRedo), new LinkPersistInfo(bbiCut, true), new LinkPersistInfo(bbiCopy), new LinkPersistInfo(bbiPaste), new LinkPersistInfo(CommandBarItem5), new LinkPersistInfo(CommandBarItem6, true) });
            msiEdit.Name = "msiEdit";
            // 
            // bbiUndo
            // 
            bbiUndo.Caption = "&Undo";
            bbiUndo.Command = ReportCommand.Undo;
            bbiUndo.Enabled = false;
            bbiUndo.Hint = "Undo the last operation";
            bbiUndo.Id = 40;
            bbiUndo.ItemShortcut = new BarShortcut(Keys.Control | Keys.Z);
            bbiUndo.Name = "bbiUndo";
            // 
            // bbiRedo
            // 
            bbiRedo.Caption = "&Redo";
            bbiRedo.Command = ReportCommand.Redo;
            bbiRedo.Enabled = false;
            bbiRedo.Hint = "Redo the last operation";
            bbiRedo.Id = 41;
            bbiRedo.ItemShortcut = new BarShortcut(Keys.Control | Keys.Y);
            bbiRedo.Name = "bbiRedo";
            // 
            // bbiCut
            // 
            bbiCut.Caption = "Cu&t";
            bbiCut.Command = ReportCommand.Cut;
            bbiCut.Enabled = false;
            bbiCut.Hint = "Delete the control and copy it to the clipboard";
            bbiCut.Id = 37;
            bbiCut.ItemShortcut = new BarShortcut(Keys.Control | Keys.X);
            bbiCut.Name = "bbiCut";
            // 
            // bbiCopy
            // 
            bbiCopy.Caption = "&Copy";
            bbiCopy.Command = ReportCommand.Copy;
            bbiCopy.Enabled = false;
            bbiCopy.Hint = "Copy the control to the clipboard";
            bbiCopy.Id = 38;
            bbiCopy.ItemShortcut = new BarShortcut(Keys.Control | Keys.C);
            bbiCopy.Name = "bbiCopy";
            // 
            // bbiPaste
            // 
            bbiPaste.Caption = "&Paste";
            bbiPaste.Command = ReportCommand.Paste;
            bbiPaste.Enabled = false;
            bbiPaste.Hint = "Add the control from the clipboard";
            bbiPaste.Id = 39;
            bbiPaste.ItemShortcut = new BarShortcut(Keys.Control | Keys.V);
            bbiPaste.Name = "bbiPaste";
            // 
            // CommandBarItem5
            // 
            CommandBarItem5.Caption = "&Delete";
            CommandBarItem5.Command = ReportCommand.Delete;
            CommandBarItem5.Enabled = false;
            CommandBarItem5.Hint = "Delete the control";
            CommandBarItem5.Id = 63;
            CommandBarItem5.Name = "CommandBarItem5";
            // 
            // CommandBarItem6
            // 
            CommandBarItem6.Caption = "Select &All";
            CommandBarItem6.Command = ReportCommand.SelectAll;
            CommandBarItem6.Enabled = false;
            CommandBarItem6.Hint = "Select all the controls in the document";
            CommandBarItem6.Id = 64;
            CommandBarItem6.ItemShortcut = new BarShortcut(Keys.Control | Keys.A);
            CommandBarItem6.Name = "CommandBarItem6";
            // 
            // msiTabButtons
            // 
            msiTabButtons.Caption = "&View";
            msiTabButtons.Id = 45;
            msiTabButtons.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(BarReportTabButtonsListItem1), new LinkPersistInfo(BarSubItem1, true), new LinkPersistInfo(BarSubItem2, true) });
            msiTabButtons.Name = "msiTabButtons";
            // 
            // BarReportTabButtonsListItem1
            // 
            BarReportTabButtonsListItem1.Caption = "Tab Buttons";
            BarReportTabButtonsListItem1.Id = 46;
            BarReportTabButtonsListItem1.Name = "BarReportTabButtonsListItem1";
            // 
            // BarSubItem1
            // 
            BarSubItem1.Caption = "&Toolbars";
            BarSubItem1.Id = 47;
            BarSubItem1.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(XrBarToolbarsListItem1) });
            BarSubItem1.Name = "BarSubItem1";
            // 
            // XrBarToolbarsListItem1
            // 
            XrBarToolbarsListItem1.Caption = "&Toolbars";
            XrBarToolbarsListItem1.Id = 48;
            XrBarToolbarsListItem1.Name = "XrBarToolbarsListItem1";
            // 
            // BarSubItem2
            // 
            BarSubItem2.Caption = "&Windows";
            BarSubItem2.Id = 49;
            BarSubItem2.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(BarDockPanelsListItem1) });
            BarSubItem2.Name = "BarSubItem2";
            // 
            // BarDockPanelsListItem1
            // 
            BarDockPanelsListItem1.Caption = "&Windows";
            BarDockPanelsListItem1.Id = 50;
            BarDockPanelsListItem1.Name = "BarDockPanelsListItem1";
            BarDockPanelsListItem1.ShowCustomizationItem = false;
            BarDockPanelsListItem1.ShowDockPanels = true;
            BarDockPanelsListItem1.ShowToolbars = false;
            // 
            // msiFormat
            // 
            msiFormat.Caption = "Fo&rmat";
            msiFormat.Id = 51;
            msiFormat.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiForeColor), new LinkPersistInfo(bbiBackColor), new LinkPersistInfo(msiFont, true), new LinkPersistInfo(msiJustify), new LinkPersistInfo(msiAlign, true), new LinkPersistInfo(msiSameSize), new LinkPersistInfo(msiHorizontalSpacing, true), new LinkPersistInfo(msiVerticalSpacing), new LinkPersistInfo(bsiCenter, true), new LinkPersistInfo(msiOrder, true) });
            msiFormat.Name = "msiFormat";
            // 
            // bbiForeColor
            // 
            bbiForeColor.ButtonStyle = BarButtonStyle.DropDown;
            bbiForeColor.Caption = "For&eground Color";
            bbiForeColor.CloseSubMenuOnClickMode = DevExpress.Utils.DefaultBoolean.False;
            bbiForeColor.Command = ReportCommand.ForeColor;
            bbiForeColor.Enabled = false;
            bbiForeColor.Hint = "Set the foreground color of the control";
            bbiForeColor.Id = 5;
            bbiForeColor.Name = "bbiForeColor";
            // 
            // bbiBackColor
            // 
            bbiBackColor.ButtonStyle = BarButtonStyle.DropDown;
            bbiBackColor.Caption = "Bac&kground Color";
            bbiBackColor.CloseSubMenuOnClickMode = DevExpress.Utils.DefaultBoolean.False;
            bbiBackColor.Command = ReportCommand.BackColor;
            bbiBackColor.Enabled = false;
            bbiBackColor.Hint = "Set the background color of the control";
            bbiBackColor.Id = 6;
            bbiBackColor.Name = "bbiBackColor";
            // 
            // msiFont
            // 
            msiFont.Caption = "&Font";
            msiFont.Id = 52;
            msiFont.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiFontBold, true), new LinkPersistInfo(bbiFontItalic), new LinkPersistInfo(bbiFontUnderline) });
            msiFont.Name = "msiFont";
            // 
            // bbiFontBold
            // 
            bbiFontBold.Caption = "&Bold";
            bbiFontBold.Command = ReportCommand.FontBold;
            bbiFontBold.Enabled = false;
            bbiFontBold.Hint = "Make the font bold";
            bbiFontBold.Id = 2;
            bbiFontBold.ItemShortcut = new BarShortcut(Keys.Control | Keys.B);
            bbiFontBold.Name = "bbiFontBold";
            // 
            // bbiFontItalic
            // 
            bbiFontItalic.Caption = "&Italic";
            bbiFontItalic.Command = ReportCommand.FontItalic;
            bbiFontItalic.Enabled = false;
            bbiFontItalic.Hint = "Make the font italic";
            bbiFontItalic.Id = 3;
            bbiFontItalic.ItemShortcut = new BarShortcut(Keys.Control | Keys.I);
            bbiFontItalic.Name = "bbiFontItalic";
            // 
            // bbiFontUnderline
            // 
            bbiFontUnderline.Caption = "&Underline";
            bbiFontUnderline.Command = ReportCommand.FontUnderline;
            bbiFontUnderline.Enabled = false;
            bbiFontUnderline.Hint = "Underline the font";
            bbiFontUnderline.Id = 4;
            bbiFontUnderline.ItemShortcut = new BarShortcut(Keys.Control | Keys.U);
            bbiFontUnderline.Name = "bbiFontUnderline";
            // 
            // msiJustify
            // 
            msiJustify.Caption = "&Justify";
            msiJustify.Id = 53;
            msiJustify.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiJustifyLeft, true), new LinkPersistInfo(bbiJustifyCenter), new LinkPersistInfo(bbiJustifyRight), new LinkPersistInfo(bbiJustifyJustify) });
            msiJustify.Name = "msiJustify";
            // 
            // bbiJustifyLeft
            // 
            bbiJustifyLeft.Caption = "&Left";
            bbiJustifyLeft.Command = ReportCommand.JustifyLeft;
            bbiJustifyLeft.Enabled = false;
            bbiJustifyLeft.Hint = "Align the control's text to the left";
            bbiJustifyLeft.Id = 7;
            bbiJustifyLeft.Name = "bbiJustifyLeft";
            // 
            // bbiJustifyCenter
            // 
            bbiJustifyCenter.Caption = "&Center";
            bbiJustifyCenter.Command = ReportCommand.JustifyCenter;
            bbiJustifyCenter.Enabled = false;
            bbiJustifyCenter.Hint = "Align the control's text to the center";
            bbiJustifyCenter.Id = 8;
            bbiJustifyCenter.Name = "bbiJustifyCenter";
            // 
            // bbiJustifyRight
            // 
            bbiJustifyRight.Caption = "&Rights";
            bbiJustifyRight.Command = ReportCommand.JustifyRight;
            bbiJustifyRight.Enabled = false;
            bbiJustifyRight.Hint = "Align the control's text to the right";
            bbiJustifyRight.Id = 9;
            bbiJustifyRight.Name = "bbiJustifyRight";
            // 
            // bbiJustifyJustify
            // 
            bbiJustifyJustify.Caption = "&Justify";
            bbiJustifyJustify.Command = ReportCommand.JustifyJustify;
            bbiJustifyJustify.Enabled = false;
            bbiJustifyJustify.Hint = "Justify the control's text";
            bbiJustifyJustify.Id = 10;
            bbiJustifyJustify.Name = "bbiJustifyJustify";
            // 
            // msiAlign
            // 
            msiAlign.Caption = "&Align";
            msiAlign.Id = 54;
            msiAlign.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiAlignLeft, true), new LinkPersistInfo(bbiAlignVerticalCenters), new LinkPersistInfo(bbiAlignRight), new LinkPersistInfo(bbiAlignTop, true), new LinkPersistInfo(bbiAlignHorizontalCenters), new LinkPersistInfo(bbiAlignBottom), new LinkPersistInfo(bbiAlignToGrid, true) });
            msiAlign.Name = "msiAlign";
            // 
            // bbiAlignLeft
            // 
            bbiAlignLeft.Caption = "&Lefts";
            bbiAlignLeft.Command = ReportCommand.AlignLeft;
            bbiAlignLeft.Enabled = false;
            bbiAlignLeft.Hint = "Left align the selected controls";
            bbiAlignLeft.Id = 12;
            bbiAlignLeft.Name = "bbiAlignLeft";
            // 
            // bbiAlignVerticalCenters
            // 
            bbiAlignVerticalCenters.Caption = "&Centers";
            bbiAlignVerticalCenters.Command = ReportCommand.AlignVerticalCenters;
            bbiAlignVerticalCenters.Enabled = false;
            bbiAlignVerticalCenters.Hint = "Align the centers of the selected controls vertically";
            bbiAlignVerticalCenters.Id = 13;
            bbiAlignVerticalCenters.Name = "bbiAlignVerticalCenters";
            // 
            // bbiAlignRight
            // 
            bbiAlignRight.Caption = "&Rights";
            bbiAlignRight.Command = ReportCommand.AlignRight;
            bbiAlignRight.Enabled = false;
            bbiAlignRight.Hint = "Right align the selected controls";
            bbiAlignRight.Id = 14;
            bbiAlignRight.Name = "bbiAlignRight";
            // 
            // bbiAlignTop
            // 
            bbiAlignTop.Caption = "&Tops";
            bbiAlignTop.Command = ReportCommand.AlignTop;
            bbiAlignTop.Enabled = false;
            bbiAlignTop.Hint = "Align the tops of the selected controls";
            bbiAlignTop.Id = 15;
            bbiAlignTop.Name = "bbiAlignTop";
            // 
            // bbiAlignHorizontalCenters
            // 
            bbiAlignHorizontalCenters.Caption = "&Middles";
            bbiAlignHorizontalCenters.Command = ReportCommand.AlignHorizontalCenters;
            bbiAlignHorizontalCenters.Enabled = false;
            bbiAlignHorizontalCenters.Hint = "Align the centers of the selected controls horizontally";
            bbiAlignHorizontalCenters.Id = 16;
            bbiAlignHorizontalCenters.Name = "bbiAlignHorizontalCenters";
            // 
            // bbiAlignBottom
            // 
            bbiAlignBottom.Caption = "&Bottoms";
            bbiAlignBottom.Command = ReportCommand.AlignBottom;
            bbiAlignBottom.Enabled = false;
            bbiAlignBottom.Hint = "Align the bottoms of the selected controls";
            bbiAlignBottom.Id = 17;
            bbiAlignBottom.Name = "bbiAlignBottom";
            // 
            // bbiAlignToGrid
            // 
            bbiAlignToGrid.Caption = "to &Grid";
            bbiAlignToGrid.Command = ReportCommand.AlignToGrid;
            bbiAlignToGrid.Enabled = false;
            bbiAlignToGrid.Hint = "Align the positions of the selected controls to the grid";
            bbiAlignToGrid.Id = 11;
            bbiAlignToGrid.Name = "bbiAlignToGrid";
            // 
            // msiSameSize
            // 
            msiSameSize.Caption = "&Make Same Size";
            msiSameSize.Id = 55;
            msiSameSize.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiSizeToControlWidth, true), new LinkPersistInfo(bbiSizeToGrid), new LinkPersistInfo(bbiSizeToControlHeight), new LinkPersistInfo(bbiSizeToControl) });
            msiSameSize.Name = "msiSameSize";
            // 
            // bbiSizeToControlWidth
            // 
            bbiSizeToControlWidth.Caption = "&Width";
            bbiSizeToControlWidth.Command = ReportCommand.SizeToControlWidth;
            bbiSizeToControlWidth.Enabled = false;
            bbiSizeToControlWidth.Hint = "Make the selected controls have the same width";
            bbiSizeToControlWidth.Id = 18;
            bbiSizeToControlWidth.Name = "bbiSizeToControlWidth";
            // 
            // bbiSizeToGrid
            // 
            bbiSizeToGrid.Caption = "Size to Gri&d";
            bbiSizeToGrid.Command = ReportCommand.SizeToGrid;
            bbiSizeToGrid.Enabled = false;
            bbiSizeToGrid.Hint = "Size the selected controls to the grid";
            bbiSizeToGrid.Id = 19;
            bbiSizeToGrid.Name = "bbiSizeToGrid";
            // 
            // bbiSizeToControlHeight
            // 
            bbiSizeToControlHeight.Caption = "&Height";
            bbiSizeToControlHeight.Command = ReportCommand.SizeToControlHeight;
            bbiSizeToControlHeight.Enabled = false;
            bbiSizeToControlHeight.Hint = "Make the selected controls have the same height";
            bbiSizeToControlHeight.Id = 20;
            bbiSizeToControlHeight.Name = "bbiSizeToControlHeight";
            // 
            // bbiSizeToControl
            // 
            bbiSizeToControl.Caption = "&Both";
            bbiSizeToControl.Command = ReportCommand.SizeToControl;
            bbiSizeToControl.Enabled = false;
            bbiSizeToControl.Hint = "Make the selected controls the same size";
            bbiSizeToControl.Id = 21;
            bbiSizeToControl.Name = "bbiSizeToControl";
            // 
            // msiHorizontalSpacing
            // 
            msiHorizontalSpacing.Caption = "&Horizontal Spacing";
            msiHorizontalSpacing.Id = 56;
            msiHorizontalSpacing.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiHorizSpaceMakeEqual, true), new LinkPersistInfo(bbiHorizSpaceIncrease), new LinkPersistInfo(bbiHorizSpaceDecrease), new LinkPersistInfo(bbiHorizSpaceConcatenate) });
            msiHorizontalSpacing.Name = "msiHorizontalSpacing";
            // 
            // bbiHorizSpaceMakeEqual
            // 
            bbiHorizSpaceMakeEqual.Caption = "Make &Equal";
            bbiHorizSpaceMakeEqual.Command = ReportCommand.HorizSpaceMakeEqual;
            bbiHorizSpaceMakeEqual.Enabled = false;
            bbiHorizSpaceMakeEqual.Hint = "Make the spacing between the selected controls equal";
            bbiHorizSpaceMakeEqual.Id = 22;
            bbiHorizSpaceMakeEqual.Name = "bbiHorizSpaceMakeEqual";
            // 
            // bbiHorizSpaceIncrease
            // 
            bbiHorizSpaceIncrease.Caption = "&Increase";
            bbiHorizSpaceIncrease.Command = ReportCommand.HorizSpaceIncrease;
            bbiHorizSpaceIncrease.Enabled = false;
            bbiHorizSpaceIncrease.Hint = "Increase the spacing between the selected controls";
            bbiHorizSpaceIncrease.Id = 23;
            bbiHorizSpaceIncrease.Name = "bbiHorizSpaceIncrease";
            // 
            // bbiHorizSpaceDecrease
            // 
            bbiHorizSpaceDecrease.Caption = "&Decrease";
            bbiHorizSpaceDecrease.Command = ReportCommand.HorizSpaceDecrease;
            bbiHorizSpaceDecrease.Enabled = false;
            bbiHorizSpaceDecrease.Hint = "Decrease the spacing between the selected controls";
            bbiHorizSpaceDecrease.Id = 24;
            bbiHorizSpaceDecrease.Name = "bbiHorizSpaceDecrease";
            // 
            // bbiHorizSpaceConcatenate
            // 
            bbiHorizSpaceConcatenate.Caption = "&Remove";
            bbiHorizSpaceConcatenate.Command = ReportCommand.HorizSpaceConcatenate;
            bbiHorizSpaceConcatenate.Enabled = false;
            bbiHorizSpaceConcatenate.Hint = "Remove the spacing between the selected controls";
            bbiHorizSpaceConcatenate.Id = 25;
            bbiHorizSpaceConcatenate.Name = "bbiHorizSpaceConcatenate";
            // 
            // msiVerticalSpacing
            // 
            msiVerticalSpacing.Caption = "&Vertical Spacing";
            msiVerticalSpacing.Id = 57;
            msiVerticalSpacing.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiVertSpaceMakeEqual, true), new LinkPersistInfo(bbiVertSpaceIncrease), new LinkPersistInfo(bbiVertSpaceDecrease), new LinkPersistInfo(bbiVertSpaceConcatenate) });
            msiVerticalSpacing.Name = "msiVerticalSpacing";
            // 
            // bbiVertSpaceMakeEqual
            // 
            bbiVertSpaceMakeEqual.Caption = "Make &Equal";
            bbiVertSpaceMakeEqual.Command = ReportCommand.VertSpaceMakeEqual;
            bbiVertSpaceMakeEqual.Enabled = false;
            bbiVertSpaceMakeEqual.Hint = "Make the spacing between the selected controls equal";
            bbiVertSpaceMakeEqual.Id = 26;
            bbiVertSpaceMakeEqual.Name = "bbiVertSpaceMakeEqual";
            // 
            // bbiVertSpaceIncrease
            // 
            bbiVertSpaceIncrease.Caption = "&Increase";
            bbiVertSpaceIncrease.Command = ReportCommand.VertSpaceIncrease;
            bbiVertSpaceIncrease.Enabled = false;
            bbiVertSpaceIncrease.Hint = "Increase the spacing between the selected controls";
            bbiVertSpaceIncrease.Id = 27;
            bbiVertSpaceIncrease.Name = "bbiVertSpaceIncrease";
            // 
            // bbiVertSpaceDecrease
            // 
            bbiVertSpaceDecrease.Caption = "&Decrease";
            bbiVertSpaceDecrease.Command = ReportCommand.VertSpaceDecrease;
            bbiVertSpaceDecrease.Enabled = false;
            bbiVertSpaceDecrease.Hint = "Decrease the spacing between the selected controls";
            bbiVertSpaceDecrease.Id = 28;
            bbiVertSpaceDecrease.Name = "bbiVertSpaceDecrease";
            // 
            // bbiVertSpaceConcatenate
            // 
            bbiVertSpaceConcatenate.Caption = "&Remove";
            bbiVertSpaceConcatenate.Command = ReportCommand.VertSpaceConcatenate;
            bbiVertSpaceConcatenate.Enabled = false;
            bbiVertSpaceConcatenate.Hint = "Remove the spacing between the selected controls";
            bbiVertSpaceConcatenate.Id = 29;
            bbiVertSpaceConcatenate.Name = "bbiVertSpaceConcatenate";
            // 
            // bsiCenter
            // 
            bsiCenter.Caption = "&Center in Form";
            bsiCenter.Id = 58;
            bsiCenter.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiCenterHorizontally, true), new LinkPersistInfo(bbiCenterVertically) });
            bsiCenter.Name = "bsiCenter";
            // 
            // bbiCenterHorizontally
            // 
            bbiCenterHorizontally.Caption = "&Horizontally";
            bbiCenterHorizontally.Command = ReportCommand.CenterHorizontally;
            bbiCenterHorizontally.Enabled = false;
            bbiCenterHorizontally.Hint = "Horizontally center the selected controls within a band";
            bbiCenterHorizontally.Id = 30;
            bbiCenterHorizontally.Name = "bbiCenterHorizontally";
            // 
            // bbiCenterVertically
            // 
            bbiCenterVertically.Caption = "&Vertically";
            bbiCenterVertically.Command = ReportCommand.CenterVertically;
            bbiCenterVertically.Enabled = false;
            bbiCenterVertically.Hint = "Vertically center the selected controls within a band";
            bbiCenterVertically.Id = 31;
            bbiCenterVertically.Name = "bbiCenterVertically";
            // 
            // msiOrder
            // 
            msiOrder.Caption = "&Order";
            msiOrder.Id = 59;
            msiOrder.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiBringToFront, true), new LinkPersistInfo(bbiSendToBack) });
            msiOrder.Name = "msiOrder";
            // 
            // bbiBringToFront
            // 
            bbiBringToFront.Caption = "&Bring to Front";
            bbiBringToFront.Command = ReportCommand.BringToFront;
            bbiBringToFront.Enabled = false;
            bbiBringToFront.Hint = "Bring the selected controls to the front";
            bbiBringToFront.Id = 32;
            bbiBringToFront.Name = "bbiBringToFront";
            // 
            // bbiSendToBack
            // 
            bbiSendToBack.Caption = "&Send to Back";
            bbiSendToBack.Command = ReportCommand.SendToBack;
            bbiSendToBack.Enabled = false;
            bbiSendToBack.Hint = "Move the selected controls to the back";
            bbiSendToBack.Id = 33;
            bbiSendToBack.Name = "bbiSendToBack";
            // 
            // msiWindow
            // 
            msiWindow.Caption = "&Window";
            msiWindow.Id = 66;
            msiWindow.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(msiWindowInterface, true), new LinkPersistInfo(CommandBarItem8), new LinkPersistInfo(CommandBarItem9), new LinkPersistInfo(CommandBarItem10), new LinkPersistInfo(msiWindows, true) });
            msiWindow.Name = "msiWindow";
            // 
            // msiWindowInterface
            // 
            msiWindowInterface.BindableChecked = true;
            msiWindowInterface.Caption = "&Tabbed Interface";
            msiWindowInterface.Checked = true;
            msiWindowInterface.CheckedCommand = ReportCommand.ShowTabbedInterface;
            msiWindowInterface.Enabled = false;
            msiWindowInterface.Hint = "Switch between tabbed and window MDI layout modes";
            msiWindowInterface.Id = 67;
            msiWindowInterface.Name = "msiWindowInterface";
            msiWindowInterface.UncheckedCommand = ReportCommand.ShowWindowInterface;
            // 
            // CommandBarItem8
            // 
            CommandBarItem8.Caption = "&Cascade";
            CommandBarItem8.Command = ReportCommand.MdiCascade;
            CommandBarItem8.Enabled = false;
            CommandBarItem8.Hint = "Arrange all open documents cascaded, so that they overlap each other";
            CommandBarItem8.Id = 68;
            CommandBarItem8.Name = "CommandBarItem8";
            // 
            // CommandBarItem9
            // 
            CommandBarItem9.Caption = "Tile &Horizontal";
            CommandBarItem9.Command = ReportCommand.MdiTileHorizontal;
            CommandBarItem9.Enabled = false;
            CommandBarItem9.Hint = "Arrange all open documents from top to bottom";
            CommandBarItem9.Id = 69;
            CommandBarItem9.Name = "CommandBarItem9";
            // 
            // CommandBarItem10
            // 
            CommandBarItem10.Caption = "Tile &Vertical";
            CommandBarItem10.Command = ReportCommand.MdiTileVertical;
            CommandBarItem10.Enabled = false;
            CommandBarItem10.Hint = "Arrange all open documents from left to right";
            CommandBarItem10.Id = 70;
            CommandBarItem10.Name = "CommandBarItem10";
            // 
            // msiWindows
            // 
            msiWindows.Caption = "Windows";
            msiWindows.Id = 71;
            msiWindows.Name = "msiWindows";
            // 
            // DesignBar2
            // 
            DesignBar2.BarName = "Toolbar";
            DesignBar2.DockCol = 0;
            DesignBar2.DockRow = 1;
            DesignBar2.DockStyle = BarDockStyle.Top;
            DesignBar2.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(CommandBarItem1), new LinkPersistInfo(bbiOpenFile), new LinkPersistInfo(bbiSaveFile), new LinkPersistInfo(CommandBarItem7), new LinkPersistInfo(bbiCut, true), new LinkPersistInfo(bbiCopy), new LinkPersistInfo(bbiPaste), new LinkPersistInfo(bbiUndo, true), new LinkPersistInfo(bbiRedo) });
            DesignBar2.Text = "Toolbar";
            // 
            // DesignBar3
            // 
            DesignBar3.BarName = "Formatting Toolbar";
            DesignBar3.DockCol = 1;
            DesignBar3.DockRow = 1;
            DesignBar3.DockStyle = BarDockStyle.Top;
            DesignBar3.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(beiFontName), new LinkPersistInfo(beiFontSize), new LinkPersistInfo(bbiFontBold), new LinkPersistInfo(bbiFontItalic), new LinkPersistInfo(bbiFontUnderline), new LinkPersistInfo(bbiForeColor, true), new LinkPersistInfo(bbiBackColor), new LinkPersistInfo(bbiJustifyLeft, true), new LinkPersistInfo(bbiJustifyCenter), new LinkPersistInfo(bbiJustifyRight), new LinkPersistInfo(bbiJustifyJustify) });
            DesignBar3.Text = "Formatting Toolbar";
            // 
            // beiFontName
            // 
            beiFontName.Caption = "Font Name";
            beiFontName.Edit = RecentlyUsedItemsComboBox1;
            beiFontName.EditWidth = 120;
            beiFontName.Hint = "Font Name";
            beiFontName.Id = 0;
            beiFontName.Name = "beiFontName";
            // 
            // RecentlyUsedItemsComboBox1
            // 
            RecentlyUsedItemsComboBox1.AppearanceDropDown.Font = new Font("Tahoma", 11.25F);
            RecentlyUsedItemsComboBox1.AppearanceDropDown.Options.UseFont = true;
            RecentlyUsedItemsComboBox1.AutoHeight = false;
            RecentlyUsedItemsComboBox1.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            RecentlyUsedItemsComboBox1.Name = "RecentlyUsedItemsComboBox1";
            // 
            // beiFontSize
            // 
            beiFontSize.Caption = "Font Size";
            beiFontSize.Edit = DesignRepositoryItemComboBox1;
            beiFontSize.EditWidth = 55;
            beiFontSize.Hint = "Font Size";
            beiFontSize.Id = 1;
            beiFontSize.Name = "beiFontSize";
            // 
            // DesignRepositoryItemComboBox1
            // 
            DesignRepositoryItemComboBox1.AutoHeight = false;
            DesignRepositoryItemComboBox1.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            DesignRepositoryItemComboBox1.Name = "DesignRepositoryItemComboBox1";
            // 
            // DesignBar4
            // 
            DesignBar4.BarName = "Layout Toolbar";
            DesignBar4.DockCol = 0;
            DesignBar4.DockRow = 2;
            DesignBar4.DockStyle = BarDockStyle.Top;
            DesignBar4.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiAlignToGrid), new LinkPersistInfo(bbiAlignLeft, true), new LinkPersistInfo(bbiAlignVerticalCenters), new LinkPersistInfo(bbiAlignRight), new LinkPersistInfo(bbiAlignTop, true), new LinkPersistInfo(bbiAlignHorizontalCenters), new LinkPersistInfo(bbiAlignBottom), new LinkPersistInfo(bbiSizeToControlWidth, true), new LinkPersistInfo(bbiSizeToGrid), new LinkPersistInfo(bbiSizeToControlHeight), new LinkPersistInfo(bbiSizeToControl), new LinkPersistInfo(bbiHorizSpaceMakeEqual, true), new LinkPersistInfo(bbiHorizSpaceIncrease), new LinkPersistInfo(bbiHorizSpaceDecrease), new LinkPersistInfo(bbiHorizSpaceConcatenate), new LinkPersistInfo(bbiVertSpaceMakeEqual, true), new LinkPersistInfo(bbiVertSpaceIncrease), new LinkPersistInfo(bbiVertSpaceDecrease), new LinkPersistInfo(bbiVertSpaceConcatenate), new LinkPersistInfo(bbiCenterHorizontally, true), new LinkPersistInfo(bbiCenterVertically), new LinkPersistInfo(bbiBringToFront, true), new LinkPersistInfo(bbiSendToBack) });
            DesignBar4.Text = "Layout Toolbar";
            // 
            // DesignBar5
            // 
            DesignBar5.BarName = "Status Bar";
            DesignBar5.CanDockStyle = BarCanDockStyle.Bottom;
            DesignBar5.DockCol = 0;
            DesignBar5.DockRow = 0;
            DesignBar5.DockStyle = BarDockStyle.Bottom;
            DesignBar5.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bsiHint) });
            DesignBar5.OptionsBar.AllowQuickCustomization = false;
            DesignBar5.OptionsBar.DrawDragBorder = false;
            DesignBar5.OptionsBar.UseWholeRow = true;
            DesignBar5.Text = "Status Bar";
            // 
            // bsiHint
            // 
            bsiHint.AutoSize = BarStaticItemSize.Spring;
            bsiHint.Border = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            bsiHint.Id = 42;
            bsiHint.Name = "bsiHint";
            // 
            // Bar1
            // 
            Bar1.BarName = "Zoom Toolbar";
            Bar1.DockCol = 1;
            Bar1.DockRow = 2;
            Bar1.DockStyle = BarDockStyle.Top;
            Bar1.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(bbiZoomOut), new LinkPersistInfo(bbiZoom), new LinkPersistInfo(bbiZoomIn) });
            Bar1.Text = "Zoom Toolbar";
            // 
            // bbiZoomOut
            // 
            bbiZoomOut.Caption = "Zoom Out";
            bbiZoomOut.Command = ReportCommand.ZoomOut;
            bbiZoomOut.Enabled = false;
            bbiZoomOut.Hint = "Zoom out the design surface";
            bbiZoomOut.Id = 73;
            bbiZoomOut.ItemShortcut = new BarShortcut(Keys.Control | Keys.Subtract);
            bbiZoomOut.Name = "bbiZoomOut";
            // 
            // bbiZoom
            // 
            bbiZoom.Caption = "Zoom";
            bbiZoom.Edit = DesignRepositoryItemComboBox2;
            bbiZoom.EditWidth = 70;
            bbiZoom.Enabled = false;
            bbiZoom.Hint = "Select or input the zoom factor";
            bbiZoom.Id = 74;
            bbiZoom.Name = "bbiZoom";
            // 
            // DesignRepositoryItemComboBox2
            // 
            DesignRepositoryItemComboBox2.AutoComplete = false;
            DesignRepositoryItemComboBox2.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            DesignRepositoryItemComboBox2.Name = "DesignRepositoryItemComboBox2";
            // 
            // bbiZoomIn
            // 
            bbiZoomIn.Caption = "Zoom In";
            bbiZoomIn.Command = ReportCommand.ZoomIn;
            bbiZoomIn.Enabled = false;
            bbiZoomIn.Hint = "Zoom in the design surface";
            bbiZoomIn.Id = 75;
            bbiZoomIn.ItemShortcut = new BarShortcut(Keys.Control | Keys.Add);
            bbiZoomIn.Name = "bbiZoomIn";
            // 
            // barDockControlTop
            // 
            barDockControlTop.CausesValidation = false;
            barDockControlTop.Dock = DockStyle.Top;
            barDockControlTop.Location = new Point(0, 0);
            barDockControlTop.Manager = XrDesignBarManager1;
            barDockControlTop.Margin = new Padding(4, 3, 4, 3);
            barDockControlTop.Size = new Size(1370, 71);
            // 
            // barDockControlBottom
            // 
            barDockControlBottom.CausesValidation = false;
            barDockControlBottom.Dock = DockStyle.Bottom;
            barDockControlBottom.Location = new Point(0, 727);
            barDockControlBottom.Manager = XrDesignBarManager1;
            barDockControlBottom.Margin = new Padding(4, 3, 4, 3);
            barDockControlBottom.Size = new Size(1370, 22);
            // 
            // barDockControlLeft
            // 
            barDockControlLeft.CausesValidation = false;
            barDockControlLeft.Dock = DockStyle.Left;
            barDockControlLeft.Location = new Point(0, 71);
            barDockControlLeft.Manager = XrDesignBarManager1;
            barDockControlLeft.Margin = new Padding(4, 3, 4, 3);
            barDockControlLeft.Size = new Size(21, 656);
            // 
            // barDockControlRight
            // 
            barDockControlRight.CausesValidation = false;
            barDockControlRight.Dock = DockStyle.Right;
            barDockControlRight.Location = new Point(1370, 71);
            barDockControlRight.Manager = XrDesignBarManager1;
            barDockControlRight.Margin = new Padding(4, 3, 4, 3);
            barDockControlRight.Size = new Size(0, 656);
            // 
            // XrDesignDockManager1
            // 
            XrDesignDockManager1.Form = this;
            XrDesignDockManager1.ImageStream = (DevExpress.Utils.ImageCollectionStreamer)resources.GetObject("XrDesignDockManager1.ImageStream");
            XrDesignDockManager1.MenuManager = XrDesignBarManager1;
            XrDesignDockManager1.RootPanels.AddRange(new DockPanel[] { panelContainer1, panelContainer4 });
            XrDesignDockManager1.TopZIndexControls.AddRange(new string[] { "DevExpress.XtraBars.BarDockControl", "DevExpress.XtraBars.StandaloneBarDockControl", "System.Windows.Forms.StatusBar", "System.Windows.Forms.MenuStrip", "System.Windows.Forms.StatusStrip", "DevExpress.XtraBars.Ribbon.RibbonStatusBar", "DevExpress.XtraBars.Ribbon.RibbonControl", "DevExpress.XtraBars.Navigation.OfficeNavigationBar", "DevExpress.XtraBars.Navigation.TileNavPane", "DevExpress.XtraBars.TabFormControl", "DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormControl", "DevExpress.XtraReports.UserDesigner.XRToolBoxPanel" });
            // 
            // panelContainer1
            // 
            panelContainer1.Controls.Add(panelContainer2);
            panelContainer1.Controls.Add(panelContainer3);
            panelContainer1.Dock = DockingStyle.Right;
            panelContainer1.ID = new Guid("1301df89-5888-4346-bfc6-a64bd2e360fe");
            panelContainer1.Location = new Point(995, 71);
            panelContainer1.Margin = new Padding(4, 3, 4, 3);
            panelContainer1.Name = "panelContainer1";
            panelContainer1.OriginalSize = new Size(375, 200);
            panelContainer1.Size = new Size(375, 656);
            panelContainer1.Text = "panelContainer1";
            // 
            // panelContainer2
            // 
            panelContainer2.ActiveChild = ReportExplorerDockPanel1;
            panelContainer2.Controls.Add(ReportExplorerDockPanel1);
            panelContainer2.Controls.Add(FieldListDockPanel1);
            panelContainer2.Dock = DockingStyle.Fill;
            panelContainer2.ID = new Guid("3e04ceb2-4606-46d1-91a7-455a2ece56a3");
            panelContainer2.Location = new Point(0, 0);
            panelContainer2.Margin = new Padding(4, 3, 4, 3);
            panelContainer2.Name = "panelContainer2";
            panelContainer2.OriginalSize = new Size(375, 298);
            panelContainer2.Size = new Size(375, 329);
            panelContainer2.Tabbed = true;
            panelContainer2.Text = "panelContainer2";
            // 
            // ReportExplorerDockPanel1
            // 
            ReportExplorerDockPanel1.Controls.Add(ReportExplorerDockPanel1_Container);
            ReportExplorerDockPanel1.Dock = DockingStyle.Fill;
            ReportExplorerDockPanel1.ID = new Guid("fb3ec6cc-3b9b-4b9c-91cf-cff78c1edbf1");
            ReportExplorerDockPanel1.Location = new Point(1, 23);
            ReportExplorerDockPanel1.Margin = new Padding(4, 3, 4, 3);
            ReportExplorerDockPanel1.Name = "ReportExplorerDockPanel1";
            ReportExplorerDockPanel1.OriginalSize = new Size(366, 242);
            ReportExplorerDockPanel1.Size = new Size(374, 279);
            ReportExplorerDockPanel1.Text = "Report Explorer";
            // 
            // ReportExplorerDockPanel1_Container
            // 
            ReportExplorerDockPanel1_Container.Location = new Point(0, 0);
            ReportExplorerDockPanel1_Container.Margin = new Padding(4, 3, 4, 3);
            ReportExplorerDockPanel1_Container.Name = "ReportExplorerDockPanel1_Container";
            ReportExplorerDockPanel1_Container.Size = new Size(374, 279);
            ReportExplorerDockPanel1_Container.TabIndex = 0;
            // 
            // FieldListDockPanel1
            // 
            FieldListDockPanel1.Controls.Add(FieldListDockPanel1_Container);
            FieldListDockPanel1.Dock = DockingStyle.Fill;
            FieldListDockPanel1.ID = new Guid("faf69838-a93f-4114-83e8-d0d09cc5ce95");
            FieldListDockPanel1.Location = new Point(1, 23);
            FieldListDockPanel1.Margin = new Padding(4, 3, 4, 3);
            FieldListDockPanel1.Name = "FieldListDockPanel1";
            FieldListDockPanel1.OriginalSize = new Size(366, 242);
            FieldListDockPanel1.Size = new Size(374, 279);
            FieldListDockPanel1.Text = "Field List";
            // 
            // FieldListDockPanel1_Container
            // 
            FieldListDockPanel1_Container.Location = new Point(0, 0);
            FieldListDockPanel1_Container.Margin = new Padding(4, 3, 4, 3);
            FieldListDockPanel1_Container.Name = "FieldListDockPanel1_Container";
            FieldListDockPanel1_Container.Size = new Size(374, 279);
            FieldListDockPanel1_Container.TabIndex = 0;
            // 
            // panelContainer3
            // 
            panelContainer3.ActiveChild = PropertyGridDockPanel1;
            panelContainer3.Controls.Add(PropertyGridDockPanel1);
            panelContainer3.Controls.Add(ReportGalleryDockPanel1);
            panelContainer3.Dock = DockingStyle.Fill;
            panelContainer3.ID = new Guid("b852cfae-9bac-4f1b-991c-998df767bbc8");
            panelContainer3.Location = new Point(0, 329);
            panelContainer3.Margin = new Padding(4, 3, 4, 3);
            panelContainer3.Name = "panelContainer3";
            panelContainer3.OriginalSize = new Size(375, 297);
            panelContainer3.Size = new Size(375, 327);
            panelContainer3.Tabbed = true;
            panelContainer3.Text = "panelContainer3";
            // 
            // PropertyGridDockPanel1
            // 
            PropertyGridDockPanel1.Controls.Add(PropertyGridDockPanel1_Container);
            PropertyGridDockPanel1.Dock = DockingStyle.Fill;
            PropertyGridDockPanel1.ID = new Guid("b38d12c3-cd06-4dec-b93d-63a0088e495a");
            PropertyGridDockPanel1.Location = new Point(1, 24);
            PropertyGridDockPanel1.Margin = new Padding(4, 3, 4, 3);
            PropertyGridDockPanel1.Name = "PropertyGridDockPanel1";
            PropertyGridDockPanel1.OriginalSize = new Size(366, 242);
            PropertyGridDockPanel1.Size = new Size(374, 276);
            PropertyGridDockPanel1.Text = "Properties";
            // 
            // PropertyGridDockPanel1_Container
            // 
            PropertyGridDockPanel1_Container.Location = new Point(0, 0);
            PropertyGridDockPanel1_Container.Margin = new Padding(4, 3, 4, 3);
            PropertyGridDockPanel1_Container.Name = "PropertyGridDockPanel1_Container";
            PropertyGridDockPanel1_Container.Size = new Size(374, 276);
            PropertyGridDockPanel1_Container.TabIndex = 0;
            // 
            // ReportGalleryDockPanel1
            // 
            ReportGalleryDockPanel1.Controls.Add(ReportGalleryDockPanel1_Container);
            ReportGalleryDockPanel1.Dock = DockingStyle.Fill;
            ReportGalleryDockPanel1.ID = new Guid("7cd5b1e8-63bb-46f7-af65-af61eb851a38");
            ReportGalleryDockPanel1.Location = new Point(1, 24);
            ReportGalleryDockPanel1.Margin = new Padding(4, 3, 4, 3);
            ReportGalleryDockPanel1.Name = "ReportGalleryDockPanel1";
            ReportGalleryDockPanel1.OriginalSize = new Size(366, 242);
            ReportGalleryDockPanel1.Size = new Size(374, 276);
            ReportGalleryDockPanel1.Text = "Report Gallery";
            // 
            // ReportGalleryDockPanel1_Container
            // 
            ReportGalleryDockPanel1_Container.Location = new Point(0, 0);
            ReportGalleryDockPanel1_Container.Margin = new Padding(4, 3, 4, 3);
            ReportGalleryDockPanel1_Container.Name = "ReportGalleryDockPanel1_Container";
            ReportGalleryDockPanel1_Container.Size = new Size(374, 276);
            ReportGalleryDockPanel1_Container.TabIndex = 0;
            // 
            // panelContainer4
            // 
            panelContainer4.ActiveChild = GroupAndSortDockPanel1;
            panelContainer4.Controls.Add(GroupAndSortDockPanel1);
            panelContainer4.Controls.Add(ErrorListDockPanel1);
            panelContainer4.Dock = DockingStyle.Bottom;
            panelContainer4.ID = new Guid("a27f419a-3c89-4764-969a-c95f4762e24a");
            panelContainer4.Location = new Point(21, 527);
            panelContainer4.Margin = new Padding(4, 3, 4, 3);
            panelContainer4.Name = "panelContainer4";
            panelContainer4.OriginalSize = new Size(200, 200);
            panelContainer4.Size = new Size(974, 200);
            panelContainer4.Tabbed = true;
            panelContainer4.Text = "panelContainer4";
            // 
            // GroupAndSortDockPanel1
            // 
            GroupAndSortDockPanel1.Controls.Add(GroupAndSortDockPanel1_Container);
            GroupAndSortDockPanel1.Dock = DockingStyle.Fill;
            GroupAndSortDockPanel1.ID = new Guid("4bab159e-c495-4d67-87dc-f4e895da443e");
            GroupAndSortDockPanel1.Location = new Point(0, 24);
            GroupAndSortDockPanel1.Margin = new Padding(4, 3, 4, 3);
            GroupAndSortDockPanel1.Name = "GroupAndSortDockPanel1";
            GroupAndSortDockPanel1.OriginalSize = new Size(804, 144);
            GroupAndSortDockPanel1.Size = new Size(974, 149);
            GroupAndSortDockPanel1.Text = "Group and Sort";
            // 
            // GroupAndSortDockPanel1_Container
            // 
            GroupAndSortDockPanel1_Container.Location = new Point(0, 0);
            GroupAndSortDockPanel1_Container.Margin = new Padding(4, 3, 4, 3);
            GroupAndSortDockPanel1_Container.Name = "GroupAndSortDockPanel1_Container";
            GroupAndSortDockPanel1_Container.Size = new Size(974, 149);
            GroupAndSortDockPanel1_Container.TabIndex = 0;
            // 
            // ErrorListDockPanel1
            // 
            ErrorListDockPanel1.Controls.Add(ErrorListDockPanel1_Container);
            ErrorListDockPanel1.Dock = DockingStyle.Fill;
            ErrorListDockPanel1.ID = new Guid("5a9a01fd-6e95-4e81-a8c4-ac63153d7488");
            ErrorListDockPanel1.Location = new Point(0, 24);
            ErrorListDockPanel1.Margin = new Padding(4, 3, 4, 3);
            ErrorListDockPanel1.Name = "ErrorListDockPanel1";
            ErrorListDockPanel1.OriginalSize = new Size(804, 144);
            ErrorListDockPanel1.Size = new Size(974, 149);
            ErrorListDockPanel1.Text = "Report Design Analyzer";
            // 
            // ErrorListDockPanel1_Container
            // 
            ErrorListDockPanel1_Container.Location = new Point(0, 0);
            ErrorListDockPanel1_Container.Margin = new Padding(4, 3, 4, 3);
            ErrorListDockPanel1_Container.Name = "ErrorListDockPanel1_Container";
            ErrorListDockPanel1_Container.Size = new Size(974, 149);
            ErrorListDockPanel1_Container.TabIndex = 0;
            // 
            // ReportDesigner1
            // 
            ReportDesigner1.ContainerControl = null;
            xrDesignPanelListener1.DesignControl = XrDesignBarManager1;
            xrDesignPanelListener2.DesignControl = XrDesignDockManager1;
            xrDesignPanelListener3.DesignControl = FieldListDockPanel1;
            xrDesignPanelListener4.DesignControl = PropertyGridDockPanel1;
            xrDesignPanelListener5.DesignControl = ReportExplorerDockPanel1;
            xrDesignPanelListener6.DesignControl = ReportGalleryDockPanel1;
            xrDesignPanelListener7.DesignControl = GroupAndSortDockPanel1;
            xrDesignPanelListener8.DesignControl = ErrorListDockPanel1;
            ReportDesigner1.DesignPanelListeners.AddRange(new XRDesignPanelListener[] { xrDesignPanelListener1, xrDesignPanelListener2, xrDesignPanelListener3, xrDesignPanelListener4, xrDesignPanelListener5, xrDesignPanelListener6, xrDesignPanelListener7, xrDesignPanelListener8 });
            ReportDesigner1.Form = this;
            // 
            // frm_ReportDesign
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1370, 749);
            Controls.Add(panelContainer1);
            Controls.Add(panelContainer4);
            Controls.Add(barDockControlLeft);
            Controls.Add(barDockControlRight);
            Controls.Add(barDockControlBottom);
            Controls.Add(barDockControlTop);
            Margin = new Padding(4, 3, 4, 3);
            Name = "frm_ReportDesign";
            Text = "frm_ReportDesign";
            WindowState = FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)XrDesignBarManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)RecentlyUsedItemsComboBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)DesignRepositoryItemComboBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)DesignRepositoryItemComboBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)XrDesignDockManager1).EndInit();
            panelContainer1.ResumeLayout(false);
            panelContainer2.ResumeLayout(false);
            ReportExplorerDockPanel1.ResumeLayout(false);
            FieldListDockPanel1.ResumeLayout(false);
            panelContainer3.ResumeLayout(false);
            PropertyGridDockPanel1.ResumeLayout(false);
            ReportGalleryDockPanel1.ResumeLayout(false);
            panelContainer4.ResumeLayout(false);
            GroupAndSortDockPanel1.ResumeLayout(false);
            ErrorListDockPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)ReportDesigner1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
