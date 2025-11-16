

using System;
using System.Collections.Generic;

using System.Linq;
using UnityEngine;

using UnityEngine.UIElements;


public class CreatorTableWindows : ICreatorTables
{
    private VisualElement _content;
    private const string _templateNameTable = "Data/UI/_Elements/Template/TableList";
    private const string _templateHeaderItem = "Data/UI/_Elements/Template/TableHeaderItem";
    private const string _templateColumnInside = "Data/UI/_Elements/Template/TableHeaderName";
    private const string _templateRowsName = "Data/UI/_Elements/Template/TableRows";
    private const string _templateRowBlackName = "Data/UI/_Elements/Template/TableRowBlack";



    private const string _tableListViewName = "table_listView";
    private const string _tableBodyListName = "table_body";
    private const string _tableHeaderListName = "table_header";
    private const string _tableHeaderLabelName = "columnLabel";
    private const string _tableHeaderBorder = "columnBorder";


    private VisualTreeAsset _templateTable;
    private VisualTreeAsset _templateHeader;
    private VisualTreeAsset _templateInside;
    private VisualTreeAsset _templateRows;

    private VisualTreeAsset _templateRowBlack;


    private int _lastSelectIndex = -1;
    private int _currentSelectIndex = -1;
    private VisualElement _lastSelectElement;

    private VisualElement _table;
    private List<VisualElement> _tableHeHeaderItem;
    private VisualElement containerHeader;

    private ListView _listView;
    private ScrollView _innerScrollView;
    private List<TableColumn> _columnsList;
    private Texture2D _defaultCursor;
    public int _index_row = 0;
    public event Action<int , string> OnRowClicked;
    private TableFunction _functions;
    public void InitTable(VisualElement content)
    {
        _content = content;
        _defaultCursor = IconManager.Instance.LoadCursorByName("Default");
        _functions = new TableFunction();
    }


    public void LoadAsset(Func<string, VisualTreeAsset> loaderFunc)
    {
        _templateTable = loaderFunc(_templateNameTable);
        _templateHeader = loaderFunc(_templateHeaderItem);
        _templateInside = loaderFunc(_templateColumnInside);
        _templateRows = loaderFunc(_templateRowsName);
        _templateRowBlack = loaderFunc(_templateRowBlackName);

    }



    public void CreateTable(List<TableColumn> columnsList)
    {
        
        if (_templateTable == null || _templateHeader == null)
        {
            Debug.LogError("CreatorTableWindows not found TemplateTable or TemplateHeader");
            return;
        }


        _lastSelectIndex = -1;
        _currentSelectIndex = -1;
        _lastSelectElement = null;

        _index_row = 0;
        _table = ToolTipsUtils.CloneOne(_templateTable);

        _listView = _table.Q<ListView>(_tableListViewName);
        _listView.fixedItemHeight = 18;
        _columnsList = columnsList;

        _innerScrollView = _listView.Q<ScrollView>();

        var arr_column0 = _columnsList[0].ListData;
        _listView.itemsSource = arr_column0;


        _listView.makeItem = MakeItem;
        _listView.bindItem = BindListViewItems;
        _listView.selectedIndicesChanged += SelectRow;

        _tableHeHeaderItem = CreateHeaderItems(columnsList);
         VisualElement containerHeaderList = _table.Q<VisualElement>(_tableHeaderListName);
        InjectHeaderList(_tableHeHeaderItem, containerHeaderList);
        IfSize0HideListView(columnsList[0].ListData.Count, _listView);
        _content.Add(_table);
    }

  

    private void IfSize0HideListView(int size , VisualElement table)
    {
        if(size == 0)
        {
            table.style.display = DisplayStyle.None;

        }
        else
        {
            table.style.display = DisplayStyle.Flex;
        }
    }

   
    private void InjectHeaderList(List<VisualElement> _tableHeHeaderItem , VisualElement containerHeaderList)
    {
        foreach (var header in _tableHeHeaderItem)
        {
            containerHeaderList.Add(header);
        }
    }

