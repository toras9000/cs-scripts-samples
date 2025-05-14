#r "nuget: Lestaly, 0.80.0"
#nullable enable
using System.Text.RegularExpressions;
using Lestaly;


public record UserEntry(int ID, string UserName, string SurName, string GivenName, string DisplayName, string Mail);

public static IEnumerable<UserEntry> ReadUserListCsv(this FileInfo self)
{
    var userListText = self.ReadAllText();
    var userListRecords = userListText.SplitFields();

    var expectCaptions = new[] { "ID", "UserName", "SurName", "GivenName", "DisplayName", "Mail", };
    var minFields = expectCaptions.Length;

    var header = true;
    foreach (var fields in userListRecords)
    {
        if (fields.Length <= 0) continue;

        if (header)
        {
            if (!expectCaptions.SequenceEqual(fields.Take(expectCaptions.Length))) throw new PavedMessageException("unexpected format");
            header = false;
        }
        else
        {
            if (fields.Length < minFields) throw new PavedMessageException("unexpected format");

            var idx = 0;
            var id = fields[idx++].TryParseNumber<int>();
            var username = fields[idx++];
            var surname = fields[idx++];
            var givenname = fields[idx++];
            var dispname = fields[idx++];
            var mail = fields[idx++];

            if (!id.HasValue) continue;
            if (username.IsEmpty()) continue;
            if (surname.IsEmpty()) continue;
            if (givenname.IsEmpty()) continue;
            if (dispname.IsEmpty()) continue;
            if (mail.IsEmpty()) continue;

            yield return new(id.Value, username, surname, givenname, dispname, mail);
        }
    }

}

public static IEnumerable<string> EnumerateTextBlocks(this FileInfo self)
{
    var buffer = new StringBuilder();
    foreach (var line in self.ReadLines())
    {
        if (line.IsWhite())
        {
            if (0 < buffer.Length)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
            continue;
        }

        buffer.AppendLine(line);
    }

    if (0 < buffer.Length)
    {
        yield return buffer.ToString();
    }
}

public static class RegexResources
{
    public static Regex Space { get; } = new(@"\s+");
    public static Regex AttrIndex { get; } = new(@"^\{\d+\}");
}

public static string NormalizeSpace(this string self)
    => RegexResources.Space.Replace(self, m =>
    {
        if (m.Index == 0) return "";
        if (self.Length <= m.Index + m.Length) return "";
        return " ";
    });

public static string RemoveIndex(this string self)
    => RegexResources.AttrIndex.Replace(self, "");

