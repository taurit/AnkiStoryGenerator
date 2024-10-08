﻿using HtmlAgilityPack;
using System.IO;

namespace AnkiStoryGenerator.Utilities;

// Source: https://stackoverflow.com/a/1121515/889779
public static class HtmlHelpers
{
    /// <summary>
    /// Converts HTML to plain text / strips tags.
    /// </summary>
    /// <param name="html">The HTML.</param>
    /// <returns></returns>
    public static string ConvertToPlainText(string? html)
    {
        if (html is null)
        {
            return string.Empty;
        }

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        StringWriter sw = new();
        ConvertTo(doc.DocumentNode, sw);
        sw.Flush();
        return sw.ToString();
    }

    private static void ConvertContentTo(HtmlNode node, TextWriter outText)
    {
        foreach (var subNode in node.ChildNodes)
        {
            ConvertTo(subNode, outText);
        }
    }

    private static void ConvertTo(HtmlNode node, TextWriter outText)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Comment:
                // don't output comments
                break;

            case HtmlNodeType.Document:
                ConvertContentTo(node, outText);
                break;

            case HtmlNodeType.Text:
                // script and style must not be output
                var parentName = node.ParentNode.Name;
                if (parentName == "script" || parentName == "style" || parentName == "title")
                    break;

                // get text
                var html = ((HtmlTextNode)node).Text;

                // is it in fact a special closing node output as text?
                if (HtmlNode.IsOverlappedClosingElement(html))
                    break;

                // check the text is meaningful and not a bunch of whitespaces
                if (html.Trim().Length > 0)
                {
                    outText.Write(HtmlEntity.DeEntitize(html));
                }

                break;

            case HtmlNodeType.Element:
                switch (node.Name)
                {
                    case "p":
                        // treat paragraphs as crlf
                        outText.Write("\r\n");
                        break;
                    case "br":
                        outText.Write("\r\n");
                        break;
                }

                if (node.HasChildNodes)
                {
                    ConvertContentTo(node, outText);
                }

                break;
        }
    }

    public static string ExtractTitleFromHtml(string? latestStoryHtml)
    {
        if (latestStoryHtml is null)
        {
            return "Title not found";
        }

        HtmlDocument doc = new();
        doc.LoadHtml(latestStoryHtml);

        var titleNode = doc.DocumentNode.SelectSingleNode("//h1");
        if (titleNode is null)
        {
            return string.Empty;
        }

        return titleNode.InnerText;
    }
}
