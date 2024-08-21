namespace BedBrigade.Common.Logic;

public static class PhoneNumberValidator
{
    // List of known valid U.S. area codes
    private static readonly HashSet<int> ValidAreaCodes = new HashSet<int>
    {
        201, 202, 203, 205, 206, 207, 208, 209, 210, 212, 213, 214, 215, 216, 217, 218, 219, 224, 225, 228, 229,
        231, 234, 239, 240, 248, 251, 252, 253, 254, 256, 260, 262, 267, 269, 270, 276, 281, 301, 302, 303, 304,
        305, 307, 308, 309, 310, 312, 313, 314, 315, 316, 317, 318, 319, 320, 321, 323, 325, 330, 331, 334, 336,
        337, 339, 347, 351, 352, 360, 361, 386, 401, 402, 404, 405, 406, 407, 408, 409, 410, 412, 413, 414, 415,
        417, 419, 423, 424, 425, 432, 434, 435, 440, 443, 469, 470, 478, 479, 480, 484, 501, 502, 503, 504, 505,
        507, 508, 509, 510, 512, 513, 515, 516, 517, 518, 520, 530, 531, 539, 540, 541, 551, 559, 561, 562, 563,
        567, 570, 571, 573, 574, 575, 580, 585, 586, 601, 602, 603, 605, 606, 607, 608, 609, 610, 612, 614, 615,
        616, 617, 618, 619, 620, 623, 626, 628, 629, 630, 631, 636, 641, 646, 650, 651, 657, 660, 661, 662, 667,
        669, 678, 681, 682, 701, 702, 703, 704, 706, 707, 708, 712, 713, 714, 715, 716, 717, 718, 719, 720, 724,
        725, 727, 731, 732, 734, 737, 740, 747, 754, 757, 760, 762, 763, 765, 769, 770, 772, 773, 774, 775, 779,
        781, 785, 786, 801, 802, 803, 804, 805, 806, 808, 810, 812, 813, 814, 815, 816, 817, 818, 828, 830, 831,
        832, 835, 843, 845, 847, 848, 850, 856, 857, 858, 859, 860, 862, 863, 864, 865, 870, 872, 878, 901, 903,
        904, 906, 907, 908, 909, 910, 912, 913, 914, 915, 916, 917, 918, 919, 920, 925, 928, 929, 931, 936, 937,
        940, 941, 947, 949, 951, 952, 954, 956, 959, 970, 971, 972, 973, 975, 978, 979, 980, 984, 985, 989
    };

    public static bool IsValidPhoneNumber(string phoneNumber)
    {
        phoneNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());

        // Ensure the phone number is exactly 10 digits long
        if (phoneNumber.Length != 10 || !long.TryParse(phoneNumber, out _))
        {
            return false;
        }

        // Extract the area code (first 3 digits)
        int areaCode = int.Parse(phoneNumber.Substring(0, 3));

        // Check if the area code is in the list of valid area codes
        if (!ValidAreaCodes.Contains(areaCode))
        {
            return false;
        }

        // Ensure the prefix starts with 2 to 9
        if (phoneNumber[3] == '0' || phoneNumber[3] == '1')
        {
            return false;
        }

        return true;
    }
}