    public VisualElement MakeItem()
    {

            VisualElement tablesContainer = new VisualElement();

            tablesContainer.style.flexDirection = FlexDirection.Row;
            tablesContainer.style.flexGrow = 1;
            tablesContainer.style.flexShrink = 1;

            for (int i=0; i < _columnsList.Count; i++)
            {
                var column = _columnsList[i];
                var listRows = ToolTipsUtils.CloneOne(_templateRows);
                VisualElement element = CreateRow("" , 1);
                listRows.Add(element);
                tablesContainer.Add(listRows);

                AlignItem(listRows, column);
                listRows.name = "body_column_" + i;


                //listRows.RegisterCallback<MouseDownEvent>(evt => UnityEngine.Cursor.SetCursor(_defaultCursor, Vector2.zero, UnityEngine.CursorMode.Auto));
                //listRows.RegisterCallback<MouseUpEvent>(evt => UnityEngine.Cursor.SetCursor(_defaultCursor, Vector2.zero, UnityEngine.CursorMode.Auto));
                //listRows.RegisterCallback<PointerMoveEvent>(evt => UnityEngine.Cursor.SetCursor(_defaultCursor, Vector2.zero, UnityEngine.CursorMode.Auto));
                //listRows.RegisterCallback<PointerDownEvent>(evt => UnityEngine.Cursor.SetCursor(_defaultCursor, Vector2.zero, UnityEngine.CursorMode.Auto));

                listRows.RegisterCallback<MouseDownEvent>(ResetCursorOnMouseDown);
                listRows.RegisterCallback<MouseUpEvent>(ResetCursorOnMouseUp);
                listRows.RegisterCallback<PointerMoveEvent>(ResetCursorOnPointerMove);
                listRows.RegisterCallback<PointerDownEvent>(ResetCursorOnPointerDown);
            }

        _index_row++;

        return tablesContainer;

   }

    private void ResetCursorOnMouseDown(MouseDownEvent evt) =>
    UnityEngine.Cursor.SetCursor(_defaultCursor, Vector2.zero, UnityEngine.CursorMode.Auto);
    private void ResetCursorOnMouseUp(MouseUpEvent evt) =>
        UnityEngine.Cursor.SetCursor(_defaultCursor, Vector2.zero, UnityEngine.CursorMode.Auto);
    private void ResetCursorOnPointerMove(PointerMoveEvent evt) =>
        UnityEngine.Cursor.SetCursor(_defaultCursor, Vector2.zero, UnityEngine.CursorMode.Auto);
    private void ResetCursorOnPointerDown(PointerDownEvent evt) =>
        UnityEngine.Cursor.SetCursor(_defaultCursor, Vector2.zero, UnityEngine.CursorMode.Auto);

    private void UnregisterRowCallbacks(VisualElement listRows)
    {
        listRows.UnregisterCallback<MouseDownEvent>(ResetCursorOnMouseDown);
        listRows.UnregisterCallback<MouseUpEvent>(ResetCursorOnMouseUp);
        listRows.UnregisterCallback<PointerMoveEvent>(ResetCursorOnPointerMove);
        listRows.UnregisterCallback<PointerDownEvent>(ResetCursorOnPointerDown);
    }

