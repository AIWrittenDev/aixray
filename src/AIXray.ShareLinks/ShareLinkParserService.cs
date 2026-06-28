using AIXray.Core;

namespace AIXray.ShareLinks;

/// <summary>
/// سرویس مرکزی پارس لینک‌ها — لینک‌ها را بر اساس پیشوند به پارس‌کننده‌ی مناسب ارسال می‌کند.
/// </summary>
public class ShareLinkParserService : IShareLinkParserService
{
    private readonly List<IShareLinkParser> _parsers;

    public ShareLinkParserService(IEnumerable<IShareLinkParser> parsers)
    {
        _parsers = parsers.ToList();
    }

    public Server? ParseLink(string link)
    {
        if (string.IsNullOrWhiteSpace(link))
            return null;

        link = link.Trim();
        foreach (var parser in _parsers)
        {
            if (link.StartsWith(parser.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return parser.TryParse(link);
            }
        }

        return null;
    }

    public List<Server> ParseLinks(string content)
    {
        var servers = new List<Server>();
        if (string.IsNullOrWhiteSpace(content))
            return servers;

        // هر خط ممکن است یک لینک باشد
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var server = ParseLink(line);
            if (server != null)
                servers.Add(server);
        }

        return servers;
    }
}
