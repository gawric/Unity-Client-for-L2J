using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class LocalMerchantChecks
{
    public static void Run()
    {
        foreach (bool buy in new[] { true, false })
        {
            byte[] wire = ListPacket(1, 1);
            var reader = new PacketReader(wire);
            if (buy)
            {
                var list = new BuyListDto(); list.ReadFrom(reader);
                Check(list.Products.Count == 1 && list.Products[0].ItemId == 1060 && list.Products[0].Count == 7 &&
                    list.Products[0].Price == 55 && list.CurrentMoney == 5000 && list.ListID == 3015000, "Buy wire fields");
                list.ReadFrom(new PacketReader(wire));
                Check(list.Products.Count == 1, "Reused DTO clears old products");
            }
            else
            {
                var list = new SellListDto(); list.ReadFrom(reader);
                Check(list.Products.Count == 1 && list.Products[0].ObjId == 123 && list.Products[0].Price == 55, "Sell wire fields");
            }
            foreach (byte[] invalid in new[] { ListPacket(2, 1), ListPacket(0, 1), ListPacket(-1, 0), new byte[] { 0x11, 0 } })
            {
                bool rejected = false;
                try
                {
                    if (buy) new BuyListDto().ReadFrom(new PacketReader(invalid));
                    else new SellListDto().ReadFrom(new PacketReader(invalid));
                }
                catch (InvalidDataException) { rejected = true; }
                Check(rejected, "Truncated or mismatched merchant list must be rejected");
            }
        }
        var cart = new MerchantCart(new[] { Product(1060, 0, 0), Product(1, 0, 0) }, true, item => item.ItemId == 1060);
        Check(!cart.TryGetTotal(out _), "Empty basket has no transaction");
        Check(cart.Move(cart.Available[0], 3, true) && cart.Move(cart.Available[0], 2, true), "Unlimited stack can be selected repeatedly");
        Check(cart.Selected.Count == 1 && cart.Selected[0].Count == 5 && cart.Available[0].Count == 0, "Unlimited source remains unlimited, one request row");
        Check(cart.Move(cart.Selected[0], 2, false) && cart.Selected[0].Count == 3, "Partial return removes only requested units");
        Check(cart.Move(cart.Available[1], 1, true) && !cart.Move(cart.Available[1], 1, true), "Non-stackable item cannot create duplicate buy rows");
        Check(!cart.Move(cart.Available[0], 0, true) && !cart.Move(cart.Available[0], -1, true), "Zero and negative quantity rejected");
        Check(cart.TryGetTotal(out long total) && total == 40, "Price uses selected units");
        var sale = new MerchantCart(new[] { Product(1060, 100, 10), Product(1, 101, 1), Product(1, 102, 1) }, false, item => item.ItemId == 1060);
        Check(sale.Move(sale.Available[0], 4, true) && sale.Available[0].Count == 6, "Sale takes part of owned stack");
        Check(!sale.Move(sale.Available[0], 7, true), "Sale cannot select more than remaining inventory");
        Check(sale.Move(sale.Selected[0], 2, false) && sale.Available[0].Count == 8 && sale.Selected[0].Count == 2, "Sale cancel restores exact count");
        Check(sale.Move(sale.Available[1], 1, true) && sale.Move(sale.Available[1], 1, true) && sale.Selected.Count == 3, "Distinct owned weapons remain separate rows");
        var limited = new MerchantCart(new[] { Product(1060, 0, 3) }, true, item => true);
        Check(!limited.Move(limited.Available[0], 4, true), "Limited stock cap");
        Check(limited.Move(limited.Available[0], 3, true) && limited.Available.Count == 0, "Exhausted limited stock removed");
        Check(limited.Move(limited.Selected[0], 1, false) && limited.Available[0].Count == 1, "Return restores finite stock");
        var large = new MerchantCart(new[] { Product(1060, 0, 0) }, true, item => true);
        Check(large.Move(large.Available[0], int.MaxValue, true) && !large.Move(large.Available[0], 1, true), "Quantity overflow rejected");
        Check(large.TryGetTotal(out total) && total == (long)int.MaxValue * 10, "Money multiplication does not wrap int32");
        CheckOutgoing(cart.Selected);
        Debug.Log("[LocalMerchantChecks] Merchant rows, truncation, stock, basket identity, partial returns, totals and outgoing layouts passed");
    }

    static void CheckOutgoing(List<Product> products)
    {
        var registry = new OutgoingPacketAutoRegistry();
        byte[] buy = registry.Create(new RequestBuyItemCommand(3015000, products)).GetData();
        Check(buy[0] == 0x1F && BitConverter.ToInt32(buy, 1) == 3015000 && BitConverter.ToInt32(buy, 5) == products.Count &&
            buy.Length == 9 + products.Count * 8 && BitConverter.ToInt32(buy, 9) == products[0].ItemId &&
            BitConverter.ToInt32(buy, 13) == products[0].Count, "Buy request layout");
        byte[] sell = registry.Create(new RequestSellItemCommand(0, products)).GetData();
        Check(sell[0] == 0x1E && sell.Length == 9 + products.Count * 12 &&
            BitConverter.ToInt32(sell, 9) == products[0].ObjId && BitConverter.ToInt32(sell, 13) == products[0].ItemId &&
            BitConverter.ToInt32(sell, 17) == products[0].Count, "Sell request uses owned object id");
    }

    static Product Product(int item, int obj, int count) => new Product(4, obj, count, 5, 0, 0, 0, 10, item);

    static byte[] ListPacket(short announced, int rows)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((byte)0x11); writer.Write(5000); writer.Write(3015000); writer.Write(announced);
            for (int i = 0; i < rows; i++)
            {
                writer.Write((short)4); writer.Write(123); writer.Write(1060); writer.Write(7);
                writer.Write((short)5); writer.Write((short)0); writer.Write(0);
                writer.Write((short)0); writer.Write((short)0); writer.Write((short)0); writer.Write(55);
            }
            return stream.ToArray();
        }
    }

    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
