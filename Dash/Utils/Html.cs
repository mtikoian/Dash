using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Dash
{
    public enum DashClasses
    {
        Btn,
        BtnSuccess,
        BtnWarning,
        BtnInfo,
        BtnError,
        BtnPrimary,
        BtnLink,
        DashAjax,
        DashConfirm,
        DashPrompt
    }

    public enum HttpVerbs
    {
        Get,
        Put,
        Post,
        Delete
    }

    public static class Html
    {
        public static IHtmlContent AuthorizedActionLink(this IHtmlHelper helper, string linkText, string action, string controller, object routeValues = null, object htmlAttributes = null,
            bool returnEmpty = true, bool hasAccess = true)
        {
            if (hasAccess)
            {
                return helper.ActionLink(linkText, action, controller, routeValues, htmlAttributes);
            }
            if (returnEmpty)
            {
                return HtmlString.Empty;
            }
            else if (htmlAttributes == null)
            {
                return new HtmlString(linkText);
            }
            else
            {
                var span = new TagBuilder("span");
                span.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
                span.InnerHtml.Append(linkText);
                return span;
            }
        }

        public static TagBuilder Icon(this IHtmlHelper helper, string icon, bool large = true, bool primary = false)
        {
            var i = new TagBuilder("i");
            i.AddCssClass("dash");
            if (primary)
            {
                i.AddCssClass("text-primary");
            }
            if (large)
            {
                i.AddCssClass("dash-lg");
            }
            i.AddCssClass("dash-" + icon);
            return i;
        }

        public static IHtmlContent InputGroupButton<TModel>(this IHtmlHelper<TModel> helper, string targetId, IHtmlContent buttonLabel, List<string> itemList)
        {
            var button = new TagBuilder("button");
            button.AddCssClass("btn btn-secondary dropdown-toggle");
            button.Attributes["type"] = "button";
            button.Attributes["data-toggle"] = "dropdown";
            button.InnerHtml.AppendHtml(buttonLabel);

            var dropdownDiv = new TagBuilder("div");
            dropdownDiv.AddCssClass("dropdown dropdown-right");
            var ul = new TagBuilder("ul");
            ul.AddCssClass("menu");
            foreach (var item in itemList)
            {
                var itemTag = new TagBuilder("li");
                itemTag.AddCssClass("menu-item c-hand");
                itemTag.Attributes["data-toggle"] = "input-replace";
                itemTag.Attributes["data-target"] = $"#{targetId}";
                itemTag.Attributes["data-value"] = item;
                itemTag.InnerHtml.AppendHtml(item);
                ul.InnerHtml.AppendHtml(itemTag);
            }
            dropdownDiv.InnerHtml.AppendHtml(button);
            dropdownDiv.InnerHtml.AppendHtml(ul);

            var groupDiv = new TagBuilder("div");
            groupDiv.AddCssClass("input-group-custom input-group-addon");
            groupDiv.InnerHtml.AppendHtml(dropdownDiv);
            return groupDiv;
        }
    }
}
