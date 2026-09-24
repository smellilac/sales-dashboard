using Bogus;
using SalesDashboard.Api.Data.Entities;

namespace SalesDashboard.Api.Data.Seed;

/// <summary>
/// Deterministic, DB-free seed generator (D13). Same <paramref name="now"/> and <paramref name="zone"/>
/// always yield the same data, ids included: it draws everything from a single seeded
/// <see cref="Randomizer"/> (never the global <c>Randomizer.Seed</c>, <c>Guid.*</c>, <c>Random.Shared</c>
/// or the wall clock) and builds ids with <see cref="SeedGuid"/>. Generation order is fixed:
/// managers → customers → catalogue → sales by day.
/// </summary>
public static class SeedDataGenerator
{
    private const int Seed = 20260924;
    private const int PeriodDays = 365;
    private const double DailyBase = 13.5;
    private const double WeekendFactor = 0.10;
    private const int BigDealCount = 4;

    /// <summary>The materialized seed. Every entity has its id and navigation ids populated.</summary>
    public sealed record SeedData(
        IReadOnlyList<Manager> Managers,
        IReadOnlyList<Customer> Customers,
        IReadOnlyList<Category> Categories,
        IReadOnlyList<Product> Products,
        IReadOnlyList<Sale> Sales,
        IReadOnlyList<SaleItem> SaleItems);

