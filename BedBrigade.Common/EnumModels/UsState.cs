namespace BedBrigade.Common.EnumModels;

public class UsState
{
    public string? StateCode { get; set; }
    public string? StateName { get; set; }
    public int ZipCodeMin { get; set; } = 0;
    public int ZipCodeMax { get; set; } = 0;
}