    private void BindListViewItems(VisualElement ve , int i)
    {

        var row = (VisualElement)ve;

        for (int n = 0; n < _columnsList.Count; n++)
        {
            var column = _columnsList[n];
            VisualElement row_list1 = row[n];
            VisualElement in_1 = row_list1[0];
            //structur template TableRowBlack - inside 0 > content_highlight & 1 > label_text
            VisualElement content_highlight = in_1[0];
            VisualElement highlightLast = content_highlight[0];
            VisualElement highlightTile = content_highlight[1];

            Label label = (Label)in_1[1];
            VisualElement image = in_1[2];

            if (column.ListData.Count > 0)
            {
                SetTextOrImage(label, image, column, i);
                ToggleHideTextOrImage(label, image, column);
                SetColorRowsWhiteOrBlack(row_list1, i);
                ReturnSetSelectIfChangePosition(highlightLast, highlightTile, n, i);
                ResetLastSelectElementIfChangePosition(highlightLast, highlightTile, n, i);
                //i - unique index row
                //column.ListData[i] - text in cell
                SetOtherColorLabel(column, label, i+column.ListData[i]);
            }

        }

        if (_lastSelectIndex == _currentSelectIndex & i == _currentSelectIndex)
        {
            _lastSelectElement = row;
           // Debug.Log("LastIdex select update link" + _currentSelectIndex + " last index " + _lastSelectIndex);
        }

    }

    private void ToggleHideTextOrImage(Label textLabel , VisualElement image, TableColumn column)
    {

        if (column.IsImage)
        {
            textLabel.style.display = DisplayStyle.None;
            image.style.display = DisplayStyle.Flex;
        }
        else
        {
            textLabel.style.display = DisplayStyle.Flex;
            image.style.display = DisplayStyle.None;
        }
    }

    private void SetTextOrImage(Label textLabel, VisualElement image, TableColumn column , int i)
    {
        if (column.IsImage)
        {
            string img_source = column.ListData[i];

            if (string.IsNullOrEmpty(img_source))
            {
                textLabel.text = "";
                image.style.backgroundImage = null;
            }
            else
            {
                textLabel.text = "";
                Texture2D texture = IconManager.Instance.LoadTextureOtherSources(column.ListData[i]);
                image.style.backgroundImage = texture;
                image.style.width = texture.width;
                image.style.height = texture.height;
            }

        }
        else
        {
            textLabel.text = column.ListData[i];
            image.style.backgroundImage = null;
            image.style.width = 0;
            image.style.height = 0;
        }
    }

    private void SetColorRowsWhiteOrBlack(VisualElement listRow, int index)
    {
       

        if (index % 2 == 0)
        {
            // Color #393835 with 79% opacity
            listRow.style.backgroundColor = new Color(0.22f, 0.22f, 0.21f, 0.79f);
        }
        else
        {
            // Fully transparent
            listRow.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
        }
    }

    private void SetOtherColorLabel(TableColumn column, VisualElement row, string text)
    {
        if (column.IsImage)
        {
            return;
        }

        if (column.IsSetColor())
        {
            Color color = column.GetColor(text);
            row.style.color = color != Color.white ? color : Color.white;
        }
        else
        {
            if(row.style.color != Color.white)
            {
                row.style.color = Color.white;
            }

        }
    }


    private void ReturnSetSelectIfChangePosition(VisualElement highlightLast , VisualElement highlightTile , int n , int i)
    {
        if (_currentSelectIndex == i)
        {
            ShowSelectHighLightInner(highlightLast, highlightTile, _columnsList.Count - 1, n);
        }
        else
        {
            _functions.ResetSelectInner(highlightLast, highlightTile);
        }
    }

    private void ResetLastSelectElementIfChangePosition(VisualElement highlightLast, VisualElement highlightTile, int n, int i)
    {
        if (_lastSelectIndex == i && _lastSelectIndex != _currentSelectIndex)
        {
            _functions.ResetSelectInner(highlightLast, highlightTile);
        }

    }


