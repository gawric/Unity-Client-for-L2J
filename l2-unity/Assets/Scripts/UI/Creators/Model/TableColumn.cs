

using System.Collections.Generic;
using UnityEngine;

public class TableColumn
{
    public bool _alignTextCenter;
    public string _nameColumn;
    public float _manualWidth;
    public List<string> _listData;
    public float _leftIndent;
    public int _maxTextSize;
    public bool _isImage;
    private Dictionary<string, UnityEngine.Color> _allColors;



    public TableColumn(bool alignTextCenter, string nameColumn, float width, List<string> listData, float leftIndent)
    {
        _alignTextCenter = alignTextCenter;
        _nameColumn = nameColumn;
        _manualWidth = width;
        _listData = listData;
        _leftIndent = leftIndent;
        _maxTextSize = 0;
        _isImage = false;
    }

    public TableColumn(bool alignTextCenter, string nameColumn, float width, List<string> listData, float leftIndent, int maxTextSize)
    {
        _alignTextCenter = alignTextCenter;
        _nameColumn = nameColumn;
        _manualWidth = width;
        _listData = listData;
        _leftIndent = leftIndent;
        _maxTextSize = maxTextSize;
        _isImage = false;
    }

    public TableColumn(bool alignTextCenter, string nameColumn, float width, List<string> listData, float leftIndent, bool isImage)
    {
        _alignTextCenter = alignTextCenter;
        _nameColumn = nameColumn;
        _manualWidth = width;
        _listData = listData;
        _leftIndent = leftIndent;
        _maxTextSize = 0;
        _isImage = isImage;
    }


    public void SetData(List<string> listData)
    {
        if (_listData != null)
        {
            _listData.Clear();
            _listData = null;
        }
        _listData = listData;
    }
    public bool AlignTextCenter
    {
        get { return _alignTextCenter; }
        set { _alignTextCenter = value; }
    }

    public string NameColumn
    {
        get { return _nameColumn; }
        set { _nameColumn = value; }
    }

    public bool IsImage
    {
        get { return _isImage; }
        set { _isImage = value; }
    }

    public float ManualWidth
    {
        get { return _manualWidth; }
        set { _manualWidth = value; }
    }

    public int MaxTextSize
    {
        get { return _maxTextSize; }
        set { _maxTextSize = value; }
    }

    public List<string> ListData
    {
        get { return _listData; }
        set { _listData = value; }
    }

    public float LeftIndent
    {
        get { return _leftIndent; }
        set { _leftIndent = value; }
    }

    public void SetColor(string rowName, string hex_color)
    {
        if (_allColors == null) _allColors = new Dictionary<string, UnityEngine.Color>();

        UnityEngine.Color color;
        if (ColorUtility.TryParseHtmlString(hex_color, out color))
        {
            if (_allColors.ContainsKey(rowName))
            {
                _allColors[rowName] = color;  // Update existing color
            }
            else
            {
                _allColors.Add(rowName, color);  // Add new color
            }
        }
        else
        {
            Debug.LogError($"Failed to parse color from hex string: {hex_color}");
        }
    }


    public UnityEngine.Color GetColor(string rowName)
    {
        if (_allColors != null && _allColors.ContainsKey(rowName))
        {
            return _allColors[rowName];
        }
        return UnityEngine.Color.white;
    }

    public void ResetColor(string rowName)
    {
        if (_allColors != null && _allColors.ContainsKey(rowName))
        {
            _allColors.Remove(rowName);
        }
    }
    public bool IsSetColor()
    {
        return _allColors != null && _allColors.Count > 0;
    }



}
