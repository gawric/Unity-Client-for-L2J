

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TableFunction 
{
    public void RemoveByName(string rowName ,  List<TableColumn> columnsList , ListView listView , ref int currentSelectIndex , Action<int, string> OnRowClicked)
    {

        var listData = columnsList[0].ListData;
        int rowIndex = FindRowIndex(rowName, columnsList[0].ListData);

        if (rowIndex == -1) return;

        RemoveRowData(rowIndex , columnsList);
        AddEmptyRow(columnsList);
        RefreshTable(listView);
        UpdateSelectionAfterRemoval(rowIndex , ref currentSelectIndex);
        bool hasContent = listData.Any(str => !string.IsNullOrEmpty(str));
        ResetSelectIndexIfListData0(hasContent , listData, ref currentSelectIndex);
        ResetSelectElement(hasContent, listView);
        TriggerRowClickEvent(rowIndex , listData , OnRowClicked);
    }





    private int FindRowIndex(string rowName , List<string> listData)
    {
        var firstColumnData = listData;
        return firstColumnData.IndexOf(rowName);
    }


    private void RemoveRowData(int rowIndex , List<TableColumn> columnsList)
    {
        foreach (var column in columnsList)
        {
            column.ListData.RemoveAt(rowIndex);
        }
    }

    private void AddEmptyRow(List<TableColumn> columnsList)
    {
        foreach (var column in columnsList)
        {
            column.ListData.Add("");
        }
    }
    private void RefreshTable(ListView listView)
    {
        listView.RefreshItems();
    }

    private void UpdateSelectionAfterRemoval(int removedRowIndex , ref int currentSelectIndex)
    {
        if (currentSelectIndex > removedRowIndex)
        {
            currentSelectIndex--;
        }
    }

    private void ResetSelectIndexIfListData0(bool hasContent , List<string> listData , ref int currentSelectIndex)
    {
        if (!hasContent)
        {
            currentSelectIndex = -1;
        }
    }

    private void TriggerRowClickEvent(int rowIndex , List<string> listData , Action<int, string> OnRowClicked)
    {
        string text = listData[rowIndex];
        string selectText = "___" + text;
        OnRowClicked?.Invoke(rowIndex, selectText);
    }

    private void ResetSelectElement(bool hasContent , ListView listView)
    {
        if (!hasContent)
        {
            ResetSelectElement(listView);
        }
    }

    private void ResetSelectElement(ListView listView)
    {
        var selectedVisualElement = listView.Q<VisualElement>(className: "unity-collection-view__item--selected");

        for (int i = 0; i < selectedVisualElement.childCount; i++)
        {
            VisualElement row1 = selectedVisualElement[i];
            if (row1.childCount > 0)
            {
                VisualElement highlightLast = row1.Q<VisualElement>("highlight");
                VisualElement highlightLastTile = row1.Q<VisualElement>("highlightTile");
                ResetSelectInner(highlightLast, highlightLastTile);
            }
        }
    }

    public void ResetSelectInner(VisualElement highlightLast, VisualElement highlightLastTile)
    {
        if (highlightLast != null) highlightLast.style.display = DisplayStyle.None;
        if (highlightLastTile != null) highlightLastTile.style.display = DisplayStyle.None;
    }










    //Block Code ADD
    /// <summary>
    /// 
    /// </summary>
    /// <param name="allListData"></param>
    /// <param name="columnsList"></param>
    /// <param name="listView"></param>

    public void AddRow(string[] allListData , List<TableColumn> columnsList , ListView listView)
    {
        AddRowData(allListData, columnsList);
        RefreshTable(listView);
    }

    private void AddRowData(string[] allListData, List<TableColumn> columnsList)
    {
        if (columnsList == null || columnsList.Count == 0) return;

        int targetIndex = FindFirstEmptyIndex(columnsList[0]);
        UpdateColumnsWithData(allListData, columnsList, targetIndex);
    }

    private int FindFirstEmptyIndex(TableColumn column)
    {
        return column.ListData.FindIndex(string.IsNullOrEmpty);
    }

    private void UpdateColumnsWithData(string[] allListData, List<TableColumn> columnsList, int targetIndex)
    {
        for (int i = 0; i < columnsList.Count; i++)
        {
            var column = columnsList[i];
            var data = GetColumnData(allListData, i);
            UpdateColumnData(column, data, targetIndex);
        }
    }

    private string GetColumnData(string[] allListData, int columnIndex)
    {
        return ArrayUtils.IsValidIndexArray(allListData, columnIndex) ?
            allListData[columnIndex] : "";
    }

    private void UpdateColumnData(TableColumn column, string data, int targetIndex)
    {
        if (targetIndex != -1)
        {
            column.ListData[targetIndex] = data;
        }
        else
        {
            column.ListData.Add(data);
        }
    }

    //Block Code Update

    public void UpdateRowByName(string rowName, string hex_color , string[] newRowData, List<TableColumn> columnsList, ListView listView)
    {
        if (columnsList == null || columnsList.Count == 0 || newRowData == null)
        {
            Debug.LogError("Invalid input parameters");
            return;
        }

        int rowIndex = FindRowIndex(rowName, columnsList[0].ListData);
        if (rowIndex == -1)
        {
            Debug.LogError($"Row with name '{rowName}' not found");
            return;
        }

        UpdateRowData(rowIndex, newRowData, columnsList);
        UpdateHexColorData(rowIndex, columnsList, newRowData , hex_color);
        RefreshTable(listView);
    }

    private void UpdateRowData(int rowIndex, string[] newRowData, List<TableColumn> columnsList)
    {
        for (int i = 0; i < columnsList.Count; i++)
        {
            var column = columnsList[i];
            string data = GetColumnData(newRowData, i);

            if (rowIndex < column.ListData.Count)
            {
                column.ListData[rowIndex] = data;
            }
        }
    }

    private void UpdateHexColorData(int rowIndex , List<TableColumn> columnList, string[] newRowData , string hex_color)
    {
        if (!string.IsNullOrEmpty(hex_color))
        {
            for (int i = 0; i < columnList.Count; i++)
            {

                var column = columnList[i];
                string ids = rowIndex + newRowData[i];
                column.SetColor(ids, hex_color);
            }
        }

    }

}