    public List<VisualElement> CreateHeaderItems(List<TableColumn> tablesColumns)
    {
        if (_templateHeader == null)
        {
            Debug.LogError("CreatorTableWindows not found TemplateHeader");
            return new List<VisualElement>();
        }
        //Create Header Box (Only BorderBox)
        containerHeader = ToolTipsUtils.CloneOne(_templateHeader);
        var headerBorder = containerHeader.Q<VisualElement>(_tableHeaderBorder);

        headerBorder.RegisterCallback<GeometryChangedEvent>(evt => OnHeaderGeometryChanged( headerBorder , _innerScrollView));

        return tablesColumns.Select(headerName =>
        {
            //Create Items Inside Header
            var rootContainerHeader = ToolTipsUtils.CloneOne(_templateInside);
            Label tableHeaderLabel = rootContainerHeader.Q<Label>(_tableHeaderLabelName);
            VisualElement containerBgElement = rootContainerHeader.Q<VisualElement>("bgElement");

            SetManualWidth(containerBgElement, headerName.ManualWidth);
            CreateHeader(tableHeaderLabel, containerBgElement, headerName);
            headerBorder.Add(rootContainerHeader);

            return containerHeader;
        }).ToList();
    }

    public void SetManualWidth(VisualElement containerBgElement, float width)
    {
        if (width > 0)
        {
            containerBgElement.style.flexBasis = width;    
            containerBgElement.style.flexGrow = 1;       
            containerBgElement.style.flexShrink = 0;
        }
    }



    void OnHeaderGeometryChanged(VisualElement containerHeader , ScrollView innerScrollView)
    {
        //todo: вернуться и пофиксить
        //EditorApplication.delayCall += () =>
        //{
        //    bool isFirst = true;
        //    float[] allWith = new float[containerHeader.childCount];

        //    for (int i = 0; i < containerHeader.childCount; i++)
        //    {
        //        var headerItem = containerHeader[i];
        //        float headerWidth = headerItem.resolvedStyle.width;

        //        float newWidth = isFirst ? headerWidth + 2 : headerWidth;
        //        isFirst = false;
        //        allWith[i] = newWidth;
        //    }


        //    if (innerScrollView != null)
        //    {
        //        var innerContainer = innerScrollView.contentContainer;
        //        ChangeWidthListView(innerContainer.Children(), allWith);
        //    }
        //};
    }

    private void ChangeWidthListView(IEnumerable<VisualElement> elementsListView ,  float[] allWith)
    {
        foreach (var item in elementsListView)
        {
  
            var row = item;
            var columns = row.Children().ToList(); 

            for(int i = 0; i < allWith.Length; i++)
            {
                var column = columns[i];
                var new_width = allWith[i];

                column.style.flexBasis = new_width;
                column.style.flexShrink = 0;
                column.MarkDirtyRepaint();
            }
        }
    }


    public void UpdateTableData(List<TableColumn> columnsList)
    {
        if (_table == null)
        {
            Debug.LogError("Table is not created. Call CreateTable first.");
            return;
        }

        if (_templateTable == null || _templateHeader == null)
        {
            Debug.LogError("CreatorTableWindows not found TemplateTable or TemplateHeader");
            return;
        }

        _lastSelectIndex = -1;
        _currentSelectIndex = -1;
        _lastSelectElement = null;
        _columnsList.Clear();
        _columnsList = columnsList;
         var arr_column0 = _columnsList[0].ListData;
        _listView.itemsSource = arr_column0;
        _listView.Rebuild();

        _listView.selectedIndex = -1;

        var headerBorder = containerHeader.Q<VisualElement>(_tableHeaderBorder);
        if(headerBorder != null && _innerScrollView != null) OnHeaderGeometryChanged(headerBorder, _innerScrollView);
        IfSize0HideListView(columnsList[0].ListData.Count, _listView);

    }




    public void RemoveByName(string rowName)
    {
        if (!IsTableInitialized()) return;

        _functions.RemoveByName(rowName, _columnsList, _listView, ref _currentSelectIndex, OnRowClicked);
    }

    public void AddRow(string[] allList)
    {
        if (!IsTableInitialized()) return;

        _functions.AddRow(allList , _columnsList , _listView);
    }


    public void UpdateRowByName(string rowName , string hex_color , string[] allList)
    {
        if (!IsTableInitialized()) return;

        _functions.UpdateRowByName(rowName, hex_color , allList, _columnsList, _listView);
    }

