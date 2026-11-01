using System.Globalization;
using System.Xml.Linq;

namespace CrapSharp.Coverage;

/// <summary>Reads the line coverage of a Cobertura XML report (the format produced by coverlet).</summary>
public static class CoberturaParser
{
    public static CoverageMap ParseFile(string reportPath) => Parse(XDocument.Load(reportPath));

    public static CoverageMap Parse(XDocument report)
    {
        var sources = report.Descendants("source").Select(s => s.Value.Trim()).Where(s => s.Length > 0).ToList();
        var map = new CoverageMap();
        foreach (var cls in report.Descendants("class"))
            RecordClass(map, cls, sources);
        return map;
    }

    // Line elements appear under the class and again under each of its methods: Record keeps the max, so no double count.
    private static void RecordClass(CoverageMap map, XElement cls, List<string> sources)
    {
        var file = ResolveFile((string?)cls.Attribute("filename"), sources);
        if (file is null)
            return;
        foreach (var line in cls.Descendants("line"))
            map.Record(file, Int(line, "number"), Int(line, "hits"));
    }

    private static int Int(XElement element, string attribute) =>
        int.Parse((string?)element.Attribute(attribute) ?? "0", CultureInfo.InvariantCulture);

    private static string? ResolveFile(string? filename, List<string> sources)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return null;
        if (Path.IsPathRooted(filename))
            return filename;
        var candidates = sources.Select(source => Path.Combine(source, filename)).ToList();
        return candidates.FirstOrDefault(File.Exists) ?? candidates.FirstOrDefault();
    }
}
