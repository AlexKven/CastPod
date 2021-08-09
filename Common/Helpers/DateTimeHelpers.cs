using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Helpers
{
    public static class DateTimeHelpers
    {
        public static bool TryUniversalParseDate(string str, out DateTime dateTime)
        {
            dateTime = default;
            var curStr = "";

            int? month = null;
            int? day = null;
            int? year = null;

            var wordSeparators = new HashSet<char>()
                {
                    ' ', ',', '.', ';', ';', '\\', '/', '-'
                };

            bool nextWord(ref DateTime dateTime)
            {
                var monthStr = curStr.ToLower() switch
                {
                    "january" or "jan" => 1,
                    "february" or "feb" => 2,
                    "march" or "mar" => 3,
                    "april" or "apr" => 4,
                    "may" => 5,
                    "june" or "jun" => 6,
                    "july" or "jul" => 7,
                    "august" or "aug" => 8,
                    "september" or "sep" => 9,
                    "october" or "oct" => 10,
                    "november" or "nov" => 11,
                    "december" or "dec" => 12,
                    _ => 0
                };
                if (monthStr > 0)
                {
                    month = monthStr;
                }
                else if (int.TryParse(curStr, out var num))
                {
                    if (num > 31)
                        year = num;
                    else if (!day.HasValue)
                    {
                        day = num;
                    }
                    else if (!month.HasValue && num <= 12)
                    {
                        month = num;
                    }

                }
                curStr = "";

                if (day.HasValue && month.HasValue && year.HasValue)
                {
                    try
                    {
                        dateTime = new DateTime(year.Value, month.Value, day.Value);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                return false;
            }

            foreach (var chr in str)
            {
                if (wordSeparators.Contains(chr))
                {
                    if (nextWord(ref dateTime))
                        return true;
                }
                else
                {
                    curStr += chr;
                }
            }
            return nextWord(ref dateTime);
        }
    }
}
