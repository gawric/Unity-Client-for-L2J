using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopItemEvaluator
{
    private DealerWindow _dealer;
    public ShopItemEvaluator(DealerWindow dealer)
    {
        this._dealer = dealer;
    }
    public void UpdateDataForm(int adena, double weightPercent, int currentWeight, int maxWeight)
    {
        _dealer.UpdateDataWeight(currentWeight);
        _dealer.UpdateDataMaxWeight(maxWeight);
        _dealer.UpdateAdena(adena);
        _dealer.UpdateWeight(weightPercent);

        UpdateWeightBar(_dealer, currentWeight, maxWeight);
    }



    public void UpdateWeightBar(DealerWindow dealer, int currentWeight, int maxWeight)
    {
        if (dealer.GetWeiBar() != null & dealer.GetWeiBarBg() != null)
        {
            float bgWidth = 138;
            //float bgWidth = _expBarBg.resolvedStyle.width;
            double expRatio = (double)currentWeight / maxWeight;
            double barWidth = bgWidth * expRatio;
            if (maxWeight == 0)
            {
                barWidth = 0;
            }
            //_weiBar.style.width = Convert.ToSingle(barWidth);
            dealer.GetWeiBarBg().style.width = Convert.ToSingle(barWidth);
        }
    }

    public void UpdateAllPrice(List<Product> _listSell)
    {
        int allPrice = 0;

        if (_listSell != null)
        {
            for (int i = 0; i < _listSell.Count; i++)
            {
               Product product =  _listSell[i];
                if (product != null)
                {
                    if(product.Count > 1)
                    {
                        allPrice += product.Price * product.Count;
                    }
                    else
                    {
                        allPrice = allPrice + _listSell[i].Price;
                    }
                }
            }

            _dealer.UpdatePrice(allPrice);
        }
        else
        {
            _dealer.UpdatePrice(0);
        }

    }

    public void UpdateWeight(List<Product> _listSell , int _currentDataWeight , int _maxWeight)
    {
        int addWeight = 0;

        if (_listSell != null)
        {
            for (int i = 0; i < _listSell.Count; i++)
            {
                if (_listSell[i] != null)
                {
                    addWeight = addWeight + _listSell[i].GetWeight();
                }
            }

            _dealer.UpdateAllWeight(_currentDataWeight + addWeight, _maxWeight);
        }
    }

}
