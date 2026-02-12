using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Utility;
public class CsvTool
{
    public static void ToCsv(IEnumerable<IConvertible[]> lineData, string[] headRow = null, string filePath = null)
    {
        filePath = filePath ?? Path.Combine(Environment.CurrentDirectory, $"export_{DateTime.Now.ToLongDateString()}.csv");
        using (var streamWriter = new StreamWriter(filePath, false, Encoding.UTF8))
        {
            if (headRow != null && headRow.Length > 1)
            {
                foreach (var item in headRow)
                {
                    streamWriter.Write(item);
                    streamWriter.Write(',');
                }
                streamWriter.WriteLine();
            }
            foreach (var line in lineData)
            {
                foreach (var item in line)
                {
                    streamWriter.Write(item);
                    streamWriter.Write(',');
                }
                streamWriter.WriteLine();
            }
        }

    }
    public static void ToCsv<T>(Func<T, IConvertible[]>func, IEnumerable<T> objs, string[] headRow = null, string filePath = null)
    {
        filePath = filePath ?? Path.Combine(Environment.CurrentDirectory, $"export_{DateTime.Now.ToLongDateString()}.csv");
        using (var streamWriter = new StreamWriter(filePath, false, Encoding.UTF8))
        {
            if (headRow != null && headRow.Length > 1)
            {
                foreach (var item in headRow)
                {
                    streamWriter.Write(item);
                    streamWriter.Write(',');
                }
                streamWriter.WriteLine();
            }
            foreach (var obj in objs)
            {
                var line= func(obj);
                foreach (var item in line)
                {
                    streamWriter.Write(item);
                    streamWriter.Write(',');
                }
                streamWriter.WriteLine();
            }
        }

    }
}

