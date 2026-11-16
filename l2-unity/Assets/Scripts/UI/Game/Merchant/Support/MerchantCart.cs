using System;
using System.Collections.Generic;

public sealed class MerchantCart
{
    private readonly Func<Product, bool> _isStackable;
    private readonly HashSet<Product> _unlimited = new HashSet<Product>();
    public List<Product> Available { get; } = new List<Product>();
    public List<Product> Selected { get; } = new List<Product>();

    public MerchantCart(IEnumerable<Product> products, bool buying, Func<Product, bool> isStackable)
    {
        _isStackable = isStackable;
        foreach (Product product in products)
        {
            Product copy = product.Clone();
            Available.Add(copy);
            if (buying && copy.Count == 0)
                _unlimited.Add(copy);
        }
    }

    public int Maximum(Product product, bool toBasket)
    {
        if (!toBasket)
            return product.Count;
        int selected = Selected.Find(item => item.Equals(product))?.Count ?? 0;
        if (!_isStackable(product))
            return selected == 0 && (product.Count > 0 || _unlimited.Contains(product)) ? 1 : 0;
        return _unlimited.Contains(product) ? int.MaxValue - selected : product.Count;
    }

    public bool Move(Product product, int quantity, bool toBasket)
    {
        List<Product> from = toBasket ? Available : Selected;
        List<Product> to = toBasket ? Selected : Available;
        Product source = from.Find(item => item.Equals(product));
        if (source == null || quantity <= 0 || quantity > Maximum(source, toBasket))
            return false;
        Product destination = to.Find(item => item.Equals(source));
        bool unlimitedSource = toBasket && _unlimited.Contains(source);
        bool unlimitedDestination = !toBasket && _unlimited.Contains(source);
        if (destination != null && !unlimitedDestination && destination.Count > int.MaxValue - quantity)
            return false;
        if (destination == null)
        {
            destination = source.Clone();
            destination.SetCount(quantity);
            to.Add(destination);
        }
        else if (!unlimitedDestination)
            destination.SetCount(destination.Count + quantity);
        if (!unlimitedSource)
        {
            source.SetCount(source.Count - quantity);
            if (source.Count == 0)
                from.Remove(source);
        }
        return true;
    }

    public bool TryGetTotal(out long total)
    {
        total = 0;
        try
        {
            foreach (Product product in Selected)
            {
                if (product.Count <= 0 || product.Price < 0 || (!_isStackable(product) && product.Count != 1))
                    return false;
                total = checked(total + (long)product.Price * product.Count);
            }
        }
        catch (OverflowException) { return false; }
        return Selected.Count > 0;
    }
}
