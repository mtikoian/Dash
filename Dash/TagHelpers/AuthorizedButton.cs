﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Dash.TagHelpers
{
    public class AuthorizedButtonTagHelper : BaseTagHelper
    {
        private IHttpContextAccessor _HttpContextAccessor;

        public AuthorizedButtonTagHelper(IHtmlHelper htmlHelper, IHttpContextAccessor httpContextAccessor) : base(htmlHelper)
        {
            _HttpContextAccessor = httpContextAccessor;
        }

        public string Action { get; set; }
        public DashClasses Class { get; set; } = DashClasses.BtnPrimary;
        public object RouteValues { get; set; }
        public string Controller { get; set; }
        public bool? HasAccess { get; set; }
        public string Title { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            Contextualize();
            var hasAccess = HasAccess ?? _HttpContextAccessor.HttpContext.User.HasAccess(Controller, Action, HttpVerbs.Get);

            output.TagMode = TagMode.StartTagAndEndTag;
            if (!hasAccess)
            {
                NoRender = true;
                base.Process(context, output);
                return;
            }

            output.TagName = "a";
            var urlHelper = new UrlHelper(_HtmlHelper.ViewContext);
            output.Attributes.Add("href", urlHelper.Action(Action, Controller, RouteValues));
            output.Attributes.Add("data-method", "GET");
            output.Attributes.Add("title", Title);
            output.Content.Append(Title);

            var classList = new List<string>();
            classList.Merge("btn");
            classList.Merge("mr-1");
            classList.Merge(Class.ToCssClass());
            output.Attributes.Add("class", classList.Join(" "));

            base.Process(context, output);
        }
    }
}