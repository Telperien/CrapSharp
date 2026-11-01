namespace Lib;

public sealed class Invoices
{
    public static string Summarize(IReadOnlyList<decimal> amounts, decimal taxRate)
    {
        var subtotal = 0m;
        var count = 0;
        foreach (var amount in amounts)
        {
            if (amount <= 0)
                continue;
            subtotal += amount;
            count++;
        }
        var tax = Math.Round(subtotal * taxRate, 2);
        var total = subtotal + tax;
        return $"{count} lines, subtotal {subtotal:0.00}, tax {tax:0.00}, total {total:0.00}";
    }

    public static string Describe(IReadOnlyList<int> sizes, int ceiling)
    {
        var total = 0;
        var accepted = 0;
        foreach (var size in sizes)
        {
            if (size >= ceiling)
                continue;
            total += size;
            accepted++;
        }
        var mean = accepted == 0 ? 0 : total / accepted;
        return $"{accepted} entries, total {total}, mean {mean}";
    }
}
