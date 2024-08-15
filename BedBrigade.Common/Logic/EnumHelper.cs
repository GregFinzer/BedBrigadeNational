using System.ComponentModel;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Logic
{
    public static class EnumHelper
    {
        public static string GetEnumDescription<T>(T value) where T : Enum
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        public static List<EnumNameValue<T>> GetEnumNameValues<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>().Select(e => new EnumNameValue<T> { Value = e, Name = GetEnumDescription(e) }).ToList();
        }

        /// <summary>
        /// Get a list of Enum Items suitable for a dropdown list from the EventTypeEnumItems
        /// </summary>
        /// <returns>List<EnumItem></EnumItem></returns>
        public static List<EventTypeEnumItem> GetEventTypeItems() // added by VS 7/1/2023
        {
            var type = typeof(EventType);
            return Enum.GetValues(type).OfType<EventType>().ToList()
                .Select(x => new EventTypeEnumItem
                {
                    Value = x,
                    Name = Enum.GetName(type, x)
                })
                .ToList();
        }

        /// <summary>
        /// Get a list of Enum Items suitable for a dropdown list from the BedRequestStatusEnum
        /// </summary>
        /// <returns>List<EnumItem></EnumItem></returns>
        public static List<ContentTypeEnumItem> GetContentTypeItems()
        {
            var type = typeof(ContentType);
            return Enum.GetValues(type).OfType<ContentType>().ToList()
                            .Select(x => new ContentTypeEnumItem
                            {
                                Value = x,
                                Name = Enum.GetName(type, x)
                            })
                            .ToList();
        }

        public static List<VehicleTypeEnumItem> GetVehicleTypeItems()
        {
            var type = typeof(VehicleType);
            return Enum.GetValues(type).OfType<VehicleType>().ToList()
                            .Select(x => new VehicleTypeEnumItem
                            {
                                Value = x,
                                Name = Enum.GetName(type, x)
                            })
                            .ToList();
        }


        public static List<EventStatusEnumItem> GetEventStatusItems() // added by VS 7/1/2023
        {
            var type = typeof(EventStatus);
            return Enum.GetValues(type).OfType<EventStatus>().ToList()
                            .Select(x => new EventStatusEnumItem
                            {
                                Value = x,
                                Name = Enum.GetName(type, x)
                            })
                            .ToList();
        } // Get Event Status Items


        /// <summary>
        /// Get a list of Enum Items suitable for a dropdown list from the BedRequestStatusEnum
        /// </summary>
        /// <returns>List<EnumItem></EnumItem></returns>
        public static List<BedRequestEnumItem> GetBedRequestStatusItems()
        {
            var type = typeof(BedRequestStatus);
            return Enum.GetValues(type).OfType<BedRequestStatus>().ToList()
                            .Select(x => new BedRequestEnumItem
                            {
                                Value = x,
                                Name = Enum.GetName(type, x)
                            })
                            .ToList();
        }



        /// <summary>
        /// Get a list of Enum Items suitable for a dropdown list from the ConfigSection enum.
        /// </summary>
        /// <returns>List<EnumItem></EnumItem></returns>
        public static List<FileUseEnumItem> GetFileUseItems()
        {
            var type = typeof(FileUse);
            return Enum.GetValues(type).OfType<FileUse>().ToList()
                            .Select(x => new FileUseEnumItem
                            {
                                Value = x,
                                Name = Enum.GetName(type, x)
                            })
                            .ToList();
        }





        /// <summary>
        /// Get a list of Enum Items suitable for a dropdown list from the ConfigSection enum.
        /// </summary>
        /// <returns>List<EnumItem></EnumItem></returns>
        public static List<ConfigSectionEnumItem> GetConfigSectionItems()
        {
            var type = typeof(ConfigSection);
            return Enum.GetValues(type).OfType<ConfigSection>().ToList()
                            .Select(x => new ConfigSectionEnumItem
                            {
                                Value = x,
                                Name = Enum.GetName(type, x)
                            })
                            .ToList();
        }
    }
}
