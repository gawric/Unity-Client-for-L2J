using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public interface ICreatorTables
{
    public void InitTable(VisualElement content);
    void LoadAsset(Func<string, VisualTreeAsset> loaderFunc);
    void CreateTable(List<TableColumn> headersName);
    void UpdateTableData(List<TableColumn> headersName);
    void RemoveByName(string rowName);
    void AddRow(string[] allList);
    void UpdateRowByName(string rowName, string hex_color , string[] allList);
    void ClearTable();

    void DestroyTable();

    bool HasTable(VisualElement element);
    public void SetSelectRow(int rowId);
    public event Action<int, string> OnRowClicked;


}
