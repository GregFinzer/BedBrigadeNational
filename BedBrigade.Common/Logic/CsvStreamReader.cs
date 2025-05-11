using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BedBrigade.Common.Logic
{
    internal class CsvStreamReader
    {
        public int Position { get; set; }
        private readonly CsvReader _csvReader ;
        public CsvStreamReader(CsvReader csvReader)
        {
            _csvReader = csvReader;
            Position = 0;
        }

        public TextReader BuildTextReader(Stream stream)
        {
            TextReader reader = new StreamReader(stream, _csvReader.DefaultEncoding);
            return reader;
        }

        public List<string> ReadHeader(TextReader reader)
        {
            List<string> row = ReadRow(reader);

            if (!_csvReader.HasHeader)
            {                
                return GetDummyHeader(row.Count);
            }

            return row;
        }

        public List<List<string>> CsvStreamToStringList(Stream stream)
        {
            bool firstRow = true;
            TextReader reader = BuildTextReader(stream);
            List<List<string>> result = new List<List<string>>();

            while (reader.Peek() != -1)
            {
                List<string> row = ReadRow(reader);

                if (row == null)
                    break;

                if (firstRow && !_csvReader.HasHeader)
                {
                    firstRow = false;
                    result.Add(GetDummyHeader(row.Count));
                }

                result.Add(row);
            }

            return result;
        }


        public List<string> ReadRow(TextReader reader)
        {
            var row = new List<string>();
            var isStringBlock = false;
            var sb = new StringBuilder();
            char lastChar = '\0';

            while (reader.Peek() != -1)
            {
                char c = (char)reader.Read();

                if (c == _csvReader.QuoteCharacter && lastChar != _csvReader.EscapeCharacter)
                    isStringBlock = !isStringBlock;

                if (c == _csvReader.Seperator && !isStringBlock && lastChar != _csvReader.EscapeCharacter) //end of word
                {
                    row.Add(CleanWord(sb)); //add word
                    sb.Length = 0;
                }
                else if (c == '\n' && !isStringBlock) //end of line
                {
                    Position++;
                    row.Add(CleanWord(sb)); //add last word in line
                    sb.Length = 0;

                    //If there is only one column it must have data (Skip Blank Lines)
                    if (row.Count > 0 && (row.Count > 1 || !string.IsNullOrEmpty(row[0])))
                        return row;

                    row = new List<string>();
                }
                else
                {
                    sb.Append(c);
                }

                lastChar = c;
            }

            row.Add(CleanWord(sb)); //add last word

            //If there is only one column it must have data (Skip Blank Lines)
            if (row.Count > 0 && (row.Count > 1 || !string.IsNullOrEmpty(row[0])))
            {
                Position++;
                return row;
            }

            return null;
        }




        public List<string> GetDummyHeader(int maxColumns)
        {
            List<string> header = new List<string>();
            for (int i = 1; i <= maxColumns; i++)
            {
                header.Add(string.Format("Column{0}", i));
            }

            return header;
        }

        /// <summary>
        /// Remove double quotes, escaped quotes, and escaped delimiters
        /// </summary>
        /// <param name="sb"></param>
        /// <returns></returns>
        private string CleanWord(StringBuilder sb)
        {
            string doubleQuotes = "".PadRight(2, _csvReader.QuoteCharacter);
            string singleQuote = "".PadRight(1, _csvReader.QuoteCharacter);

            //Two double quotes
            if (sb.Length == 2 && sb.ToString() == doubleQuotes)
                return string.Empty;

            bool endsWithDoubleDoubleQuotes = sb.ToString().EndsWith(doubleQuotes);

            //Replace Double Double quotes with single double quotes.  "" with "
            sb.Replace(doubleQuotes, singleQuote);

            //Replace Escaped Double Quote.  \" with "
            sb.Replace(_csvReader.EscapeCharacter.ToString() + singleQuote, singleQuote);

            //Escaped Separator.  \, with ,
            sb.Replace("\\" + _csvReader.Seperator.ToString(), _csvReader.Seperator.ToString());

            string result = sb.ToString();
            if (_csvReader.TrimSpaces)
            {
                result = result.Trim();
            }
            else
            {
                result = result.Trim('\r', '\n');
            }

            if (result.Length >= 3 && result.StartsWith(singleQuote) && result.EndsWith(singleQuote))
            {
                if (endsWithDoubleDoubleQuotes)
                {
                    result = result.Substring(1, result.Length - 1);
                }
                else
                {
                    result = result.Substring(1, result.Length - 2);
                }
            }

            if (result == string.Empty && _csvReader.ConvertBlankToNull)
                return null;

            return result;
        }
    }
}
