namespace Lib;

// Declarative boilerplate: similar shape, not a duplication worth removing.
public sealed record CustomerDto(string Name, string Email, string Phone);

public sealed record SupplierDto(string Name, string Email, string Phone);

public sealed class CustomerOptions
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
}

public sealed class SupplierOptions
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
}