    private bool IsTableInitialized()
    {
        if (_listView == null || _columnsList == null || _columnsList.Count == 0)
        {
            Debug.LogError("ListView or columns not initialized");
            return false;
        }
        return true;
    }




    public void DestroyTable()
    {
        if (_table != null)
        {

            _listView.makeItem -= MakeItem;
            _listView.bindItem -= BindListViewItems;
            _listView.selectedIndicesChanged -= SelectRow;
            //_listView.itemsSource.Clear();
            _lastSelectIndex = -1;
            _currentSelectIndex = -1;
            _lastSelectElement = null;

            _table = null;
            _tableHeHeaderItem = null;
            containerHeader = null;
            _lastSelectIndex = -1;
        }
    }

    public void ClearTable()
    {
        if (_table == null)
        {
            return;
        }

        VisualElement containerBodyList = _table.Q<VisualElement>(_tableBodyListName);
        RemoveFromHierarchy(containerBodyList);
    }

    public bool HasTable(VisualElement element)
    {
        if (element == null)
            return false;

      
        bool hasTableBody = element.Q<VisualElement>("table_body") != null;
        bool hasTableHeader = element.Q<VisualElement>("table_header") != null;

        bool hasTableClasses = element.ClassListContains("l2-table") ||
                              element.ClassListContains("table-container");

        return hasTableBody && hasTableHeader || hasTableClasses;
    }

    private void RemoveFromHierarchy(VisualElement containerBodyList)
    {
        foreach (var column in containerBodyList.Children().ToList())
        {

            foreach (var row in column.Children().ToList())
            {
                row.RemoveFromHierarchy();
            }
        }
    }


    private void CreateHeader(Label tableHeaderLabel , VisualElement bgElement , TableColumn headerName)
    {
        bgElement.RegisterCallback<MouseDownEvent>(evt => ClickMouse(evt, bgElement));
        bgElement.RegisterCallback<MouseUpEvent>(evt => UpClickMouse(evt, bgElement));
        tableHeaderLabel.text = headerName.NameColumn;
    }




    private void AlignItem(VisualElement listRows, TableColumn column)
    {
        var rows = listRows.Children().ToList();
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];

            if (column.AlignTextCenter)
            {
                row.style.alignItems = new StyleEnum<Align>(Align.Center);
                // row.style.justifyContent = new StyleEnum<Justify>(Justify.Center);
                if (column.IsImage) row.style.justifyContent = new StyleEnum<Justify>(Justify.Center);
            }
            else
            {
                var label = row.Q<Label>("labelText");
                label.style.paddingLeft = column.LeftIndent;
            }

