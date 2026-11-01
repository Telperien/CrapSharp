namespace Lib;

public sealed class Orders
{
    // Exact clone of Invoices.Summarize below (same text).
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

    // Renamed clone of Invoices.Describe: same structure, other identifiers and literals.
    public static string Describe(IReadOnlyList<int> weights, int limit)
    {
        var sum = 0;
        var kept = 0;
        foreach (var weight in weights)
        {
            if (weight > limit)
                continue;
            sum += weight;
            kept++;
        }
        var average = kept == 0 ? 0 : sum / kept;
        return $"{kept} items, sum {sum}, average {average}";
    }
}
