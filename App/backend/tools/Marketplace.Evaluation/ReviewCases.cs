using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Marketplace.Evaluation;

internal static class ReviewCases
{
    internal const string Version = "business-review-cases-v1";
    internal static readonly string[] Categories = ["Cafeterías", "Panaderías", "Ferreterías", "Librerías"];
    internal static readonly string[] Zones = ["Centro", "Nueva Córdoba", "Alberdi"];
    private static readonly string[][] Offers = [
        ["café al paso", "desayunos con tostadas", "meriendas dulces", "bebidas sin lácteos"],
        ["pan de masa madre", "facturas", "tortas por encargo", "panificados sin ingredientes de origen animal"],
        ["herramientas manuales", "materiales para pintura", "accesorios de plomería", "artículos de electricidad"],
        ["textos escolares", "novelas", "libros infantiles", "útiles de escritura"]];

    internal static int[] Seeds(string split) => split switch
    {
        "training" => [61], "calibration" => [101, 103, 107],
        _ => throw new ArgumentException("Only training/calibration packages can be prepared; evaluation remains reserved.")
    };
    internal static string DatasetId(int seed) => $"{Version}/{seed.ToString(CultureInfo.InvariantCulture)}";
    internal static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
    internal static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));
    internal static string SelectionKey(int seed, int sequence) => Hash(string.Create(CultureInfo.InvariantCulture, $"review-v1|{seed}|{sequence}"));
    internal static IReadOnlyList<(PilotDataset Data, SyntheticQuery Query)> Select(PilotDataset[] datasets, string split)
    {
        var selected = new List<(PilotDataset Data, SyntheticQuery Query)>();
        var strata = from surface in new[] { "magazine", "search" } from zone in Enumerable.Range(0, 3) select $"{surface}/{zone}";
        foreach (var stratum in strata)
        {
            var all = datasets.SelectMany(data => data.Queries.Where(q => q.Stratum == stratum).Select(query => (Data: data, Query: query)))
                .OrderBy(x => SelectionKey(x.Data.Seed, x.Query.Sequence), StringComparer.Ordinal)
                .ThenBy(x => x.Data.Seed).ThenBy(x => x.Query.Sequence).ToArray();
            foreach (var dataset in datasets)
            {
                var first = all.Where(x => x.Data.Seed == dataset.Seed).Take(split == "training" ? 2 : 1).ToArray();
                if (first.Length != (split == "training" ? 2 : 1)) throw new ArgumentException("Insufficient queries in a required stratum.");
                selected.AddRange(first);
            }
            if (split == "calibration")
            {
                var extra = all.Where(x => !selected.Contains(x)).Take(2).ToArray();
                if (extra.Length != 2) throw new ArgumentException("Insufficient calibration queries.");
                selected.AddRange(extra);
            }
        }
        return selected.OrderBy(x => x.Data.Seed).ThenBy(x => x.Query.Sequence).ToArray();
    }
    internal static string Need(SyntheticQuery query) => query.Surface == "magazine"
        ? $"Explorar opciones cercanas de {Categories[query.Category].ToLowerInvariant()} dentro del radio indicado."
        : $"Encontrar {Offers[query.Category][query.Need]}; prefiero opciones cercanas dentro del radio indicado.";
    internal static string Offer(SyntheticBusiness business) =>
        $"Negocio ficticio. Oferta disponible en este escenario: {Offers[business.Category][business.PrimaryNeed]}; {Offers[business.Category][business.SecondaryNeed]}.";
    internal static byte[] Csv(string header, IEnumerable<string[]> rows) => Encoding.UTF8.GetBytes(header + "\n" +
        string.Concat(rows.Select(row => string.Join(",", row.Select(value => "\"" + value.Replace("\"", "\"\"") + "\"")) + "\n")));
}
