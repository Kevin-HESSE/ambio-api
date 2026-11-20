using Ambio.Domain.JobApplications;

using Bogus;

namespace Ambio.Api.IntegrationTests.Fixtures;

public static class FixturesFor
{
    private const string IsArchived = "archived";
    private const string IsArchivedRuleSet = $"default,{IsArchived}";

    public static class JobApplication
    {
        public static SpontaneousApplication GetSpontaneous(bool archived = false, string? title = null,
            string? company = null, ApplicationStatus? status = null)
        {
            Faker<SpontaneousApplication> faker = ApplySpontaneousRules(title, company, status);

            return archived
                ? faker.Generate(IsArchivedRuleSet)
                : faker.Generate();
        }

        public static List<SpontaneousApplication> GetSpontaneous(int count, bool archived = false,
            string? title = null,
            string? company = null,
            ApplicationStatus? status = null)
        {
            Faker<SpontaneousApplication> faker = ApplySpontaneousRules(title, company, status);

            return archived
                ? faker.Generate(count, IsArchivedRuleSet)
                : faker.Generate(count);
        }

        public static OfferApplication GetOffer(bool archived = false, string? title = null,
            string? company = null, ApplicationStatus? status = null)
        {
            Faker<OfferApplication> faker = ApplyOfferRules(title, company, status);

            return archived
                ? faker.Generate(IsArchivedRuleSet)
                : faker.Generate();
        }

        public static List<OfferApplication> GetOffer(int count, bool archived = false,
            string? title = null,
            string? company = null,
            ApplicationStatus? status = null)
        {
            Faker<OfferApplication> faker = ApplyOfferRules(title, company, status);

            return archived
                ? faker.Generate(count, IsArchivedRuleSet)
                : faker.Generate(count);
        }

        private static Faker<SpontaneousApplication> ApplySpontaneousRules(string? title, string? company, ApplicationStatus? status)
        {
            var faker = new Faker<SpontaneousApplication>()
                .RuleFor(j => j.Title, f => title ?? f.Name.JobTitle())
                .RuleFor(j => j.Company, f => company ?? f.Company.CompanyName())
                .RuleFor(j => j.Status, f => status ?? f.PickRandom<ApplicationStatus>())
                .RuleFor(j => j.AppliedOn, (f) => f.Date.PastDateOnly(refDate: TestClock.Today))
                .RuleSet(IsArchived, set =>
                    set.FinishWith((faker, c) => c.ArchivedAt = faker.Date.Past(refDate: TestClock.Start.UtcDateTime).ToUniversalTime())
                );

            return faker;
        }

        private static Faker<OfferApplication> ApplyOfferRules(string? title, string? company, ApplicationStatus? status)
        {
            var faker = new Faker<OfferApplication>()
                .RuleFor(j => j.Title, f => title ?? f.Name.JobTitle())
                .RuleFor(j => j.Company, f => company ?? f.Company.CompanyName())
                .RuleFor(j => j.Url, f => f.Internet.Url())
                .RuleFor(j => j.Status, f => status ?? f.PickRandom<ApplicationStatus>())
                .RuleFor(j => j.AppliedOn, (f) => f.Date.PastDateOnly(refDate: TestClock.Today))
                .RuleSet(IsArchived, set =>
                    set.FinishWith((faker, c) => c.ArchivedAt = faker.Date.Past(refDate: TestClock.Start.UtcDateTime).ToUniversalTime())
                );
            return faker;
        }
    }
}
