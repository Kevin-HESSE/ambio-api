using Ambio.Domain.JobApplications;

using Bogus;

namespace Ambio.Api.IntegrationTests.Fixtures;

public static class FixturesFor
{
    private const string IsArchived = "archived";
    private const string IsArchivedRuleSet = $"default,{IsArchived}";

    public static class JobApplication
    {
        public static SpontaneousApplication GetSpontaneous(bool archived = false, ApplicationStatus? status = null)
        {
            Faker<SpontaneousApplication> faker = ApplySpontaneousRules(status);

            return archived
                ? faker.Generate(IsArchivedRuleSet)
                : faker.Generate();
        }

        public static List<SpontaneousApplication> GetSpontaneous(int count, bool archived = false,
            ApplicationStatus? status = null)
        {
            Faker<SpontaneousApplication> faker = ApplySpontaneousRules(status);

            return archived
                ? faker.Generate(count, IsArchivedRuleSet)
                : faker.Generate(count);
        }

        public static OfferApplication GetOffer(bool archived = false, ApplicationStatus? status = null)
        {
            Faker<OfferApplication> faker = ApplyOfferRules(status);

            return archived
                ? faker.Generate(IsArchivedRuleSet)
                : faker.Generate();
        }

        public static List<OfferApplication> GetOffer(int count, bool archived = false,
            ApplicationStatus? status = null)
        {
            Faker<OfferApplication> faker = ApplyOfferRules(status);

            return archived
                ? faker.Generate(count, IsArchivedRuleSet)
                : faker.Generate(count);
        }

        private static Faker<SpontaneousApplication> ApplySpontaneousRules(ApplicationStatus? status)
        {
            var faker = new Faker<SpontaneousApplication>()
                .RuleFor(j => j.Title, f => f.Name.JobTitle())
                .RuleFor(j => j.Company, f => f.Company.CompanyName())
                .RuleFor(j => j.Status, f => status ?? f.PickRandom<ApplicationStatus>())
                .RuleFor(j => j.AppliedOn, (f) => f.Date.PastDateOnly())
                .RuleSet(IsArchived, set =>
                    set.FinishWith((faker, c) => c.ArchivedAt = faker.Date.Past().ToUniversalTime())
                );

            return faker;
        }

        private static Faker<OfferApplication> ApplyOfferRules(ApplicationStatus? status)
        {
            var faker = new Faker<OfferApplication>()
                .RuleFor(j => j.Title, f => f.Name.JobTitle())
                .RuleFor(j => j.Company, f => f.Company.CompanyName())
                .RuleFor(j => j.Url, f => f.Internet.Url())
                .RuleFor(j => j.Status, f => status ?? f.PickRandom<ApplicationStatus>())
                .RuleFor(j => j.AppliedOn, (f) => f.Date.PastDateOnly())
                .RuleSet(IsArchived, set =>
                    set.FinishWith((faker, c) => c.ArchivedAt = faker.Date.Future().ToUniversalTime())
                );
            return faker;
        }
    }
}
