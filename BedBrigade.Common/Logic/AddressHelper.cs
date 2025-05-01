using BedBrigade.Common.EnumModels;

namespace BedBrigade.Common.Logic
{
    public static class AddressHelper
    {
        public static List<UsState> GetStateList()
        {
            List<UsState> StateList = new List<UsState>
                {
                    new UsState {StateCode = "AL", StateName = "Alabama",ZipCodeMin = 35004, ZipCodeMax = 36925},
                    new UsState {StateCode = "AK", StateName = "Alaska",ZipCodeMin = 99501,ZipCodeMax = 99950},
                    new UsState {StateCode = "AZ",StateName = "Arizona",ZipCodeMin = 85001,ZipCodeMax = 86556},
                    new UsState {StateCode = "AR",StateName = "Arkansas",ZipCodeMin = 71601,ZipCodeMax = 72959},
                    new UsState {StateCode = "CA",StateName = "California",ZipCodeMin = 90001,ZipCodeMax = 96162},
                    new UsState {StateCode = "CO",StateName = "Colorado",ZipCodeMin = 80001,ZipCodeMax = 81658},
                    new UsState {StateCode = "CT",StateName = "Connecticut",ZipCodeMin = 6001,ZipCodeMax = 6928},
                    new UsState {StateCode = "DE",StateName = "Delaware",ZipCodeMin = 19701,ZipCodeMax = 19980},
                    new UsState {StateCode = "DC",StateName = "District of Columbia",ZipCodeMin = 20001,ZipCodeMax = 20799},
                    new UsState {StateCode = "FL",StateName = "Florida",ZipCodeMin = 32003,ZipCodeMax = 34997},
                    new UsState {StateCode = "GA",StateName = "Georgia",ZipCodeMin = 30002,ZipCodeMax = 39901},
                    new UsState {StateCode = "HI",StateName = "Hawaii",ZipCodeMin = 96701,ZipCodeMax = 96898},
                    new UsState {StateCode = "ID",StateName = "Idaho",ZipCodeMin = 83201,ZipCodeMax = 83877},
                    new UsState {StateCode = "IL",StateName = "Illinois",ZipCodeMin = 60001,ZipCodeMax = 62999},
                    new UsState {StateCode = "IN",StateName = "Indiana",ZipCodeMin = 46001,ZipCodeMax = 47997},
                    new UsState {StateCode = "IA",StateName = "Iowa",ZipCodeMin = 50001,ZipCodeMax = 52809},
                    new UsState {StateCode = "KS",StateName = "Kansas",ZipCodeMin = 66002,ZipCodeMax = 67954},
                    new UsState {StateCode = "KY",StateName = "Kentucky",ZipCodeMin = 40003,ZipCodeMax = 42788},
                    new UsState {StateCode = "LA",StateName = "Louisiana",ZipCodeMin = 70001,ZipCodeMax = 71497},
                    new UsState {StateCode = "ME",StateName = "Maine",ZipCodeMin = 3901,ZipCodeMax = 4992},
                    new UsState {StateCode = "MD",StateName = "Maryland",ZipCodeMin = 20588,ZipCodeMax = 21930},
                    new UsState {StateCode = "MA",StateName = "Massachusetts",ZipCodeMin = 1001,ZipCodeMax = 5544},
                    new UsState {StateCode = "MI",StateName = "Michigan",ZipCodeMin = 48001,ZipCodeMax = 49971},
                    new UsState {StateCode = "MN",StateName = "Minnesota",ZipCodeMin = 55001,ZipCodeMax = 56763},
                    new UsState {StateCode = "MS",StateName = "Mississippi",ZipCodeMin = 38601,ZipCodeMax = 39776},
                    new UsState {StateCode = "MO",StateName = "Missouri",ZipCodeMin = 63001,ZipCodeMax = 65899},
                    new UsState {StateCode = "MT",StateName = "Montana",ZipCodeMin = 59001,ZipCodeMax = 59937},
                    new UsState {StateCode = "NE",StateName = "Nebraska",ZipCodeMin = 68001,ZipCodeMax = 69367},
                    new UsState {StateCode = "NV",StateName = "Nevada",ZipCodeMin = 88901,ZipCodeMax = 89883},
                    new UsState {StateCode = "NH",StateName = "New Hampshire",ZipCodeMin = 3031,ZipCodeMax = 3897},
                    new UsState {StateCode = "NJ",StateName = "New Jersey",ZipCodeMin = 7001,ZipCodeMax = 8989},
                    new UsState {StateCode = "NM",StateName = "New Mexico",ZipCodeMin = 87001,ZipCodeMax = 88441},
                    new UsState {StateCode = "NY",StateName = "New York",ZipCodeMin = 501,ZipCodeMax = 14975},
                    new UsState {StateCode = "NC",StateName = "North Carolina",ZipCodeMin = 27006,ZipCodeMax = 28909},
                    new UsState {StateCode = "ND",StateName = "North Dakota",ZipCodeMin = 58001,ZipCodeMax = 58856},
                    new UsState {StateCode = "OH",StateName = "Ohio",ZipCodeMin = 43001,ZipCodeMax = 45999},
                    new UsState {StateCode = "OK",StateName = "Oklahoma",ZipCodeMin = 73001,ZipCodeMax = 74966},
                    new UsState {StateCode = "OR",StateName = "Oregon",ZipCodeMin = 97001,ZipCodeMax = 97920},
                    new UsState {StateCode = "PA",StateName = "Pennsylvania",ZipCodeMin = 15001,ZipCodeMax = 19640},
                    new UsState {StateCode = "RI",StateName = "Rhode Island",ZipCodeMin = 2801,ZipCodeMax = 2940},
                    new UsState {StateCode = "SC",StateName = "South Carolina",ZipCodeMin = 29001,ZipCodeMax = 29948},
                    new UsState {StateCode = "SD",StateName = "South Dakota",ZipCodeMin = 57001,ZipCodeMax = 57799},
                    new UsState {StateCode = "TN",StateName = "Tennessee",ZipCodeMin = 37010,ZipCodeMax = 38589},
                    new UsState {StateCode = "TX",StateName = "Texas",ZipCodeMin = 73301,ZipCodeMax = 88595},
                    new UsState {StateCode = "UT",StateName = "Utah",ZipCodeMin = 84001,ZipCodeMax = 84791},
                    new UsState {StateCode = "VT",StateName = "Vermont",ZipCodeMin = 5001,ZipCodeMax = 5907},
                    new UsState {StateCode = "VA",StateName = "Virginia",ZipCodeMin = 20101,ZipCodeMax = 24658},
                    new UsState {StateCode = "WA",StateName = "Washington",ZipCodeMin = 98001,ZipCodeMax = 99403},
                    new UsState {StateCode = "WV",StateName = "West Virginia",ZipCodeMin = 24701,ZipCodeMax = 26886},
                    new UsState {StateCode = "WI",StateName = "Wisconsin",ZipCodeMin = 53001,ZipCodeMax = 54990},
                    new UsState {StateCode = "WY",StateName = "Wyoming",ZipCodeMin = 82001,ZipCodeMax = 83414}

                };
            return StateList;
        } // Get State List
          

        public static string? FindStateAbbreviation(string input)
        {
            // Normalize the input (e.g., trim and make case-insensitive)
            string normalizedInput = input.Trim().ToLower();
            List<UsState> mystateList = GetStateList();

            // Check if the input matches any state abbreviation
            UsState matchByCode = mystateList.FirstOrDefault(state => state.StateCode.Equals(normalizedInput, StringComparison.OrdinalIgnoreCase));
            if (matchByCode != null)
            {
                return matchByCode.StateCode; // Return the abbreviation
            }

            // Check if the input matches any state name
            UsState matchByName = mystateList.FirstOrDefault(state => state.StateName.Equals(normalizedInput, StringComparison.CurrentCultureIgnoreCase));
            if (matchByName != null)
            {
                return matchByName.StateCode; // Return the abbreviation if the full state name matches
            }


            // Return null if no match is found
            return null;
        } //FindStateAbbreviation


    } // class

} // namespace
