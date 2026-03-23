using Agents.Sample;

try
{
    List<int> numbers = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    INumberReportAnalyzer analyzer = new BadStyle();
    NumberReport report = analyzer.BuildReport(numbers);

    Console.WriteLine($"Status    : {report.Status}");
Console.WriteLine($"Count     : {report.Count}");
Console.WriteLine($"Total     : {report.Total}");
Console.WriteLine($"Average   : {report.Average:F2}");
Console.WriteLine($"Min / Max : {report.Min} / {report.Max}");
Console.WriteLine($"Evens     : {string.Join(", ", report.Evens)}");
Console.WriteLine($"Odds      : {string.Join(", ", report.Odds)}");
Console.WriteLine($"FirstFive : {string.Join(", ", report.FirstFive)}");
Console.WriteLine($"LastFive  : {string.Join(", ", report.LastFive)}");
    Console.WriteLine($"Generated : {report.GeneratedAt:O}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Report generation failed: {ex.GetType().Name} - {ex.Message}");
    Environment.Exit(1);
}
