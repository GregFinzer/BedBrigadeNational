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
    [Description("SignUp Email Confirmation Form")]
    SignUpEmailConfirmationForm = 8,
    [Description("SignUp SMS Confirmation Form")]
    SignUpSmsConfirmationForm = 9,
    Reserved = 10,
    News = 11,
    Stories = 12,
    [Description("Newsletter Form")]
    NewsletterForm = 13,
}