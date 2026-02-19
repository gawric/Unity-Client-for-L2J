using System.Collections.Generic;
using UnityEngine;

public class RankedClanTableColumn : MonoBehaviour
{

    private  const string firstRang = "1st Level Privilege";
    private  const string twoRang = "2st Level Privilege";
    private  const string treeRang = "3st Level Privilege";
    private  const string forRang = "4st Level Privilege";
    private  const string fiveRang = "5st Level Privilege";

    private const string mainClanRang = "Main clan rank";
    private const string kingClanRang = "King's Guard Rank";
    private const string knightClanRang = "Rank of Knighthood";
    private const string academyClanRang = "Academy Rank";

    public static void ForEachClan(List<GradeList> gradeList , ICreatorTables _creatorTableWindows)
    {


        var rankList = new List<string>();
        var quantityList = new List<string>();

        for (int i = 0; i < 15; i++)
        {
            if (i < gradeList.Count)
            {
                rankList.Add(GetTextRank(i));
                quantityList.Add(gradeList[i].GetPower().ToString());
            }
            else
            {
                rankList.Add("");
                quantityList.Add("");
            }
        }

         List<TableColumn> listTableColumn = GetColumns(rankList, quantityList);
        _creatorTableWindows.CreateTable(listTableColumn);

    }

    private static List<TableColumn> GetColumns(List<string> rankList, List<string> quantityList)
    {
        var name = new TableColumn(false, "Rank", 140, rankList, 13);
        var lvl = new TableColumn(false, "Quantity", 10, quantityList, 0);

        return new List<TableColumn> { name, lvl };
    }

    private static string GetTextRank(int i)
    {
        switch (i)
        {
            case 0:
                return firstRang;
            case 1:
                return twoRang;
            case 2:
                return treeRang;
            case 3:
                return forRang;
            case 4:
                return fiveRang;
            case 5:
                return mainClanRang;
            case 6:
                return kingClanRang;
            case 7:
                return knightClanRang;
            case 8:
                return academyClanRang;
            default:
                return "";
        }
    }
}
