using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BedBrigade.Common.Logic;

/// <summary>
/// Methods to make words plural or singular
/// </summary>
public static class Pluralization
{
    private static readonly IList<string> _unpluralizables = new List<string>
        { "equipment", "information", "rice", "money", "species", "series", "fish", "sheep", "deer" };

    private static readonly IDictionary<string, string> _pluralizations = new Dictionary<string, string>
    {
        // Start with the rarest cases, and move to the most common
        { "person", "people" },
        { "ox", "oxen" },
        { "child", "children" },
        { "foot", "feet" },
        { "tooth", "teeth" },
        { "goose", "geese" },
        // And now the more standard rules.
        { "(.*)fe?", "$1ves" }, // ie, wolf, wife
        { "(.*)man$", "$1men" },
        { "(.+[aeiou]y)$", "$1s" },
        { "(.+[^aeiou])y$", "$1ies" },
        { "(.+z)$", "$1zes" },
        { "([m|l])ouse$", "$1ice" },
        { "(.+)(e|i)x$", @"$1ices" }, // ie, Matrix, Index
        { "(octop|vir)us$", "$1i" },
        { "(.+(s|x|sh|ch))$", @"$1es" },
        { "(.+)", @"$1s" }
    };

    private static readonly IDictionary<string, string> _singularizations = new Dictionary<string, string>
    {
        // Start with the rarest cases, and move to the most common
        { "people", "person" },
        { "oxen", "ox" },
        { "children", "child" },
        { "feet", "foot" },
        { "teeth", "tooth" },
        { "geese", "goose" },
        // And now the more standard rules.
        { "(.*)ives?", "$1ife" },
        { "(.*)ves?", "$1f" },
        // ie, wolf, wife
        { "(.*)men$", "$1man" },
        { "(.+[aeiou])ys$", "$1y" },
        { "(.+[^aeiou])ies$", "$1y" },
        { "(.+)zes$", "$1" },
        { "([m|l])ice$", "$1ouse" },
        { "matrices", @"matrix" },
        { "indices", @"index" },
        { "(.+[^aeiou])ices$", "$1ice" },
        { "(.*)ices", @"$1ex" },
        // ie, Matrix, Index
        { "(octop|vir)i$", "$1us" },
        { "(.+(s|x|sh|ch))es$", @"$1" },
        { "(.+)s", @"$1" }
    };

    /// <summary>
    /// Returns true if the word is plural
    /// </summary>
    /// <param name="word"></param>
    /// <returns></returns>
    public static bool IsPlural(string word)
    {
        if (_unpluralizables.Contains(word.ToLowerInvariant()))
        {
            return true;
        }

        foreach (var singularization in _singularizations)
        {
            if (Regex.IsMatch(word, singularization.Key))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Make a word plural
    /// </summary>
    /// <param name="singular"></param>
    /// <returns></returns>
    public static string MakePlural(string singular)
    {
        if (_unpluralizables.Contains(singular))
            return singular;

        var plural = "";

        foreach (var pluralization in _pluralizations)
        {
            if (Regex.IsMatch(singular, pluralization.Key))
            {
                plural = Regex.Replace(singular, pluralization.Key, pluralization.Value);
                break;
            }
        }

        return plural;
    }


    /// <summary>
    /// Make a word singular
    /// </summary>
    /// <param name="word"></param>
    /// <returns></returns>
    public static string MakeSingular(string word)
    {
        Dictionary<string, string> list = new Dictionary<string, string>();

        //Equivalent
        list.Add("equipment", "equipment");
        list.Add("information", "information");
        list.Add("rice", "rice");
        list.Add("money", "money");
        list.Add("species", "species");
        list.Add("series", "series");
        list.Add("fish", "fish");
        list.Add("sheep", "sheep");

        //Plural Forms
        list.Add("([^aeiouy]|qu)ies$", "$1y");
        list.Add("s$", "");
        list.Add("(n)ews$", "$1ews");
        list.Add("([ti])a$", "$1um");
        list.Add("((a)naly|(b)a|(d)iagno|(p)arenthe|(p)rogno|(s)ynop|(t)he)ses$", "$1$2sis");
        list.Add("(^analy)ses$", "$1sis");
        list.Add("([^f])ves$", "$1fe");
        list.Add("(hive)s$", "$1");
        list.Add("(tive)s$", "$1");
        list.Add("([lr])ves$", "$1f");
        list.Add("(s)eries$", "$1eries");
        list.Add("(m)ovies$", "$1ovie");
        list.Add("(x|ch|ss|sh)es$", "$1");
        list.Add("([m|l])ice$", "$1ouse");
        list.Add("(bus)es$", "$1");
        list.Add("(o)es$", "$1");
        list.Add("(shoe)s$", "$1");
        list.Add("(cris|ax|test)es$", "$1is");
        list.Add("([octop|vir])i$", "$1us");
        list.Add("(alias|status)es$", "$1");
        list.Add("^(ox)en$", "$1");
        list.Add("(vert|ind)ices$", "$1ex");
        list.Add("(matr)ices$", "$1ix");
        list.Add("(quiz)zes$", "$1");

        foreach (string key in list.Keys)
        {
            Regex re = new Regex(key, RegexOptions.IgnoreCase);

            if (re.IsMatch(word))
                return re.Replace(word, list[key]);
        }

        return word;
    }

}

