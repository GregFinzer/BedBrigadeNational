using System.ComponentModel;

namespace BedBrigade.Common.Enums;

public enum ContentType
{
    Header = 1,
    Footer = 2,
    Body = 3,
    Home = 4,
    [Description("Delivery Check List")]
    DeliveryCheckList = 5,
    [Description("Email Tax Form")]
    EmailTaxForm =6,
    [Description("Bed Request Confirmation Form")]
    BedRequestConfirmationForm = 7,
    [Description("SignUp Confirmation Form")]
    SignUpConfirmationForm = 8,
}