            if(column.MaxTextSize != 0)
            {
                var label = row.Q<Label>("labelText");
                MaxLabelSize(label, column.MaxTextSize);
            }


        }
    }

    public void MaxLabelSize(Label label , int maxSize)
    {
        label.style.maxWidth = maxSize;
    }

    public VisualElement CreateRow(string text , int index)
    {
        VisualElement row;
        row = GetNewRow();
        Label labelText = row.Q<Label>("labelText");
        CleapText(labelText);

        if (labelText != null)
        {
            labelText.text = text;
        }

        return row;
    }

    public List<VisualElement> CreateRows(TableColumn column)
    {
        List < VisualElement > rows = new List<VisualElement>();

        for (int i = 0; i < column.ListData.Count; i++)
        {
            VisualElement row;
            string text = column.ListData[i];
            row = GetNewRow();
            Label labelText = row.Q<Label>("labelText");
            CleapText(labelText);

            if (labelText !=  null)
            {
                labelText.text = text;
            }

            rows.Add(row);
        }

        return rows;
    }

    private void CleapText(Label rowLabel)
    {
        //rowLabel.style.width = 200;                   // ôèêñ. øèðèíà
        rowLabel.style.whiteSpace = WhiteSpace.NoWrap; // çàïðåòèòü ïåðåíîñ
        rowLabel.style.textOverflow = TextOverflow.Clip; // îáðåçàòü
    }

    private VisualElement GetNewRow()
    {
            return  ToolTipsUtils.CloneOne(_templateRowBlack);
    }
    public void ClickMouse(MouseDownEvent evt , VisualElement element)
    {
        element.RemoveFromClassList("l2-table-header-name-bg");
        element.AddToClassList("l2-table-header-name-bg-click");
    }

    public void SelectRow(IEnumerable<int> selectedItems)
    {
        var indexRow = selectedItems.FirstOrDefault();


        var selectedVisualElement = _listView.Q<VisualElement>(className: "unity-collection-view__item--selected");

        if(ClickRow(selectedVisualElement , indexRow))
        {
            _currentSelectIndex = indexRow;
        }
    }

 

    public bool ClickRow(VisualElement _selectListColumn , int currentSelectIndex)
    {
        UnityEngine.Cursor.SetCursor(_defaultCursor, Vector2.zero, UnityEngine.CursorMode.Auto);
        if (_selectListColumn != null)
        {
            return SetSelectElement(_selectListColumn ,  currentSelectIndex);
        }
        return false;
    }

    private bool  SetSelectElement(VisualElement _selectListColumn , int  currentSelectIndex)
    {
        bool isSelect = false;
        string select_text = "";
        for (int i = 0; i < _selectListColumn.childCount; i++)
        {
            VisualElement row1 = _selectListColumn[i];

            if (row1.childCount > 0)
            {

                Label labeltext = row1.Q<Label>("labelText");
                VisualElement image = row1.Q<VisualElement>("image");

                var bg = image.style.backgroundImage.value;

                if (bg.IsEmpty())
                {
                    if (labeltext == null | string.IsNullOrEmpty(labeltext.text)) break;
                }

                select_text += "___"+labeltext.text;

                if (_lastSelectElement != null && currentSelectIndex != _lastSelectIndex)
                {
                    VisualElement last_row1 = _lastSelectElement[i];
                    VisualElement highlightLast = last_row1.Q<VisualElement>("highlight");
                    VisualElement highlightLastTile = last_row1.Q<VisualElement>("highlightTile");
                    _functions.ResetSelectInner(highlightLast, highlightLastTile);
                }



                ShowSelectHighLight(row1, _selectListColumn.childCount - 1, i);
                isSelect = true;
            }
        }


        if (_lastSelectElement != _selectListColumn & isSelect == true)
        {
            _lastSelectElement = _selectListColumn;
            _lastSelectIndex = currentSelectIndex;
        }

        EventClickOut(select_text, currentSelectIndex);

        return isSelect;
    }


    private void EventClickOut(string select_text , int currentSelectIndex)
    {
        OnRowClicked?.Invoke(currentSelectIndex, select_text);
    }


    private void ShowSelectHighLightInner(VisualElement highlight, VisualElement highlightTile, int lastChild, int i)
    {
        if(i == lastChild)
        {
            highlightTile.style.display = DisplayStyle.Flex;
        }
        else
        {
            highlight.style.display = DisplayStyle.Flex;
        }

    }

    private void ShowSelectHighLight(VisualElement row1 , int childCount , int i)
    {
       // row1.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0));
        VisualElement highlight = row1.Q<VisualElement>(i == childCount ? "highlightTile" : "highlight");
        highlight.style.display = DisplayStyle.Flex;
    }

    public void UpClickMouse(MouseUpEvent evt , VisualElement element)
    {
        element.RemoveFromClassList("l2-table-header-name-bg-click");
        element.AddToClassList("l2-table-header-name-bg");
    }

    public void SetSelectRow(int rowId)
    {
        if(rowId == -1)
        {
            //if(_selectListColumn != null)
            //{
             //   ResetSelectList(_selectListColumn, rowId);
             //   _lastElementIndex = rowId;
            //}

        }
    }
}


