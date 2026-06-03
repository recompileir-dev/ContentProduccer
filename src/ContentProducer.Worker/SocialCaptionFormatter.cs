using System.Net;

namespace ContentProducer.Worker;

public static class SocialCaptionFormatter
{
    public static string BuildInstagramCaption(GeneratedArticle article, string postUrl)
    {
        return string.Join(
            Environment.NewLine,
            article.InstagramCaption.Trim(),
            string.Empty,
            "مطالعه کامل در سایت:",
            postUrl);
    }

    public static string BuildTelegramCaption(GeneratedArticle article, string postUrl)
    {
        string title = WebUtility.HtmlEncode(TrimToLength(article.Title.Trim(), 120));
        string summary = WebUtility.HtmlEncode(TrimToLength(article.InstagramCaption.Trim(), 700));
        string url = WebUtility.HtmlEncode(postUrl);

        return string.Join(
            Environment.NewLine,
            $"<b>{title}</b>",
            string.Empty,
            summary,
            string.Empty,
            $"<a href=\"{url}\">مطالعه کامل در سایت</a>");
    }

    private static string TrimToLength(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength - 3) + "...";
    }
}
