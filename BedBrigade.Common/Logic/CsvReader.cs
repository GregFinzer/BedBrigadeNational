using System.Text;

namespace BedBrigade.Common.Logic
{
    public class CsvReader
    {
        private CsvStreamReader _csvStreamReader;
        public Encoding DefaultEncoding { get; set; }
        public bool HasHeader { get; set; }
        public char Seperator { get; set; }
        public char QuoteCharacter { get; set; }
        public char EscapeCharacter { get; set; }
        public bool TrimSpaces { get; set; }
        public bool ConvertBlankToNull { get; set; }

        public CsvReader()
        {
            _csvStreamReader = new CsvStreamReader(this);
            DefaultEncoding = Encoding.Default;
            HasHeader = true;
            TrimSpaces = true;
            ConvertBlankToNull = false;
            Seperator = ',';
            QuoteCharacter = '"';
            EscapeCharacter = '\\';
        }

        public List<Dictionary<string, string>> CsvFileToDictionary(string filePath)
        {
            List<List<string>> list = CsvFileToStringList(filePath);
            return ConvertCsvListToCsvDictionary(list);
        }

        private List<List<string>> CsvFileToStringList(string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return CsvStreamToStringList(fileStream);
            }
        }

        private List<List<string>> CsvStreamToStringList(Stream stream)
        {
            return _csvStreamReader.CsvStreamToStringList(stream);
        }

        private List<Dictionary<string, string>> ConvertCsvListToCsvDictionary(List<List<string>> list)
        {
            if (list.Count <= 1)
                return new List<Dictionary<string, string>>();

            for (int headerIndex = 0; headerIndex < list[0].Count; headerIndex++)
            {
                list[0][headerIndex] = list[0][headerIndex].Replace(" ", string.Empty);
            }

            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            for (int rowIndex = 1; rowIndex < list.Count; rowIndex++)
            {
                Dictionary<string, string> dictionaryRow = new Dictionary<string, string>();

                for (int colIndex = 0; colIndex < list[rowIndex].Count; colIndex++)
                {
                    //Ignore extraneous columns and duplicate columns
                    if (colIndex < list[0].Count && !dictionaryRow.ContainsKey(list[0][colIndex]))
                    {
                        dictionaryRow.Add(list[0][colIndex], list[rowIndex][colIndex]);
                    }
                }

                result.Add(dictionaryRow);
            }

            return result;
        }
    }
}
