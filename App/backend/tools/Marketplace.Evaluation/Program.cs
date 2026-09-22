using System.Globalization;
using System.Text.Json;
using Marketplace.BLL;
using Marketplace.Evaluation;

try
{
    var options = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var i = 0; i < args.Length; i += 2)
    {
        if (i + 1 == args.Length || args[i] is not ("--output" or "--queries" or "--seeds" or "--config" or "--suite" or "--validate-review" or "--prepare-review" or "--collect-review" or "--package-sha256" or "--evaluate-review" or "--variant" or "--calibrate-review" or "--prepare-freeze" or "--source-root" or "--verify-freeze" or "--manifest-sha256") ||
            !options.TryAdd(args[i], args[i + 1])) throw new ArgumentException("Invalid or repeated option.");
    }
    if (!options.TryGetValue("--output", out var output))
        throw new ArgumentException("Usage: --output <empty-directory> with pilot options, --prepare-review training|calibration, --validate-review <csv-folder>, OR --collect-review <package> --package-sha256 <pinned-hash>.");
    if (options.TryGetValue("--prepare-freeze", out var freezePackage))
    {
        if (options.Count != 4 || !options.TryGetValue("--package-sha256", out var freezeDigest) || !options.TryGetValue("--source-root", out var sourceRoot))
            throw new ArgumentException("Draft preparation requires --prepare-freeze, --package-sha256, --source-root and --output only.");
        var draft = FreezePreparation.Write(sourceRoot, freezePackage, freezeDigest, output);
        Console.WriteLine($"Borrador preparado; política NO congelada. Guardar este SHA-256 fuera de la carpeta: {draft.ManifestSha256}");
        return 0;
    }
    if (options.TryGetValue("--verify-freeze", out var freezeDirectory))
    {
        if (options.Count != 3 || !options.TryGetValue("--manifest-sha256", out var manifestDigest))
            throw new ArgumentException("Draft verification requires --verify-freeze, --manifest-sha256 and --output only.");
        FreezePreparation.Verify(freezeDirectory, manifestDigest, output);
        Console.WriteLine("Integridad del borrador verificada; no certifica aprobación metodológica.");
        return 0;
    }
    if (options.ContainsKey("--source-root") || options.ContainsKey("--manifest-sha256"))
        throw new ArgumentException("Freeze-specific options require a compatible freeze command.");
    if (options.TryGetValue("--prepare-review", out var split))
    {
        if (options.Count != 2) throw new ArgumentException("Preparation accepts only --prepare-review training|calibration and --output.");
        Console.WriteLine($"Paquete sin juicios preparado. Conservar este SHA-256 fuera del paquete: {ReviewPackage.Write(split, output)}");
        return 0;
    }
    if (options.TryGetValue("--collect-review", out var package))
    {
        if (options.Count != 3 || !options.TryGetValue("--package-sha256", out var digest))
            throw new ArgumentException("Collection requires --collect-review, --package-sha256 and --output only.");
        var result = ReviewPackage.Collect(package, digest, output);
        Console.WriteLine(result.Complete ? "Paquete verificado y juicios completos; no acepta el ranking." : "Paquete verificado; faltan juicios o resoluciones.");
        return result.Complete ? 0 : 2;
    }
    if (options.TryGetValue("--evaluate-review", out var evaluationPackage))
    {
        if (options.Count != 5 || !options.TryGetValue("--package-sha256", out var pinnedDigest) ||
            !options.TryGetValue("--config", out var policyPath) || !options.TryGetValue("--variant", out var policyName))
            throw new ArgumentException("Comparison requires --evaluate-review, --package-sha256, --config, --variant and --output only.");
        var policy = policyName switch {
            "EquityOnly" => RankingVariant.EquityOnly, "EquityNewcomer" => RankingVariant.EquityNewcomer,
            "EquityRotation" => RankingVariant.EquityRotation, "Equity" => RankingVariant.Equity,
            _ => throw new ArgumentException("Variant must be EquityOnly, EquityNewcomer, EquityRotation or Equity.") };
        var weights = JsonSerializer.Deserialize<RankingConfiguration>(File.ReadAllText(policyPath), PilotExport.Json)
            ?? throw new ArgumentException("Configuration cannot be null.");
        var complete = HumanRankingExport.Write(evaluationPackage, pinnedDigest, output, weights, policy);
        Console.WriteLine(complete ? "Comparación de calibración guardada; no decide aceptación." : "Faltan juicios humanos; no se ejecutó la comparación.");
        return complete ? 0 : 2;
    }
    if (options.TryGetValue("--calibrate-review", out var calibrationPackage))
    {
        if (options.Count != 3 || !options.TryGetValue("--package-sha256", out var calibrationDigest))
            throw new ArgumentException("Calibration requires --calibrate-review, --package-sha256 and --output only.");
        var completed = CalibrationExport.Write(calibrationPackage, calibrationDigest, output);
        Console.WriteLine(completed ? "Barrido de calibración terminado; consultar selección provisional y criterios." : "Faltan juicios humanos; no se ejecutó el barrido.");
        return completed ? 0 : 2;
    }
    if (options.ContainsKey("--package-sha256") || options.ContainsKey("--variant"))
        throw new ArgumentException("--package-sha256/--variant require a compatible review command.");
    if (options.TryGetValue("--validate-review", out var reviewDirectory))
    {
        if (options.Count != 2) throw new ArgumentException("Review validation accepts only --validate-review and --output.");
        var review = HumanReviewFiles.Write(reviewDirectory, output);
        Console.WriteLine(review.Complete ? "Cobertura de juicios completa; esto no acepta el ranking." : "Juicios incompletos; consultar review-status.json.");
        return review.Complete ? 0 : 2;
    }
    var suite = options.GetValueOrDefault("--suite", "pilot");
    if (suite is not ("pilot" or "ablation")) throw new ArgumentException("Suite must be pilot or ablation.");
    var count = int.Parse(options.GetValueOrDefault("--queries", "5000"), CultureInfo.InvariantCulture);
    var seeds = options.GetValueOrDefault("--seeds", "17,29,43").Split(',').Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();
    var configuration = options.TryGetValue("--config", out var configPath)
        ? JsonSerializer.Deserialize<RankingConfiguration>(File.ReadAllText(configPath), PilotExport.Json)
            ?? throw new ArgumentException("Configuration cannot be null.") : new();
    PilotExport.Write(output, seeds, count, configuration, ablations: suite == "ablation");
    Console.WriteLine($"Piloto sintético guardado en {Path.GetFullPath(output)}");
    return 0;
}
catch (Exception error) when (error is ArgumentException or IOException or JsonException or FormatException or OverflowException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