    public static SeedData Generate(DateTimeOffset now, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        var random = new Randomizer(Seed);
        var faker = new Faker("ru") { Random = random };

        var nowUtc = now.UtcDateTime;
        var todayLocal = TimeZoneInfo.ConvertTime(now, zone).DateTime;
        var startDate = DateOnly.FromDateTime(todayLocal).AddDays(-PeriodDays);
        var endDate = DateOnly.FromDateTime(todayLocal);
        var periodStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), zone);

        // Non-sale ids share a stable timestamp (the period start); only sale ids carry SoldAt.
        var baseTimestamp = new DateTimeOffset(periodStartUtc, TimeSpan.Zero);

        var managers = BuildManagers(faker, random, baseTimestamp);
        var customers = BuildCustomers(faker, random, baseTimestamp);
        var ownedByManager = AssignOwners(managers, customers, random);
        var catalog = BuildCatalog(random, baseTimestamp, periodStartUtc.AddDays(-30), nowUtc.AddDays(-180));

        var drafts = new List<SaleDraft>();
        GenerateDailySales(drafts, managers, ownedByManager, customers, catalog, random, zone, startDate, endDate, nowUtc, now);
        AddBigDeals(drafts, managers, ownedByManager, customers, catalog, random, zone, startDate, nowUtc, now);

        var (sales, items) = Materialize(drafts, random);

        return new SeedData(
            managers.Select(m => m.Entity).ToList(),
            customers,
            catalog.Categories,
            catalog.Products.Select(p => p.Entity).ToList(),
            sales,
            items);
    }

    // ---- managers ---------------------------------------------------------

    private sealed record SeedManager(Manager Entity, ManagerProfile Profile);

    private static List<SeedManager> BuildManagers(Faker faker, Randomizer random, DateTimeOffset timestamp)
    {
        var managers = new List<SeedManager>(ManagerProfile.All.Count);
        foreach (var profile in ManagerProfile.All)
        {
            var entity = new Manager
            {
                Id = SeedGuid.Create(timestamp, random),
                FirstName = faker.Name.FirstName(),
                LastName = faker.Name.LastName(),
                Team = profile.Team,
                Position = profile.Position,
                IsActive = profile.IsActive,
            };
            managers.Add(new SeedManager(entity, profile));
        }

        return managers;
    }

    // ---- customers --------------------------------------------------------

    private static List<Customer> BuildCustomers(Faker faker, Randomizer random, DateTimeOffset timestamp)
    {
        // ~25% Enterprise, ~60% Smb, ~15% Government.
        var segments = new List<CustomerSegment>(80);
        segments.AddRange(Enumerable.Repeat(CustomerSegment.Enterprise, 20));
        segments.AddRange(Enumerable.Repeat(CustomerSegment.Smb, 48));
        segments.AddRange(Enumerable.Repeat(CustomerSegment.Government, 12));

        return segments
            .Select(segment => new Customer
            {
                Id = SeedGuid.Create(timestamp, random),
                ContactName = faker.Name.FullName(),
                Company = faker.Company.CompanyName(),
                Segment = segment,
            })
            .ToList();
    }

    private static Dictionary<int, List<Customer>> AssignOwners(
        List<SeedManager> managers, List<Customer> customers, Randomizer random)
    {
        // Each customer gets 1-2 "own" managers; result maps manager index -> its customers.
        var owned = new Dictionary<int, List<Customer>>();
        foreach (var customer in customers)
        {
            var ownerCount = random.Int(1, 2);
            var picked = new HashSet<int>();
            while (picked.Count < ownerCount)
            {
                picked.Add(random.Int(0, managers.Count - 1));
            }

            foreach (var index in picked)
            {
                if (!owned.TryGetValue(index, out var list))
                {
                    list = [];
                    owned[index] = list;
                }

                list.Add(customer);
            }
        }

        return owned;
    }

    // ---- catalogue --------------------------------------------------------

    private sealed record ProductVersion(decimal ListPrice, decimal BaseCost, DateTime VersionAt);

    private sealed record CatalogProduct(Product Entity, string CategoryName, IReadOnlyList<ProductVersion> Versions)
    {
        public ProductVersion VersionAt(DateTime soldAtUtc) => Versions.Last(v => v.VersionAt <= soldAtUtc);
    }

    private sealed record Catalog(
        IReadOnlyList<Category> Categories,
        IReadOnlyList<CatalogProduct> Products,
        IReadOnlyDictionary<string, List<CatalogProduct>> ByCategory);

    private static Catalog BuildCatalog(
        Randomizer random, DateTimeOffset idTimestamp, DateTime initialVersionAt, DateTime updateVersionAt)
    {
        var categories = SeedCatalog.Categories
            .Select(def => new Category { Id = SeedGuid.Create(idTimestamp, random), Name = def.Name })
            .ToList();
        var categoryByName = categories.ToDictionary(c => c.Name, StringComparer.Ordinal);
        var ratioByName = SeedCatalog.Categories.ToDictionary(
            c => c.Name, c => (c.CostRatioMin, c.CostRatioMax), StringComparer.Ordinal);

        var products = new List<CatalogProduct>(SeedCatalog.Products.Count);
        var byCategory = new Dictionary<string, List<CatalogProduct>>(StringComparer.Ordinal);
        var sku = 1;

        foreach (var def in SeedCatalog.Products)
        {
            var listPrice = Money(random.Decimal(def.PriceMin, def.PriceMax));
            var (ratioMin, ratioMax) = ratioByName[def.Category];
            var baseCost = Money(listPrice * random.Decimal(ratioMin, ratioMax));

            var versions = new List<ProductVersion> { new(listPrice, baseCost, initialVersionAt) };

            // ~30% of products get a mid-year price bump and a new version.
            if (random.Bool(0.30f))
            {
                var newList = Money(listPrice * (1m + random.Decimal(0.05m, 0.15m)));
                var newCost = Money(baseCost * (1m + random.Decimal(0.03m, 0.10m)));
                versions.Add(new ProductVersion(newList, newCost, updateVersionAt));
            }

            var latest = versions[^1];
            var entity = new Product
            {
                Id = SeedGuid.Create(idTimestamp, random),
                Sku = $"CAT-{sku:D3}",
                Name = def.Name,
                CategoryId = categoryByName[def.Category].Id,
                ListPrice = latest.ListPrice,
                BaseCost = latest.BaseCost,
                VersionAt = latest.VersionAt,
            };
            sku++;

            var product = new CatalogProduct(entity, def.Category, versions);
            products.Add(product);

            if (!byCategory.TryGetValue(def.Category, out var list))
            {
                list = [];
                byCategory[def.Category] = list;
            }

            list.Add(product);
        }

        return new Catalog(categories, products, byCategory);
    }

    // ---- sales ------------------------------------------------------------

    private sealed record ItemDraft(CatalogProduct Product, DateTime ProductVersionAt, int Quantity, decimal UnitPrice, decimal UnitCost);

    private sealed record SaleDraft(Guid ManagerId, Guid CustomerId, DateTime SoldAtUtc, SaleStatus Status, List<ItemDraft> Items);

    private static readonly HashSet<string> ExpensiveCategories = new(StringComparer.Ordinal)
    {
        SeedCatalog.IndustrialDrones,
        SeedCatalog.AgroDrones,
        SeedCatalog.Payload,
    };

    private static (string[] Categories, float[] Weights) TeamCategoryWeights(string team) => team switch
    {
        ManagerProfile.TeamEnterprise => (
            [SeedCatalog.IndustrialDrones, SeedCatalog.Payload, SeedCatalog.BatteriesAndCharging, SeedCatalog.Accessories, SeedCatalog.ConsumerDrones],
            [3.0f, 2.0f, 1.5f, 1.5f, 0.5f]),
        ManagerProfile.TeamAgro => (
            [SeedCatalog.AgroDrones, SeedCatalog.BatteriesAndCharging, SeedCatalog.Accessories, SeedCatalog.Payload],
            [3.0f, 1.5f, 1.5f, 0.5f]),
        ManagerProfile.TeamRetail => (
            [SeedCatalog.ConsumerDrones, SeedCatalog.Accessories, SeedCatalog.BatteriesAndCharging],
            [3.0f, 2.0f, 1.0f]),
        _ => (
            [SeedCatalog.IndustrialDrones, SeedCatalog.Payload, SeedCatalog.BatteriesAndCharging, SeedCatalog.Accessories],
            [2.5f, 2.5f, 1.5f, 1.5f]),
    };

    private static double SeasonFactor(int month) => month switch
    {
        1 => 0.6,
        2 => 0.8,
        3 or 4 or 5 => 1.3,
        9 or 10 => 1.1,
        12 => 1.4,
        _ => 1.0,
    };

    private static double SegmentSizeFactor(CustomerSegment segment) => segment switch
    {
        CustomerSegment.Enterprise => 1.3,
        CustomerSegment.Government => 1.4,
        _ => 0.8,
    };

    private static float SegmentPickWeight(CustomerSegment segment) =>
        segment == CustomerSegment.Government ? 0.5f : 1.0f; // Government buys less often.

    private static void GenerateDailySales(
        List<SaleDraft> drafts,
        List<SeedManager> managers,
        Dictionary<int, List<Customer>> ownedByManager,
        List<Customer> customers,
        Catalog catalog,
        Randomizer random,
        TimeZoneInfo zone,
        DateOnly startDate,
        DateOnly endDate,
        DateTime nowUtc,
        DateTimeOffset now)
    {
        var vacationStart = now.AddDays(-120).UtcDateTime;
        var vacationEnd = vacationStart.AddDays(42);
        var terminatedCutoff = now.AddDays(-90).UtcDateTime;

        var customerArray = customers.ToArray();
        var customerWeights = customers.Select(c => SegmentPickWeight(c.Segment)).ToArray();

        var carry = 0.0;
        for (var day = startDate; day <= endDate; day = day.AddDays(1))
        {
            var weekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var expected = DailyBase * SeasonFactor(day.Month) * (weekend ? WeekendFactor : 1.0);
            var value = expected + carry;
            var count = (int)Math.Floor(value);
            carry = value - count;

            for (var i = 0; i < count; i++)
            {
                var soldAtUtc = RollSoldAt(random, zone, day);
                if (soldAtUtc > nowUtc)
                {
                    continue;
                }

                var managerIndex = PickManager(random, managers, soldAtUtc, vacationStart, vacationEnd, terminatedCutoff);
                if (managerIndex < 0)
                {
                    continue;
                }

                var manager = managers[managerIndex];
                var customer = PickCustomer(random, managerIndex, ownedByManager, customerArray, customerWeights);
                var status = RollStatus(random, manager.Profile);
                var items = BuildItems(random, catalog, manager.Entity.Team, manager.Profile, customer.Segment, soldAtUtc);
                if (items.Count == 0)
                {
                    continue;
                }

                drafts.Add(new SaleDraft(manager.Entity.Id, customer.Id, soldAtUtc, status, items));
            }
        }
    }

    private static DateTime RollSoldAt(Randomizer random, TimeZoneInfo zone, DateOnly day)
    {
        // Business hours 09:00-20:00 (up to 19:59:59).
        var local = day.ToDateTime(
            new TimeOnly(random.Int(9, 19), random.Int(0, 59), random.Int(0, 59)), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }

    private static int PickManager(
        Randomizer random,
        List<SeedManager> managers,
        DateTime soldAtUtc,
        DateTime vacationStart,
        DateTime vacationEnd,
        DateTime terminatedCutoff)
    {
        double totalWeight = 0;
        Span<double> cumulative = stackalloc double[managers.Count];
        for (var i = 0; i < managers.Count; i++)
        {
            var profile = managers[i].Profile;
            var eligible = profile.Kind switch
            {
                ManagerProfileKind.Vacation => soldAtUtc < vacationStart || soldAtUtc >= vacationEnd,
                ManagerProfileKind.Terminated => soldAtUtc < terminatedCutoff,
                _ => true,
            };
            totalWeight += eligible ? profile.FrequencyWeight : 0;
            cumulative[i] = totalWeight;
        }

        if (totalWeight <= 0)
        {
            return -1;
        }

        var roll = random.Double() * totalWeight;
        for (var i = 0; i < managers.Count; i++)
        {
            if (roll < cumulative[i])
            {
                return i;
            }
        }

        return managers.Count - 1;
    }

    private static Customer PickCustomer(
        Randomizer random,
        int managerIndex,
        Dictionary<int, List<Customer>> ownedByManager,
        Customer[] customers,
        float[] weights)
    {
        if (random.Bool(0.8f) && ownedByManager.TryGetValue(managerIndex, out var owned) && owned.Count > 0)
        {
            return owned[random.Int(0, owned.Count - 1)];
        }

        return random.WeightedRandom(customers, weights);
    }

    private static SaleStatus RollStatus(Randomizer random, ManagerProfile profile)
    {
        var roll = random.Double();
        if (roll < profile.CancelledShare)
        {
            return SaleStatus.Cancelled;
        }

        if (roll < profile.CancelledShare + profile.RefundedShare)
        {
            return SaleStatus.Refunded;
        }

        return SaleStatus.Paid;
    }

    private static List<ItemDraft> BuildItems(
        Randomizer random,
        Catalog catalog,
        string team,
        ManagerProfile profile,
        CustomerSegment segment,
        DateTime soldAtUtc)
    {
        var lineCount = random.Int(profile.LineItemsMin, profile.LineItemsMax);
        var (categories, weights) = TeamCategoryWeights(team);
        var sizeFactor = SegmentSizeFactor(segment);

        var items = new List<ItemDraft>(lineCount);
        var used = new HashSet<Guid>();
        for (var line = 0; line < lineCount; line++)
        {
            var product = PickDistinctProduct(random, catalog, categories, weights, used);
            if (product is null)
            {
                continue;
            }

            var version = product.VersionAt(soldAtUtc);
            var qtyMax = QuantityCap(product.CategoryName, profile);
            var qtyMin = Math.Min(profile.QuantityMin, qtyMax);
            var quantity = Math.Max(1, (int)Math.Round(random.Int(qtyMin, qtyMax) * sizeFactor));

            var discount = random.Decimal(profile.DiscountMin, profile.DiscountMax);
            var unitPrice = Money(version.ListPrice * (1m - discount));
            var unitCost = Money(version.BaseCost * random.Decimal(0.97m, 1.03m));

            items.Add(new ItemDraft(product, version.VersionAt, quantity, unitPrice, unitCost));
        }

        return items;
    }

    private static int QuantityCap(string categoryName, ManagerProfile profile)
    {
        if (!ExpensiveCategories.Contains(categoryName))
        {
            return profile.QuantityMax;
        }

        // Drones and payload sell in ones and twos, even for big-deal managers.
        return profile.Kind == ManagerProfileKind.BigDeals ? 3 : 2;
    }

    private static CatalogProduct? PickDistinctProduct(
        Randomizer random,
        Catalog catalog,
        string[] categories,
        float[] weights,
        HashSet<Guid> used)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var category = random.WeightedRandom(categories, weights);
            var candidates = catalog.ByCategory[category];
            var candidate = candidates[random.Int(0, candidates.Count - 1)];
            if (used.Add(candidate.Entity.Id))
            {
                return candidate;
            }
        }

        return null;
    }

    private static void AddBigDeals(
        List<SaleDraft> drafts,
        List<SeedManager> managers,
        Dictionary<int, List<Customer>> ownedByManager,
        List<Customer> customers,
        Catalog catalog,
        Randomizer random,
        TimeZoneInfo zone,
        DateOnly startDate,
        DateTime nowUtc,
        DateTimeOffset now)
    {
        // 4 extra Paid deals of 15-40M ₽, in different months and by different managers.
        var expensive = catalog.Products.Where(p => ExpensiveCategories.Contains(p.CategoryName)).ToList();
        var eligibleManagers = managers
            .Select((m, index) => (m, index))
            .Where(x => x.m.Profile.Kind is not (ManagerProfileKind.Vacation or ManagerProfileKind.Terminated))
            .Select(x => x.index)
            .ToList();
        var customerArray = customers.ToArray();
        var customerWeights = customers.Select(c => SegmentPickWeight(c.Segment)).ToArray();

        var monthOffsets = new[] { 30, 120, 210, 300 };
        var usedManagers = new HashSet<int>();

        for (var i = 0; i < BigDealCount; i++)
        {
            var dayLocal = TimeZoneInfo.ConvertTime(now.AddDays(-monthOffsets[i]), zone).DateTime;
            var day = DateOnly.FromDateTime(dayLocal);
            if (day < startDate)
            {
                day = startDate.AddDays(i);
            }

            var soldAtUtc = RollSoldAt(random, zone, day);
            if (soldAtUtc > nowUtc)
            {
                soldAtUtc = nowUtc.AddHours(-1);
            }

            var managerIndex = eligibleManagers[random.Int(0, eligibleManagers.Count - 1)];
            var guard = 0;
            while (!usedManagers.Add(managerIndex) && guard++ < eligibleManagers.Count)
            {
                managerIndex = eligibleManagers[random.Int(0, eligibleManagers.Count - 1)];
            }

            var customer = PickCustomer(random, managerIndex, ownedByManager, customerArray, customerWeights);
            var product = expensive[random.Int(0, expensive.Count - 1)];
            var version = product.VersionAt(soldAtUtc);
            var target = random.Decimal(15_000_000m, 40_000_000m);
            var quantity = Math.Max(1, (int)Math.Round(target / version.ListPrice));
            var unitPrice = Money(version.ListPrice * (1m - random.Decimal(0.00m, 0.05m)));
            var unitCost = Money(version.BaseCost * random.Decimal(0.97m, 1.03m));

            drafts.Add(new SaleDraft(
                managers[managerIndex].Entity.Id,
                customer.Id,
                soldAtUtc,
                SaleStatus.Paid,
                [new ItemDraft(product, version.VersionAt, quantity, unitPrice, unitCost)]));
        }
    }

    // ---- materialize with ordered ids ------------------------------------

    private static (List<Sale> Sales, List<SaleItem> Items) Materialize(List<SaleDraft> drafts, Randomizer random)
    {
        // Assign ids in SoldAt order so sale ids (UUID v7) are ordered by SoldAt.
        var ordered = drafts
            .Select((draft, index) => (draft, index))
            .OrderBy(x => x.draft.SoldAtUtc)
            .ThenBy(x => x.index)
            .Select(x => x.draft)
            .ToList();

        var sales = new List<Sale>(ordered.Count);
        var items = new List<SaleItem>(ordered.Count * 2);

        foreach (var draft in ordered)
        {
            var soldAt = new DateTimeOffset(draft.SoldAtUtc, TimeSpan.Zero);
            var saleId = SeedGuid.Create(soldAt, random);
            sales.Add(new Sale
            {
                Id = saleId,
                ManagerId = draft.ManagerId,
                CustomerId = draft.CustomerId,
                SoldAt = draft.SoldAtUtc,
                Status = draft.Status,
            });

            foreach (var item in draft.Items)
            {
                items.Add(new SaleItem
                {
                    Id = SeedGuid.Create(soldAt, random),
                    SaleId = saleId,
                    ProductId = item.Product.Entity.Id,
                    ProductVersionAt = item.ProductVersionAt,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    UnitCost = item.UnitCost,
                });
            }
        }

        return (sales, items);
    }

    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
