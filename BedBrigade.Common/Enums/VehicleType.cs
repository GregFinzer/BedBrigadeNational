using System.ComponentModel;

namespace BedBrigade.Common.Enums;

public enum VehicleType 
{
    [Description("I do not have a delivery vehicle")]
    None = 0,
    [Description("I have a minivan")]
    Minivan = 1,
    [Description("I have a full size van")]
    FullSizeVan = 2,
    [Description("I have a small SUV")]
    SmallSUV = 3,
    [Description("I have a medium SUV")]
    MediumSUV = 4,
    [Description("I have a large SUV")]
    LargeSUV = 5,
    [Description("I have a pickup truck")]
    Truck = 6,
    [Description("I have other type of vehicle")]
    Other = 8,
}