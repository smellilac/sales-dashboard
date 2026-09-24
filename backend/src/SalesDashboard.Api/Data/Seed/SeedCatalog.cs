namespace SalesDashboard.Api.Data.Seed;

/// <summary>
/// The static product catalogue (six categories, 36 products). Prices are given as bands; the generator
/// rolls a concrete <c>list_price</c> inside each band and a <c>base_cost</c> from the category's cost-ratio
/// band. Cost ratio encodes margin: accessories carry the highest margin (lowest ratio), agro drones the
/// lowest (highest ratio); all ratios stay within the 0.65–0.85 bound (D-spec).
/// </summary>
internal static class SeedCatalog
{
    /// <param name="CostRatioMin">Lowest <c>base_cost / list_price</c> for products in this category.</param>
    /// <param name="CostRatioMax">Highest <c>base_cost / list_price</c> for products in this category.</param>
    public sealed record CategoryDef(string Name, decimal CostRatioMin, decimal CostRatioMax);

    public sealed record ProductDef(string Name, string Category, decimal PriceMin, decimal PriceMax);

    public const string IndustrialDrones = "Промышленные дроны";
    public const string AgroDrones = "Агродроны";
    public const string ConsumerDrones = "Потребительские дроны";
    public const string Payload = "Полезная нагрузка";
    public const string BatteriesAndCharging = "Аккумуляторы и зарядка";
    public const string Accessories = "Аксессуары";

    public static IReadOnlyList<CategoryDef> Categories { get; } =
    [
        new(IndustrialDrones, 0.72m, 0.80m),
        new(AgroDrones, 0.80m, 0.85m),
        new(ConsumerDrones, 0.72m, 0.82m),
        new(Payload, 0.70m, 0.80m),
        new(BatteriesAndCharging, 0.68m, 0.78m),
        new(Accessories, 0.65m, 0.72m),
    ];

    public static IReadOnlyList<ProductDef> Products { get; } =
    [
        // Промышленные дроны (0.4–1.5 млн ₽).
        new("Matrice 350 RTK", IndustrialDrones, 1_200_000m, 1_500_000m),
        new("Matrice 30T", IndustrialDrones, 700_000m, 950_000m),
        new("Mavic 3 Enterprise", IndustrialDrones, 400_000m, 520_000m),

        // Агродроны (1–2.5 млн ₽).
        new("Agras T50", AgroDrones, 1_800_000m, 2_500_000m),
        new("Agras T25", AgroDrones, 1_000_000m, 1_400_000m),

        // Потребительские дроны (70–350 тыс. ₽).
        new("Mini 4 Pro", ConsumerDrones, 70_000m, 95_000m),
        new("Mini 4 Pro Fly More Combo", ConsumerDrones, 110_000m, 140_000m),
        new("Air 3", ConsumerDrones, 120_000m, 160_000m),
        new("Air 3S", ConsumerDrones, 150_000m, 190_000m),
        new("Mavic 3 Classic", ConsumerDrones, 200_000m, 260_000m),
        new("Mavic 3 Pro", ConsumerDrones, 250_000m, 320_000m),

        // Полезная нагрузка (0.5–1.8 млн ₽).
        new("Zenmuse L1", Payload, 500_000m, 700_000m),
        new("Zenmuse P1", Payload, 800_000m, 1_050_000m),
        new("Zenmuse L2", Payload, 900_000m, 1_200_000m),
        new("Zenmuse H20N", Payload, 900_000m, 1_150_000m),
        new("Zenmuse H30T", Payload, 1_400_000m, 1_800_000m),

        // Аккумуляторы и зарядка (20–250 тыс. ₽).
        new("TB60 Intelligent Battery", BatteriesAndCharging, 70_000m, 100_000m),
        new("TB65 Intelligent Battery", BatteriesAndCharging, 90_000m, 130_000m),
        new("BS65 Battery Station", BatteriesAndCharging, 200_000m, 250_000m),
        new("WB37 Battery", BatteriesAndCharging, 20_000m, 30_000m),
        new("Mavic 3E Battery", BatteriesAndCharging, 22_000m, 32_000m),
        new("Air 3 Intelligent Battery", BatteriesAndCharging, 20_000m, 28_000m),
        new("100W Charging Hub", BatteriesAndCharging, 25_000m, 40_000m),
        new("65W Car Charger", BatteriesAndCharging, 20_000m, 28_000m),

        // Аксессуары (3–80 тыс. ₽).
        new("RC Plus Controller", Accessories, 60_000m, 80_000m),
        new("RC Pro Enterprise Controller", Accessories, 55_000m, 75_000m),
        new("Speaker Module", Accessories, 30_000m, 45_000m),
        new("Spotlight Module", Accessories, 28_000m, 42_000m),
        new("Beacon", Accessories, 12_000m, 20_000m),
        new("ND Filter Set", Accessories, 8_000m, 16_000m),
        new("microSD 512GB", Accessories, 6_000m, 12_000m),
        new("Landing Pad", Accessories, 4_000m, 9_000m),
        new("Propeller Guard", Accessories, 3_500m, 7_000m),
        new("Low-Noise Propellers", Accessories, 3_000m, 6_000m),
        new("USB-C Cable", Accessories, 3_000m, 5_000m),
        new("Lens Cleaning Kit", Accessories, 3_000m, 6_000m),
    ];
}
