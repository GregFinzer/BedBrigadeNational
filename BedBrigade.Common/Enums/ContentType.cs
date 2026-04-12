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
    [Description("SignUp SMS Reminder Form")]
    SignUpSmsReminderForm = 9,
    Reserved = 10,
    News = 11,
    Stories = 12,
    [Description("Newsletter Form")]
    NewsletterForm = 13,
    [Description("Contact Us Confirmation Form")]
    ContactUsConfirmationForm = 14,
    [Description("Forgot Password Form")]
    ForgotPasswordForm = 15,
    [Description("Delivery SMS Reminder Form")]
    DeliverySmsReminderForm = 16,
    [Description("Delivery Email Reminder Form")]
    DeliveryEmailReminderForm = 17,
    [Description("SignUp Email Reminder Form")]
    SignUpEmailReminderForm = 18
}