using FluentValidation;

namespace Ambio.Application.JobApplications;

public class OfferApplicationRequestValidator : AbstractValidator<OfferApplicationRequest>
{
    public OfferApplicationRequestValidator(TimeProvider timeProvider)
    {
        Include(new JobApplicationRequestValidator(timeProvider));

        RuleFor(ja => ja.Url)
            .NotEmpty()
            .Must(BeValidUrl)
            .OverridePropertyName("url");
    }

    private static bool BeValidUrl(object? value)
    {
        return value is string url
               && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
               && HasDomainName(uri);
    }

    // "https://acme" is an absolute URI, but not a site: the host needs a dot and an extension made of letters.
    private static bool HasDomainName(Uri uri)
    {
        var labels = uri.Host.Split('.');
        return uri.HostNameType == UriHostNameType.Dns
               && labels.Length >= 2
               && labels.All(label => label.Length > 0)
               && labels[^1].Length >= 2
               && labels[^1].All(char.IsLetter);
    }
}
