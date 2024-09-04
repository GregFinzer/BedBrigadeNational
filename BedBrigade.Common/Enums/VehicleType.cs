using System.ComponentModel;

namespace BedBrigade.Common.Enums;

public enum VehicleType // added by VS 9/1/2023
{
    [Description("I do not have a delivery vehicle")]
    None = 0,
    [Description("I have a minivan")]
    Minivan = 1,
    [Description("I have a large SUV")]
    SUV = 2,
    [Description("I have a pickup truck with cap")]
    Truck = 3,
    [Description("I have other type of vehicle")]
    Other = 8,
}