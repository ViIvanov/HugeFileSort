﻿using Sort;
using System.Diagnostics;
using System.Text;

const int DefaultChunkSize = 100_000;

if ((args?.Length ?? 0) == 0)
{
    Console.WriteLine($"Usage: {Path.GetFileNameWithoutExtension(Environment.ProcessPath)} <file path> [<chunk size>]");
    return -1;
}

var file = args![0];
var chunkSize = args?.Length > 1 ? int.Parse(args[1]) : DefaultChunkSize;

var comparer = new Comparer(StringComparison.Ordinal);

var stopwatch = Stopwatch.StartNew();

Encoding encoding;
List<string> tempFiles;
using (var reader = new StreamReader(file, true))
{
    encoding = reader.CurrentEncoding;

    tempFiles = reader
        .EnumerateLines()
        .Chunk(chunkSize)
        .AsParallel()
        .Select(chunk => {
            Array.Sort(chunk, comparer);
            var tempFileName = Path.GetTempFileName();
            File.WriteAllLines(tempFileName, chunk);
            return tempFileName;
        }).ToList();

}

stopwatch.Stop();
Console.WriteLine($"Splitted in {stopwatch.Elapsed}");

var stopwatchMerge = Stopwatch.StartNew();
stopwatch.Start();

try
{
      var files = tempFiles
            .Select(f => File.OpenText(f))
            .ToList();
        var destination = Path.ChangeExtension(file, $".Sorted{Path.GetExtension(file)}");
        File.WriteAllLines(destination, files.Select(f => f.EnumerateLines()).MergeLines(comparer), encoding);
        files.ForEach(f => f.Dispose());
        stopwatchMerge.Stop();
}
finally
{ 
    tempFiles.ForEach(f => File.Delete(f));
}

stopwatch.Stop();

Console.WriteLine($"Merged in {stopwatchMerge.Elapsed}");
Console.WriteLine($"Sorted in {stopwatch.Elapsed}");

return 0;


